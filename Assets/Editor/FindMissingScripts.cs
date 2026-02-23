using UnityEditor;
using UnityEngine;

public static class FindMissingScripts
{
    [MenuItem("Tools/Tetrabeasts/Find Missing Scripts In Open Scenes")]
    public static void FindInOpenScenes()
    {
        int missingCount = 0;

        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in all)
        {
            if (go == null) continue;

            // skip assets/prefabs not in scene view
            if (EditorUtility.IsPersistent(go)) continue;

            var comps = go.GetComponents<Component>();
            for (int i = 0; i < comps.Length; i++)
            {
                if (comps[i] == null)
                {
                    missingCount++;
                    Debug.LogError($"Missing script on GameObject: {GetHierarchyPath(go)}", go);
                }
            }
        }

        Debug.Log($"FindMissingScripts: found {missingCount} missing script component(s) in open scenes.");
    }

    static string GetHierarchyPath(GameObject go)
    {
        string path = go.name;
        Transform t = go.transform;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}