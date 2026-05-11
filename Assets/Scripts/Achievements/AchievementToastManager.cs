using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class AchievementToastManager : MonoBehaviour
{
    public static AchievementToastManager I { get; private set; }

    [Header("Data")]
    public AchievementDatabaseSO database;

    [Header("Toast Prefab")]
    public AchievementToastUI toastPrefab;
    public Transform toastParent; // RectTransform under Canvas (bottom-right)

    [Header("Timing")]
    public float showSeconds = 3.5f;
    public float fadeSeconds = 0.25f;

    [Header("Audio")]
    public float unlockSfxVolume = 1f;

    readonly Queue<string> _queue = new();
    bool _processing;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;

        if (database) database.BuildLookup();
    }

    void OnEnable()
    {
        if (PlayerProgress.I != null)
            PlayerProgress.I.AchievementUnlocked += OnAchievementUnlocked;
    }

    void OnDisable()
    {
        if (PlayerProgress.I != null)
            PlayerProgress.I.AchievementUnlocked -= OnAchievementUnlocked;
    }

    void OnAchievementUnlocked(string achievementId)
    {
        if (SteamAchievementService.ShouldUseSteamAchievementNotifications)
            return;

        _queue.Enqueue(achievementId);
        if (!_processing) StartCoroutine(ProcessQueue());
    }

    IEnumerator ProcessQueue()
    {
        _processing = true;

        while (_queue.Count > 0)
        {
            string id = _queue.Dequeue();
            var def = database ? database.Get(id) : null;
            if (def == null) continue;

            // Play SFX
            if (AudioManager.I && AudioManager.I.sfxAchievementUnlocked)
                AudioManager.I.PlayUISFX(AudioManager.I.sfxAchievementUnlocked, unlockSfxVolume);

            // Spawn toast
            var toast = Instantiate(toastPrefab, toastParent);
            toast.gameObject.SetActive(true);

            toast.Set(def);

            // Fade in/out
            var cg = toast.GetComponent<CanvasGroup>();
            if (!cg) cg = toast.gameObject.AddComponent<CanvasGroup>();

            yield return Fade(cg, 0f, 1f, fadeSeconds);
            yield return new WaitForSeconds(showSeconds);
            yield return Fade(cg, 1f, 0f, fadeSeconds);

            Destroy(toast.gameObject);
        }

        _processing = false;
    }

    IEnumerator Fade(CanvasGroup cg, float from, float to, float seconds)
    {
        if (seconds <= 0f) { cg.alpha = to; yield break; }

        cg.alpha = from;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime; // Ignore pause / timescale
            cg.alpha = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        cg.alpha = to;
    }

    public void DEV_ShowToast(string achievementId)
    {
        if (string.IsNullOrEmpty(achievementId)) return;

        if (database) database.BuildLookup();

        _queue.Enqueue(achievementId);
        if (!_processing) StartCoroutine(ProcessQueue());
    }
}
