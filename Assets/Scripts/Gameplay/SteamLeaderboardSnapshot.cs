using System.Collections.Generic;

public class SteamLeaderboardSnapshot
{
    public readonly List<SteamLeaderboardEntry> global = new();
    public readonly List<SteamLeaderboardEntry> friends = new();
    public readonly List<SteamLeaderboardEntry> currentRank = new();
    public string statusMessage;
    public bool isAvailable;
}

