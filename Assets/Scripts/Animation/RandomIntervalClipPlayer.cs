using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public sealed class RandomIntervalClipPlayer : MonoBehaviour
{
    [System.Serializable]
    sealed class SpriteFrameSet
    {
        public AnimationClip clip;
        public Sprite[] frames;
        public float[] times;
        public float clipLength;
    }

    [Header("Clips")]
    [SerializeField] GameObject animationRoot;
    [SerializeField] AnimationClip[] clips = new AnimationClip[2];
    [SerializeField] bool playImmediatelyOnEnable = false;
    [SerializeField, HideInInspector] SpriteFrameSet[] spriteFrameSets = new SpriteFrameSet[0];

    [Header("Timing")]
    [SerializeField] Vector2 intervalRangeSeconds = new Vector2(30f, 90f);
    [SerializeField, Min(0.01f)] float playbackSpeed = 1f;
    [SerializeField] bool useUnscaledTime = true;
    [SerializeField, Min(0f)] float finalFrameSampleOffsetSeconds = 0.001f;

    [Header("Animator")]
    [SerializeField] bool disableAnimatorWhileActive = true;
    [SerializeField] Animator animatorToDisableWhileActive;

    Coroutine _loop;
    AnimationClip _activeClip;
    Image _targetImage;
    Graphic _targetGraphic;
    bool _animatorWasDisabledByThis;
    bool _animatorPreviousEnabled;
    bool _warnedMissingClips;

    void Reset()
    {
        animationRoot = gameObject;
        animatorToDisableWhileActive = GetComponent<Animator>();
    }

    void Awake()
    {
        EnsureReferences();
        RebuildSpriteFrameSetsFromClipsInEditor();
    }

    void OnEnable()
    {
        EnsureReferences();
        if (!HasAnyValidClip())
            WarnMissingClipsOnce();

        DisableAnimatorIfNeeded();
        _loop = StartCoroutine(PlayLoop());
    }

    void OnDisable()
    {
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }

        if (_activeClip)
            SampleClip(_activeClip, GetFinalSampleTime(_activeClip));

        _activeClip = null;
        RestoreAnimatorIfNeeded();
    }

    void OnValidate()
    {
        intervalRangeSeconds.x = Mathf.Max(0f, intervalRangeSeconds.x);
        intervalRangeSeconds.y = Mathf.Max(intervalRangeSeconds.x, intervalRangeSeconds.y);
        playbackSpeed = Mathf.Max(0.01f, playbackSpeed);
        finalFrameSampleOffsetSeconds = Mathf.Max(0f, finalFrameSampleOffsetSeconds);
        RebuildSpriteFrameSetsFromClipsInEditor();
    }

    public void SetUseUnscaledTime(bool value)
    {
        useUnscaledTime = value;
    }

    IEnumerator PlayLoop()
    {
        yield return null;

        if (playImmediatelyOnEnable)
            yield return PlayRandomClip();

        while (isActiveAndEnabled)
        {
            yield return Wait(Random.Range(intervalRangeSeconds.x, intervalRangeSeconds.y));
            yield return PlayRandomClip();
        }
    }

    IEnumerator PlayRandomClip()
    {
        AnimationClip clip = PickRandomClip();
        if (!clip)
        {
            WarnMissingClipsOnce();
            yield break;
        }

        _activeClip = clip;
        SpriteFrameSet frameSet = GetSpriteFrameSet(clip);

        if (CanPlaySpriteFrameSet(frameSet))
            yield return PlaySpriteFrameSet(frameSet);
        else
            yield return PlaySampledClip(clip);

        _activeClip = null;
    }

    IEnumerator PlaySpriteFrameSet(SpriteFrameSet frameSet)
    {
        float duration = Mathf.Max(0f, frameSet.clipLength);
        float elapsed = 0f;

        ApplySpriteFrame(frameSet, 0f);

        while (elapsed < duration)
        {
            yield return null;
            elapsed = Mathf.Min(duration, elapsed + (GetDeltaTime() * playbackSpeed));
            ApplySpriteFrame(frameSet, elapsed);
        }

        ApplySpriteFrame(frameSet, duration);
    }

    IEnumerator PlaySampledClip(AnimationClip clip)
    {
        SampleClip(clip, 0f);

        float duration = Mathf.Max(0f, clip.length);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return null;
            elapsed = Mathf.Min(duration, elapsed + (GetDeltaTime() * playbackSpeed));
            SampleClip(clip, elapsed);
        }

        SampleClip(clip, GetFinalSampleTime(clip));
    }

    AnimationClip PickRandomClip()
    {
        if (clips == null || clips.Length == 0)
            return null;

        int validClipCount = 0;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i])
                validClipCount++;
        }

        if (validClipCount == 0)
            return null;

        int pickedClipIndex = Random.Range(0, validClipCount);
        for (int i = 0; i < clips.Length; i++)
        {
            if (!clips[i])
                continue;

            if (pickedClipIndex == 0)
                return clips[i];

            pickedClipIndex--;
        }

        return null;
    }

    IEnumerator Wait(float seconds)
    {
        if (seconds <= 0f)
            yield break;

        if (!useUnscaledTime)
        {
            yield return new WaitForSeconds(seconds);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    void SampleClip(AnimationClip clip, float time)
    {
        if (!clip)
            return;

        GameObject target = animationRoot ? animationRoot : gameObject;
        clip.SampleAnimation(target, Mathf.Clamp(time, 0f, Mathf.Max(0f, clip.length)));

        if (_targetGraphic)
            _targetGraphic.SetAllDirty();
    }

    void ApplySpriteFrame(SpriteFrameSet frameSet, float time)
    {
        if (!CanPlaySpriteFrameSet(frameSet))
            return;

        int frameIndex = GetFrameIndexAtTime(frameSet, time);
        Sprite frame = frameSet.frames[frameIndex];
        if (!frame)
            return;

        _targetImage.sprite = frame;
        _targetImage.SetAllDirty();
    }

    int GetFrameIndexAtTime(SpriteFrameSet frameSet, float time)
    {
        if (frameSet.times == null || frameSet.times.Length == 0)
            return 0;

        int maxFrameIndex = Mathf.Min(frameSet.frames.Length, frameSet.times.Length) - 1;
        for (int i = maxFrameIndex; i >= 0; i--)
        {
            if (time >= frameSet.times[i])
                return i;
        }

        return 0;
    }

    float GetFinalSampleTime(AnimationClip clip)
    {
        return Mathf.Max(0f, clip.length - finalFrameSampleOffsetSeconds);
    }

    float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    void EnsureReferences()
    {
        if (!animationRoot)
            animationRoot = gameObject;

        if (!animatorToDisableWhileActive && animationRoot)
            animatorToDisableWhileActive = animationRoot.GetComponent<Animator>();

        if (!_targetImage && animationRoot)
            _targetImage = animationRoot.GetComponent<Image>();

        if (!_targetGraphic && animationRoot)
            _targetGraphic = animationRoot.GetComponent<Graphic>();
    }

    bool CanPlaySpriteFrameSet(SpriteFrameSet frameSet)
    {
        return _targetImage &&
               frameSet != null &&
               frameSet.frames != null &&
               frameSet.frames.Length > 0;
    }

    SpriteFrameSet GetSpriteFrameSet(AnimationClip clip)
    {
        if (!clip)
            return null;

        if (spriteFrameSets == null || spriteFrameSets.Length == 0)
            RebuildSpriteFrameSetsFromClipsInEditor();

        if (spriteFrameSets == null)
            return null;

        for (int i = 0; i < spriteFrameSets.Length; i++)
        {
            SpriteFrameSet frameSet = spriteFrameSets[i];
            if (frameSet != null && frameSet.clip == clip)
                return frameSet;
        }

        return null;
    }

    bool HasAnyValidClip()
    {
        if (clips == null)
            return false;

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i])
                return true;
        }

        return false;
    }

    void DisableAnimatorIfNeeded()
    {
        if (!disableAnimatorWhileActive || !animatorToDisableWhileActive || _animatorWasDisabledByThis)
            return;

        _animatorPreviousEnabled = animatorToDisableWhileActive.enabled;
        animatorToDisableWhileActive.enabled = false;
        _animatorWasDisabledByThis = true;
    }

    void RestoreAnimatorIfNeeded()
    {
        if (!_animatorWasDisabledByThis || !animatorToDisableWhileActive)
        {
            _animatorWasDisabledByThis = false;
            return;
        }

        animatorToDisableWhileActive.enabled = _animatorPreviousEnabled;
        _animatorWasDisabledByThis = false;
    }

    void WarnMissingClipsOnce()
    {
        if (_warnedMissingClips)
            return;

        Debug.LogWarning($"{nameof(RandomIntervalClipPlayer)} on {name} needs at least one animation clip.", this);
        _warnedMissingClips = true;
    }

