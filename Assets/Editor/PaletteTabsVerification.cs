#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

[InitializeOnLoad]
public static class PaletteTabsVerification
{
    const string Key = "PaletteTabsVerification.Running";
    static TestRunnerApi api;
    static PaletteTabsVerification() { if (SessionState.GetBool(Key, false)) Register(); }
    static void Register()
    {
        api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new Results());
    }
    [MenuItem("Tetrabeasts/Verify Palette and Shared Tabs %#F9")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool(Key, true);
        Register();
        api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode,
            groupNames = new[] { "^GameplayPlaylistTests\\.PlaylistUIContains" } }));
    }
    sealed class Results : ICallbacks
    {
        public void RunStarted(ITestAdaptor tests) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            TestRunnerApi.SaveResultToFile(result, "Logs/SharedTabs-final-tests.xml");
            SessionState.SetBool(Key, false);
            Debug.Log("Palette and shared tabs checks completed: " + result.ResultState);
        }
    }
}
#endif
