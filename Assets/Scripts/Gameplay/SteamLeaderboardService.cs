using System;
using System.Collections.Generic;
using UnityEngine;

#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
using Steamworks;
#endif

public class SteamLeaderboardService : MonoBehaviour
{
    public const string DefaultLeaderboardName = "Tetrabeasts_GlobalScore";

    static SteamLeaderboardService _instance;
    public static SteamLeaderboardService Instance => _instance;

    public event Action AvatarsChanged;

    [SerializeField] string leaderboardName = DefaultLeaderboardName;
#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
    [SerializeField] bool logSteamStatus = true;
#endif

    readonly Dictionary<ulong, Sprite> _avatarSprites = new();

#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
    SteamLeaderboard_t _leaderboard;
    bool _steamInitialized;
    bool _leaderboardReady;
    bool _leaderboardFindInFlight;
    string _lastStatus = "Steam leaderboard ready.";

    CallResult<LeaderboardFindResult_t> _findLeaderboardResult;
    CallResult<LeaderboardScoreUploaded_t> _uploadResult;
    CallResult<LeaderboardScoresDownloaded_t> _globalResult;
    CallResult<LeaderboardScoresDownloaded_t> _friendsResult;
    CallResult<LeaderboardScoresDownloaded_t> _currentRankResult;
    Callback<AvatarImageLoaded_t> _avatarLoadedCallback;

    readonly Queue<Action> _pendingLeaderboardActions = new();
#else
    string _lastStatus = "Steamworks.NET is not enabled for this build.";
#endif

    public bool IsAvailable
    {
        get
        {
#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
            return _steamInitialized;
#else
            return false;
#endif
        }
    }

    public static SteamLeaderboardService Ensure(string requestedLeaderboardName = DefaultLeaderboardName)
    {
        if (_instance)
        {
            _instance.SetLeaderboardName(requestedLeaderboardName);
            return _instance;
        }

        var go = new GameObject("SteamLeaderboardService");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<SteamLeaderboardService>();
        _instance.SetLeaderboardName(requestedLeaderboardName);
        return _instance;
    }