#if UNITY_EDITOR
    void RebuildSpriteFrameSetsFromClipsInEditor()
    {
        if (clips == null || clips.Length == 0)
        {
            spriteFrameSets = new SpriteFrameSet[0];
            return;
        }

        var rebuiltFrameSets = new List<SpriteFrameSet>();
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (!clip)
                continue;

            SpriteFrameSet frameSet = CreateSpriteFrameSet(clip);
            if (frameSet != null)
                rebuiltFrameSets.Add(frameSet);
        }

        spriteFrameSets = rebuiltFrameSets.ToArray();
    }

    static SpriteFrameSet CreateSpriteFrameSet(AnimationClip clip)
    {
        EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        for (int i = 0; i < bindings.Length; i++)
        {
            EditorCurveBinding binding = bindings[i];
            if (binding.propertyName != "m_Sprite")
                continue;

            ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            if (keyframes == null || keyframes.Length == 0)
                continue;

            var frames = new List<Sprite>(keyframes.Length);
            var times = new List<float>(keyframes.Length);
            for (int frameIndex = 0; frameIndex < keyframes.Length; frameIndex++)
            {
                if (keyframes[frameIndex].value is not Sprite sprite)
                    continue;

                frames.Add(sprite);
                times.Add(Mathf.Max(0f, keyframes[frameIndex].time));
            }

            if (frames.Count == 0)
                continue;

            return new SpriteFrameSet
            {
                clip = clip,
                frames = frames.ToArray(),
                times = times.ToArray(),
                clipLength = Mathf.Max(clip.length, times[^1])
            };
        }

        return null;
    }
#else
    void RebuildSpriteFrameSetsFromClipsInEditor()
    {
    }
#endif
}
