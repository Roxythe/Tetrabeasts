using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class CommanderLevelUpUI : MonoBehaviour
{
    public const float GrowSeconds = .7f;
    public const float HoldSeconds = 2f;
    public TMP_Text label;
    public RadialLightBurst lights;
    public CanvasGroup group;
    [Min(.01f)] public float growSeconds = GrowSeconds;
    [Min(0f)] public float holdSeconds = HoldSeconds;
    [Min(0f)] public float fadeSeconds = .2f;
    [Header("Animation scene preview")]
    public bool loopWhenIdle = true;
    public bool previewInEditMode;
    bool playing;
    double previewStart;

    public static CommanderLevelUpUI Create(Canvas parent, TMP_FontAsset font)
    {
        var assets = GameModeAssets.Load();
        if (!assets || !assets.commanderLevelUpPrefab)
        {
            Debug.LogError("Assign the Commander Level Up prefab in GameModeAssets.");
            return null;
        }
        var view = Instantiate(assets.commanderLevelUpPrefab, parent.transform, false);
        if (view.label && !view.label.font && font) view.label.font = font;
        view.gameObject.SetActive(false);
        return view;
    }

    void OnEnable()
    {
        previewStart = Time.realtimeSinceStartupAsDouble;
        if (!Application.isPlaying) Present(growSeconds);
    }

    void Update()
    {
        if (playing || !loopWhenIdle || (!Application.isPlaying && !previewInEditMode)) return;
        float elapsed = (float)(Time.realtimeSinceStartupAsDouble - previewStart);
        Present(elapsed % Mathf.Max(.01f, growSeconds + holdSeconds + fadeSeconds));
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            UnityEditor.SceneView.RepaintAll();
        }
#endif
    }

    public IEnumerator Play(MarathonGaugeUI gauge = null)
    {
        if (lights)
        {
            int nextSeed = System.Guid.NewGuid().GetHashCode();
            lights.seed = nextSeed != lights.seed ? nextSeed : unchecked(nextSeed + 1);
        }
        playing = true;
        gameObject.SetActive(true); transform.SetAsLastSibling();
        var assets = GameModeAssets.Load();
        if (AudioManager.I && assets && assets.commanderLevelUpSfx)
            AudioManager.I.PlayUISFX(assets.commanderLevelUpSfx, jitter: false);
        var flash = gauge ? gauge.FlashLevelUp(growSeconds + holdSeconds + fadeSeconds) : null;
        try
        {
            for (float elapsed = 0f; elapsed < growSeconds + holdSeconds + fadeSeconds; elapsed += Time.unscaledDeltaTime)
            {
                flash?.MoveNext();
                Present(elapsed);
                yield return null;
            }
        }
        finally
        {
            (flash as System.IDisposable)?.Dispose();
            playing = false;
            if (this) gameObject.SetActive(false);
        }
    }

    void Present(float elapsed)
    {
        if (!label || !lights || !group) return;
        group.alpha = elapsed <= growSeconds + holdSeconds ? 1f :
            1f - Mathf.Clamp01((elapsed - growSeconds - holdSeconds) / Mathf.Max(.01f, fadeSeconds));
        float growth = Mathf.Clamp01(elapsed / Mathf.Max(.01f, growSeconds));
        float scale = Mathf.Lerp(.015f, 1f, 1f - Mathf.Pow(1f - growth, 3f));
        label.rectTransform.localScale = Vector3.one * scale;
        lights.rectTransform.localScale = Vector3.one * scale;
        lights.SetAnimationTime(elapsed);
        label.color = Color.Lerp(new Color(1f, .8f, .15f), Color.white, .5f + .5f * Mathf.Sin(elapsed * Mathf.PI * 4f));
    }
}
