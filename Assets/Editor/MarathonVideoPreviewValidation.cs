#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public static class MarathonVideoPreviewValidation
{
    static Scene scene;
    static VideoPlayer player;
    static RenderTexture texture;
    static double deadline;
    static long firstFrame;
    static string error;
    [MenuItem("Tetrabeasts/Validate Village Background Playback")]
    public static void Run()
    {
        if (player || EditorApplication.isPlayingOrWillChangePlaymode) return;
        scene = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("Isolated village video check"); SceneManager.MoveGameObjectToScene(root, scene);
        player = root.AddComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.clip = AssetDatabase.LoadAssetAtPath<CastleData>("Assets/ScriptableObjects/Castle/TestCastle 0.asset").levelBackgroundVideoClip;
        player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        player.playbackSpeed = .66f;
        player.audioOutputMode = VideoAudioOutputMode.None;
        player.renderMode = VideoRenderMode.RenderTexture;
        texture = new RenderTexture(320, 180, 0); player.targetTexture = texture;
        firstFrame = -1; error = null;
        player.errorReceived += OnError;
        player.prepareCompleted += OnPrepared;
        deadline = EditorApplication.timeSinceStartup + 30;
        EditorApplication.update += Tick;
        player.Prepare();
    }
    static void OnPrepared(VideoPlayer video) => video.Play();
    static void OnError(VideoPlayer video, string message) => error = message;
    static void Tick()
    {
        EditorApplication.QueuePlayerLoopUpdate();
        if (player && player.frame >= 0)
        {
            if (firstFrame < 0) firstFrame = player.frame;
            else if (player.frame > firstFrame + 5) { Finish(true); return; }
        }
        if (error != null || EditorApplication.timeSinceStartup > deadline) Finish(false);
    }
    static void Finish(bool success)
    {
        EditorApplication.update -= Tick;
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/MarathonVideo-validation.txt", success ? "PASS: Starting Village background prepared and advanced more than five frames at 0.66x speed.\n" : "FAIL: " + (error ?? "Video did not advance frames in editor preview."));
        if (player) { player.Stop(); player.targetTexture = null; }
        EditorSceneManager.ClosePreviewScene(scene);
        if (texture) { texture.Release(); Object.DestroyImmediate(texture); }
        player = null;
    }
}
#endif
