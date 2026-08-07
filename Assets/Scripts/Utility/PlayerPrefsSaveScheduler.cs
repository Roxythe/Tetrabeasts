using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerPrefsSaveScheduler : MonoBehaviour
{
    static PlayerPrefsSaveScheduler instance;

    [SerializeField, Min(0f)] float saveDebounceSeconds = 0.25f;

    Coroutine saveCoroutine;
    bool cloudUploadQueued;
    float lastQueueTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Application.isPlaying)
            Ensure();
    }

    public static void QueueSave(bool queueCloudUpload = true)
    {
        if (!Application.isPlaying)
        {
            PlayerPrefs.Save();
            if (queueCloudUpload)
                SteamCloudSaveService.QueueUpload();
            return;
        }

        Ensure()?.QueueSaveInternal(queueCloudUpload);
    }

    public static void Flush(bool uploadNow = false)
    {
        if (instance)
            instance.FlushInternal(uploadNow);
        else
            PlayerPrefs.Save();
    }

    static PlayerPrefsSaveScheduler Ensure()
    {
        if (instance)
            return instance;

        var existing = FindFirstObjectByType<PlayerPrefsSaveScheduler>(FindObjectsInactive.Include);
        if (existing)
        {
            instance = existing;
            if (!instance.gameObject.activeSelf)
                instance.gameObject.SetActive(true);
            return instance;
        }

        var go = new GameObject(nameof(PlayerPrefsSaveScheduler));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<PlayerPrefsSaveScheduler>();
        return instance;
    }

    void QueueSaveInternal(bool queueCloudUpload)
    {
        cloudUploadQueued |= queueCloudUpload;
        lastQueueTime = Time.unscaledTime;

        if (!isActiveAndEnabled || saveDebounceSeconds <= 0f)
        {
            FlushInternal(uploadNow: false);
            return;
        }

        if (saveCoroutine == null)
            saveCoroutine = StartCoroutine(SaveAfterDebounce());
    }

    IEnumerator SaveAfterDebounce()
    {
        while (Time.unscaledTime < lastQueueTime + saveDebounceSeconds)
            yield return null;

        saveCoroutine = null;
        FlushInternal(uploadNow: false);
    }

    void FlushInternal(bool uploadNow)
    {
        if (saveCoroutine != null)
            StopCoroutine(saveCoroutine);

        saveCoroutine = null;

        bool shouldUpload = cloudUploadQueued;
        PlayerPrefs.Save();

        cloudUploadQueued = false;

        if (uploadNow)
        {
            SteamCloudSaveService.TryUploadNow();
            return;
        }

        if (!shouldUpload)
            return;

        SteamCloudSaveService.QueueUpload();
    }

    void OnApplicationPause(bool pause)
    {
        if (pause)
            FlushInternal(uploadNow: true);
    }

    void OnApplicationQuit()
    {
        FlushInternal(uploadNow: true);
    }

    void OnDestroy()
    {
        if (instance != this)
            return;

        FlushInternal(uploadNow: false);
        instance = null;
    }
}