    void Awake()
    {
        if (_instance && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeSteamIfAvailable();
    }

    void Update()
    {
#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
        if (_steamInitialized)
            SteamAPI.RunCallbacks();
#endif
    }

    void OnApplicationQuit()
    {
#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
        if (_steamInitialized)
            SteamAPI.Shutdown();
#endif
    }

    public void SetLeaderboardName(string requestedLeaderboardName)
    {
        if (string.IsNullOrWhiteSpace(requestedLeaderboardName))
            requestedLeaderboardName = DefaultLeaderboardName;

        if (leaderboardName == requestedLeaderboardName)
            return;

        leaderboardName = requestedLeaderboardName;
#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
        _leaderboardReady = false;
        _leaderboard = default;
#endif
    }

    public void SubmitScore(int score, PlayerCharacterData commander, PlayerCharacterData[] commanderRoster = null)
    {
        if (score <= 0)
            return;

#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
        if (!_steamInitialized)
            return;

        RunWhenLeaderboardReady(() =>
        {
            int commanderHash = GetCommanderHash(commander);
            var details = new[] { commanderHash };
            var handle = SteamUserStats.UploadLeaderboardScore(
                _leaderboard,
                ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest,
                score,
                details,
                details.Length);

            _uploadResult ??= CallResult<LeaderboardScoreUploaded_t>.Create(OnScoreUploaded);
            _uploadResult.Set(handle);
        });
#else
        _lastStatus = "Steamworks.NET is not enabled for this build.";
#endif
    }

    public void RefreshAll(PlayerCharacterData[] commanderRoster, Action<SteamLeaderboardSnapshot> onComplete)
    {
#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
        if (!_steamInitialized)
        {
            onComplete?.Invoke(CreateUnavailableSnapshot(_lastStatus));
            return;
        }

        RunWhenLeaderboardReady(() =>
        {
            var snapshot = new SteamLeaderboardSnapshot { isAvailable = true, statusMessage = "Steam leaderboard ready." };
            int remaining = 3;

            void Done()
            {
                remaining--;
                if (remaining > 0)
                    return;

                MergeFriendAndLocalRows(snapshot);
                ApplyCommanderSprites(snapshot.global, commanderRoster);
                ApplyCommanderSprites(snapshot.friends, commanderRoster);
                ApplyCommanderSprites(snapshot.currentRank, commanderRoster);
                snapshot.statusMessage = _lastStatus;
                onComplete?.Invoke(snapshot);
            }

            DownloadEntries(
                ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal,
                1,
                10,
                _globalResult ??= CallResult<LeaderboardScoresDownloaded_t>.Create(),
                entries =>
                {
                    snapshot.global.AddRange(entries);
                    Done();
                });

            DownloadEntries(
                ELeaderboardDataRequest.k_ELeaderboardDataRequestFriends,
                1,
                10,
                _friendsResult ??= CallResult<LeaderboardScoresDownloaded_t>.Create(),
                entries =>
                {
                    snapshot.friends.AddRange(entries);
                    Done();
                });

            DownloadEntries(
                ELeaderboardDataRequest.k_ELeaderboardDataRequestAroundUser,
                -4,
                5,
                _currentRankResult ??= CallResult<LeaderboardScoresDownloaded_t>.Create(),
                entries =>
                {
                    snapshot.currentRank.AddRange(entries);
                    Done();
                });
        });
#else
        onComplete?.Invoke(CreateUnavailableSnapshot(_lastStatus));
#endif
    }

    public Sprite GetAvatarSprite(ulong steamId)
    {
        if (steamId == 0)
            return null;

        if (_avatarSprites.TryGetValue(steamId, out var sprite))
            return sprite;

#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
        if (_steamInitialized)
            TryCacheAvatarSprite(new CSteamID(steamId));
#endif
        return _avatarSprites.TryGetValue(steamId, out sprite) ? sprite : null;
    }

    public bool TryGetLocalPlayerName(out string playerName)
    {
        playerName = null;

#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
        if (!_steamInitialized)
            return false;

        playerName = SteamFriends.GetPersonaName();
        return !string.IsNullOrWhiteSpace(playerName);
#else
        return false;
#endif
    }

    public static int GetCommanderHash(PlayerCharacterData commander)
    {
        if (!commander)
            return 0;

        string stableId = !string.IsNullOrWhiteSpace(commander.name) ? commander.name : commander.displayName;
        return StableHash(stableId);
    }

    public static PlayerCharacterData ResolveCommander(int commanderHash, PlayerCharacterData[] roster)
    {
        if (commanderHash == 0 || roster == null)
            return null;

        for (int i = 0; i < roster.Length; i++)
        {
            var commander = roster[i];
            if (!commander)
                continue;

            if (GetCommanderHash(commander) == commanderHash)
                return commander;
        }

        return null;
    }

    static int StableHash(string value)
    {
        unchecked
        {
            const uint fnvOffset = 2166136261;
            const uint fnvPrime = 16777619;
            uint hash = fnvOffset;

            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= fnvPrime;
            }

            return (int)hash;
        }
    }

    SteamLeaderboardSnapshot CreateUnavailableSnapshot(string message)
    {
        return new SteamLeaderboardSnapshot
        {
            isAvailable = false,
            statusMessage = string.IsNullOrWhiteSpace(message) ? "Steam leaderboard unavailable." : message
        };
    }

    void ApplyCommanderSprites(List<SteamLeaderboardEntry> entries, PlayerCharacterData[] commanderRoster)
    {
        if (entries == null)
            return;

        for (int i = 0; i < entries.Count; i++)
        {
            var commander = ResolveCommander(entries[i].commanderHash, commanderRoster);
            if (commander && commander.portrait)
                entries[i].commanderSprite = commander.portrait;
        }
    }

