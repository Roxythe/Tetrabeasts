using System.Collections.Generic;

public static class TutorialTestingScope
{
    static readonly HashSet<string> completedThisTestPass = new();

    public static void Reset()
    {
        completedThisTestPass.Clear();
    }

    public static bool WasCompletedThisTestPass(string tutorialKey)
    {
        return !string.IsNullOrWhiteSpace(tutorialKey) && completedThisTestPass.Contains(tutorialKey);
    }

    public static void MarkCompletedThisTestPass(string tutorialKey)
    {
        if (!string.IsNullOrWhiteSpace(tutorialKey))
            completedThisTestPass.Add(tutorialKey);
    }

    public static bool MatchesFilter(string tutorialKey, string filter)
    {
        if (string.IsNullOrWhiteSpace(tutorialKey))
            return false;

        if (string.IsNullOrWhiteSpace(filter))
            return true;

        var ids = filter.Split(',', ';', '\n', '\r');
        for (int i = 0; i < ids.Length; i++)
        {
            string id = ids[i]?.Trim();
            if (string.IsNullOrEmpty(id))
                continue;

            if (id == "*" || string.Equals(id, tutorialKey, System.StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
