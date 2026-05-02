using UnityEngine;

[System.Serializable]
public class SteamLeaderboardEntry
{
    public int rank;
    public string playerName;
    public int score;
    public ulong steamId;
    public int commanderHash;
    public bool isLocalPlayer;
    public Sprite avatarSprite;
    public Sprite commanderSprite;

    public bool HasScore => rank > 0 || score > 0;
}