    void InitializeSteamIfAvailable()
    {
#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
        if (_steamInitialized)
            return;

        try
        {
            _steamInitialized = SteamAPI.Init();
            _lastStatus = _steamInitialized ? "Steam leaderboard ready." : "Steam API did not initialize.";

            if (_steamInitialized)
            {
                _findLeaderboardResult = CallResult<LeaderboardFindResult_t>.Create(OnLeaderboardFound);
                _uploadResult = CallResult<LeaderboardScoreUploaded_t>.Create(OnScoreUploaded);
                _globalResult = CallResult<LeaderboardScoresDownloaded_t>.Create();
                _friendsResult = CallResult<LeaderboardScoresDownloaded_t>.Create();
                _currentRankResult = CallResult<LeaderboardScoresDownloaded_t>.Create();
                _avatarLoadedCallback = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);
                FindLeaderboard();
            }
        }
        catch (Exception ex)
        {
            _steamInitialized = false;
            _lastStatus = $"Steam API unavailable: {ex.Message}";
            if (logSteamStatus)
                Debug.LogWarning(_lastStatus);
        }
#endif
    }

    void NotifyAvatarsChanged()
    {
        AvatarsChanged?.Invoke();
    }

#if TETRABEASTS_STEAMWORKS || STEAMWORKS_NET
    void RunWhenLeaderboardReady(Action action)
    {
        if (!_steamInitialized)
            return;

        if (_leaderboardReady)
        {
            action?.Invoke();
            return;
        }

        if (action != null)
            _pendingLeaderboardActions.Enqueue(action);

        FindLeaderboard();
    }

    void FindLeaderboard()
    {
        if (!_steamInitialized || _leaderboardReady || _leaderboardFindInFlight)
            return;

        _leaderboardFindInFlight = true;
        var handle = SteamUserStats.FindOrCreateLeaderboard(
            leaderboardName,
            ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending,
            ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric);

        _findLeaderboardResult.Set(handle);
    }

    void OnLeaderboardFound(LeaderboardFindResult_t result, bool ioFailure)
    {
        _leaderboardFindInFlight = false;

        if (ioFailure || result.m_bLeaderboardFound == 0)
        {
            _lastStatus = $"Steam leaderboard '{leaderboardName}' was not found.";
            if (logSteamStatus)
                Debug.LogWarning(_lastStatus);
            return;
        }

        _leaderboard = result.m_hSteamLeaderboard;
        _leaderboardReady = true;
        _lastStatus = "Steam leaderboard ready.";

        while (_pendingLeaderboardActions.Count > 0)
            _pendingLeaderboardActions.Dequeue()?.Invoke();
    }

    void OnScoreUploaded(LeaderboardScoreUploaded_t result, bool ioFailure)
    {
        if (ioFailure || result.m_bSuccess == 0)
        {
            _lastStatus = "Steam leaderboard score upload failed.";
            if (logSteamStatus)
                Debug.LogWarning(_lastStatus);
            return;
        }

        _lastStatus = result.m_bScoreChanged != 0
            ? $"Steam leaderboard updated. New global rank: #{result.m_nGlobalRankNew}."
            : $"Steam leaderboard kept existing best. Current rank: #{result.m_nGlobalRankNew}.";
    }

    void DownloadEntries(
        ELeaderboardDataRequest request,
        int start,
        int end,
        CallResult<LeaderboardScoresDownloaded_t> callResult,
        Action<List<SteamLeaderboardEntry>> onDownloaded)
    {
        var handle = SteamUserStats.DownloadLeaderboardEntries(_leaderboard, request, start, end);
        callResult.Set(handle, (result, ioFailure) =>
        {
            if (ioFailure)
            {
                onDownloaded?.Invoke(new List<SteamLeaderboardEntry>());
                return;
            }

            onDownloaded?.Invoke(ReadEntries(result));
        });
    }

    List<SteamLeaderboardEntry> ReadEntries(LeaderboardScoresDownloaded_t result)
    {
        var entries = new List<SteamLeaderboardEntry>();
        ulong localSteamId = SteamUser.GetSteamID().m_SteamID;

        for (int i = 0; i < result.m_cEntryCount; i++)
        {
            var details = new int[1];
            if (!SteamUserStats.GetDownloadedLeaderboardEntry(
                    result.m_hSteamLeaderboardEntries,
                    i,
                    out var steamEntry,
                    details,
                    details.Length))
                continue;

            ulong steamId = steamEntry.m_steamIDUser.m_SteamID;
            var entry = new SteamLeaderboardEntry
            {
                rank = steamEntry.m_nGlobalRank,
                score = steamEntry.m_nScore,
                steamId = steamId,
                commanderHash = details.Length > 0 ? details[0] : 0,
                playerName = SteamFriends.GetFriendPersonaName(steamEntry.m_steamIDUser),
                isLocalPlayer = steamId == localSteamId,
                avatarSprite = GetAvatarSprite(steamId)
            };

            if (string.IsNullOrWhiteSpace(entry.playerName))
                entry.playerName = entry.isLocalPlayer ? SteamFriends.GetPersonaName() : "Unknown";

            entries.Add(entry);
        }

        return entries;
    }

    void MergeFriendAndLocalRows(SteamLeaderboardSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        ulong localSteamId = SteamUser.GetSteamID().m_SteamID;
        SteamLeaderboardEntry localEntry = null;

        for (int i = 0; i < snapshot.currentRank.Count; i++)
        {
            if (snapshot.currentRank[i].steamId == localSteamId)
            {
                localEntry = snapshot.currentRank[i];
                break;
            }
        }

        if (localEntry != null)
        {
            bool alreadyIncluded = false;
            for (int i = 0; i < snapshot.friends.Count; i++)
            {
                if (snapshot.friends[i].steamId == localSteamId)
                {
                    alreadyIncluded = true;
                    break;
                }
            }

            if (!alreadyIncluded)
                snapshot.friends.Add(localEntry);
        }

        snapshot.friends.Sort((a, b) =>
        {
            int scoreCompare = b.score.CompareTo(a.score);
            return scoreCompare != 0 ? scoreCompare : a.rank.CompareTo(b.rank);
        });
    }

    Sprite TryCacheAvatarSprite(CSteamID steamId)
    {
        ulong id = steamId.m_SteamID;
        if (_avatarSprites.TryGetValue(id, out var existing))
            return existing;

        int image = SteamFriends.GetMediumFriendAvatar(steamId);
        if (image <= 0)
            return null;

        if (!SteamUtils.GetImageSize(image, out uint width, out uint height) || width == 0 || height == 0)
            return null;

        var rgba = new byte[width * height * 4];
        if (!SteamUtils.GetImageRGBA(image, rgba, rgba.Length))
            return null;

        FlipImageVertically(rgba, (int)width, (int)height);

        var texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false, true)
        {
            name = $"SteamAvatar_{id}"
        };
        texture.LoadRawTextureData(rgba);
        texture.Apply();

        var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        _avatarSprites[id] = sprite;
        return sprite;
    }

    void OnAvatarImageLoaded(AvatarImageLoaded_t data)
    {
        if (TryCacheAvatarSprite(data.m_steamID) != null)
            NotifyAvatarsChanged();
    }

    static void FlipImageVertically(byte[] rgba, int width, int height)
    {
        int stride = width * 4;
        var row = new byte[stride];

        for (int y = 0; y < height / 2; y++)
        {
            int top = y * stride;
            int bottom = (height - 1 - y) * stride;

            Buffer.BlockCopy(rgba, top, row, 0, stride);
            Buffer.BlockCopy(rgba, bottom, rgba, top, stride);
            Buffer.BlockCopy(row, 0, rgba, bottom, stride);
        }
    }
#endif
}
