using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class SpecialAbilityPopup : MonoBehaviour
{
    const string DefaultCharacterAnimationName = "SpecialAbility_Animation";
    const string DefaultDimmerPanelName = "Dimmer_Panel";
    const string RuntimeSlideContentRootName = "SlideContent_Root";

    [SerializeField] string characterAnimationName = DefaultCharacterAnimationName;
    [SerializeField] string dimmerPanelName = DefaultDimmerPanelName;
    [SerializeField, Min(0.01f)] float fallbackDurationSeconds = 1.25f;
    [SerializeField, Min(0f)] float completionBufferSeconds = 0.05f;
    [SerializeField, Min(0f)] float characterAnimationLastFrameHoldOffsetSeconds = 0.03f;
    [SerializeField, Min(0.1f)] float maxDurationSeconds = 8f;
    [SerializeField, Min(0f)] float dimmerFadeSeconds = 0.18f;
    [SerializeField, Min(0f)] float slideInSeconds = 0.35f;
    [SerializeField, Min(0f)] float slideOutSeconds = 0.55f;
    [SerializeField, Min(0f)] float loopSfxFadeOutLeadSeconds = 1f;
    [SerializeField, Min(0f)] float characterAnimationStartDelaySeconds = 0f;
    [SerializeField, Min(0f)] float slideInOffsetPixels = 0f;

    Animator characterAnimator;
    Animator[] popupAnimators;
    Transform characterAnimationRoot;
    Transform dimmerTransform;
    RectTransform slideContentRoot;
    readonly List<GraphicFadeTarget> dimmerFadeTargets = new();
    readonly List<RectTransform> slideRects = new();
    readonly List<Vector2> slideEndPositions = new();

    struct GraphicFadeTarget
    {
        public Graphic graphic;
        public float targetAlpha;
    }

    public void Prepare(PlayerCharacterData characterData)
    {
        Configure(characterData);
        PrepareIntroState();
    }

    public void ResetIntroState()
    {
        PrepareIntroState();
    }

    public IEnumerator PlayPrepared(
        System.Action onCharacterAnimationStart = null,
        System.Action onSlideInComplete = null,
        System.Action<float> onClosingStarted = null)
    {
        StartGenericAnimators();

        yield return PlayIntro();

        onSlideInComplete?.Invoke();

        if (characterAnimationStartDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(characterAnimationStartDelaySeconds);

        onCharacterAnimationStart?.Invoke();
        StartCharacterAnimator();

        yield return WaitForCharacterAnimation();
        yield return PlayOutro(onClosingStarted);
    }

    public IEnumerator Play(
        PlayerCharacterData characterData,
        System.Action onCharacterAnimationStart = null,
        System.Action onSlideInComplete = null,
        System.Action<float> onClosingStarted = null)
    {
        Prepare(characterData);
        yield return PlayPrepared(onCharacterAnimationStart, onSlideInComplete, onClosingStarted);
    }

    void Configure(PlayerCharacterData characterData)
    {
        characterAnimator = null;
        popupAnimators = null;
        characterAnimationRoot = null;
        dimmerTransform = null;
        dimmerFadeTargets.Clear();
        slideRects.Clear();
        slideEndPositions.Clear();

        GameObject animationPrefab = characterData ? characterData.specialAbilityAnimationPrefab : null;
        if (animationPrefab)
            ReplaceCharacterAnimation(animationPrefab);
        else
            characterAnimator = FindCharacterAnimator();

        PrepareAnimators();
    }

    void ReplaceCharacterAnimation(GameObject animationPrefab)
    {
        Transform slot = FindDeepChild(transform, characterAnimationName);
        if (!slot)
        {
            Debug.LogWarning($"SpecialAbilityPopup: Could not find child '{characterAnimationName}' to replace.");
            return;
        }

        Transform parent = slot.parent;
        int siblingIndex = slot.GetSiblingIndex();
        Vector3 localPosition = slot.localPosition;
        Quaternion localRotation = slot.localRotation;
        Vector3 localScale = slot.localScale;

        RectTransform oldRect = slot as RectTransform;
        bool copyRect = oldRect != null;
        Vector2 anchorMin = copyRect ? oldRect.anchorMin : default;
        Vector2 anchorMax = copyRect ? oldRect.anchorMax : default;
        Vector2 pivot = copyRect ? oldRect.pivot : default;
        Vector2 sizeDelta = copyRect ? oldRect.sizeDelta : default;
        Vector3 anchoredPosition = copyRect ? oldRect.anchoredPosition3D : default;

        slot.gameObject.SetActive(false);
        Destroy(slot.gameObject);

        GameObject instance = Instantiate(animationPrefab, parent, false);
        instance.SetActive(true);
        instance.name = characterAnimationName;
        instance.transform.SetSiblingIndex(siblingIndex);
        characterAnimationRoot = instance.transform;

        if (copyRect && instance.TryGetComponent(out RectTransform newRect))
        {
            newRect.anchorMin = anchorMin;
            newRect.anchorMax = anchorMax;
            newRect.pivot = pivot;
            newRect.sizeDelta = sizeDelta;
            newRect.anchoredPosition3D = anchoredPosition;
            newRect.localRotation = localRotation;
            newRect.localScale = localScale;
        }
        else
        {
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            instance.transform.localScale = localScale;
        }

        characterAnimator = instance.GetComponentInChildren<Animator>(true);
    }

    Animator FindCharacterAnimator()
    {
        Transform slot = FindDeepChild(transform, characterAnimationName);
        characterAnimationRoot = slot;
        return slot ? slot.GetComponentInChildren<Animator>(true) : null;
    }

    void PrepareAnimators()
    {
        popupAnimators = GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < popupAnimators.Length; i++)
        {
            Animator animator = popupAnimators[i];
            if (!animator) continue;

            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.speed = 0f;

            if (animator.runtimeAnimatorController)
            {
                animator.Rebind();
                animator.Update(0f);
            }
        }
    }

    void PrepareIntroState()
    {
        Canvas.ForceUpdateCanvases();

        dimmerTransform = FindDeepChild(transform, dimmerPanelName);
        PrepareDimmerFadeTargets();
        PrepareSlideTargets();
    }

    void PrepareDimmerFadeTargets()
    {
        dimmerFadeTargets.Clear();
        if (!dimmerTransform)
            return;

        Graphic[] graphics = dimmerTransform.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (!graphic) continue;

            Color color = graphic.color;
            dimmerFadeTargets.Add(new GraphicFadeTarget
            {
                graphic = graphic,
                targetAlpha = color.a
            });

            color.a = 0f;
            graphic.color = color;
        }
    }

    void PrepareSlideTargets()
    {
        slideRects.Clear();
        slideEndPositions.Clear();

        slideContentRoot = EnsureSlideContentRoot();
        if (!slideContentRoot)
            return;

        slideContentRoot.anchoredPosition = Vector2.zero;
        slideRects.Add(slideContentRoot);
        slideEndPositions.Add(Vector2.zero);

        ApplyContentSlide(0f);
    }

    RectTransform EnsureSlideContentRoot()
    {
        RectTransform root = transform as RectTransform;
        if (!root)
            return null;

        Transform existing = root.Find(RuntimeSlideContentRootName);
        RectTransform contentRoot = existing as RectTransform;
        if (!contentRoot)
        {
            var go = new GameObject(RuntimeSlideContentRootName, typeof(RectTransform));
            contentRoot = go.GetComponent<RectTransform>();
            contentRoot.SetParent(root, false);
        }

        contentRoot.anchorMin = Vector2.zero;
        contentRoot.anchorMax = Vector2.one;
        contentRoot.offsetMin = Vector2.zero;
        contentRoot.offsetMax = Vector2.zero;
        contentRoot.pivot = new Vector2(0.5f, 0.5f);
        contentRoot.localRotation = Quaternion.identity;
        contentRoot.localScale = Vector3.one;
        contentRoot.anchoredPosition = Vector2.zero;

        var childrenToMove = new List<Transform>();
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (!child || child == dimmerTransform || child == contentRoot)
                continue;

            childrenToMove.Add(child);
        }

        for (int i = 0; i < childrenToMove.Count; i++)
            childrenToMove[i].SetParent(contentRoot, false);

        contentRoot.SetAsLastSibling();
        return contentRoot;
    }

    IEnumerator PlayIntro()
    {
        float dimmerDuration = dimmerFadeTargets.Count > 0 ? Mathf.Max(0f, dimmerFadeSeconds) : 0f;
        float slideDuration = slideRects.Count > 0 ? Mathf.Max(0f, slideInSeconds) : 0f;
        float duration = Mathf.Max(dimmerDuration, slideDuration);

        if (duration <= 0f)
        {
            ApplyDimmerAlpha(1f);
            ApplyContentSlide(1f);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;

            if (dimmerFadeTargets.Count > 0)
            {
                float dimmerT = dimmerDuration <= 0f ? 1f : Mathf.Clamp01(t / dimmerDuration);
                ApplyDimmerAlpha(dimmerT);
            }

            if (slideRects.Count > 0)
            {
                float slideT = slideDuration <= 0f ? 1f : Mathf.Clamp01(t / slideDuration);
                ApplyContentSlide(Mathf.SmoothStep(0f, 1f, slideT));
            }

            yield return null;
        }

        ApplyDimmerAlpha(1f);
        ApplyContentSlide(1f);
    }

    IEnumerator PlayOutro(System.Action<float> onClosingStarted)
    {
        float fadeDuration = Mathf.Max(0f, loopSfxFadeOutLeadSeconds);
        onClosingStarted?.Invoke(fadeDuration);

        float preSlideDelay = Mathf.Max(0f, fadeDuration - slideOutSeconds);
        if (preSlideDelay > 0f)
            yield return new WaitForSecondsRealtime(preSlideDelay);

        if (slideRects.Count == 0)
        {
            float remainingFade = Mathf.Max(0f, fadeDuration - preSlideDelay);
            if (remainingFade > 0f)
                yield return new WaitForSecondsRealtime(remainingFade);

            yield break;
        }

        yield return SlideContentOut();
    }

    IEnumerator FadeDimmerIn()
    {
        if (dimmerFadeTargets.Count == 0)
            yield break;

        float duration = Mathf.Max(0f, dimmerFadeSeconds);
        if (duration <= 0f)
        {
            ApplyDimmerAlpha(1f);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            ApplyDimmerAlpha(Mathf.Clamp01(t / duration));
            yield return null;
        }

        ApplyDimmerAlpha(1f);
    }

    void ApplyDimmerAlpha(float normalized)
    {
        for (int i = 0; i < dimmerFadeTargets.Count; i++)
        {
            Graphic graphic = dimmerFadeTargets[i].graphic;
            if (!graphic) continue;

            Color color = graphic.color;
            color.a = Mathf.Lerp(0f, dimmerFadeTargets[i].targetAlpha, normalized);
            graphic.color = color;
        }
    }

    IEnumerator SlideContentIn()
    {
        if (slideRects.Count == 0)
            yield break;

        float duration = Mathf.Max(0f, slideInSeconds);
        if (duration <= 0f)
        {
            ApplyContentSlide(1f);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            ApplyContentSlide(eased);
            yield return null;
        }

        ApplyContentSlide(1f);
    }

    IEnumerator SlideContentOut()
    {
        if (slideRects.Count == 0)
            yield break;

        float duration = Mathf.Max(0f, slideOutSeconds);
        if (duration <= 0f)
        {
            ApplyContentSlide(0f);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            ApplyContentSlide(1f - eased);
            yield return null;
        }

        ApplyContentSlide(0f);
    }

    void ApplyContentSlide(float normalized)
    {
        RectTransform root = transform as RectTransform;
        float width = root && root.rect.width > 1f ? root.rect.width : Screen.width;
        float offset = slideInOffsetPixels > 0f ? slideInOffsetPixels : width;
        Vector2 hiddenOffset = new Vector2(offset, 0f);

        for (int i = 0; i < slideRects.Count; i++)
        {
            RectTransform rect = slideRects[i];
            if (!rect) continue;

            rect.anchoredPosition = Vector2.Lerp(slideEndPositions[i] + hiddenOffset, slideEndPositions[i], normalized);
        }
    }

    void StartGenericAnimators()
    {
        if (popupAnimators == null)
            popupAnimators = GetComponentsInChildren<Animator>(true);

        for (int i = 0; i < popupAnimators.Length; i++)
        {
            Animator animator = popupAnimators[i];
            if (!animator) continue;
            if (IsCharacterAnimator(animator)) continue;

            StartAnimator(animator);
        }
    }

    void StartCharacterAnimator()
    {
        if (characterAnimator)
            StartAnimator(characterAnimator);
    }

    void StartAnimator(Animator animator)
    {
        if (!animator) return;

        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.speed = 1f;

        if (animator.runtimeAnimatorController)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    bool IsCharacterAnimator(Animator animator)
    {
        if (!animator)
            return false;

        if (animator == characterAnimator)
            return true;

        return characterAnimationRoot && animator.transform.IsChildOf(characterAnimationRoot);
    }

    IEnumerator WaitForCharacterAnimation()
    {
        if (!characterAnimator || !characterAnimator.runtimeAnimatorController || characterAnimator.layerCount <= 0)
        {
            yield return new WaitForSecondsRealtime(fallbackDurationSeconds);
            yield break;
        }

        yield return null;

        AnimationClip characterClip = GetCharacterAnimationClip(characterAnimator);
        float duration = Mathf.Min(characterClip ? characterClip.length : fallbackDurationSeconds, maxDurationSeconds);
        float playDuration = Mathf.Max(0f, duration - characterAnimationLastFrameHoldOffsetSeconds);

        if (playDuration > 0f)
            yield return new WaitForSecondsRealtime(playDuration);

        HoldCharacterAnimationOnLastFrame(characterClip);

        if (completionBufferSeconds > 0f)
            yield return new WaitForSecondsRealtime(completionBufferSeconds);
    }

    void HoldCharacterAnimationOnLastFrame(AnimationClip clip)
    {
        if (!characterAnimator)
            return;

        if (characterAnimator.runtimeAnimatorController && characterAnimator.layerCount > 0)
        {
            AnimatorStateInfo state = characterAnimator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash != 0)
            {
                characterAnimator.speed = 1f;
                characterAnimator.Play(state.fullPathHash, 0, 0.999f);
                characterAnimator.Update(0f);
            }
        }

        characterAnimator.speed = 0f;

        if (clip)
            clip.SampleAnimation(characterAnimator.gameObject, Mathf.Max(0f, clip.length - 0.001f));
    }

    AnimationClip GetCharacterAnimationClip(Animator animator)
    {
        AnimatorClipInfo[] activeClips = animator.GetCurrentAnimatorClipInfo(0);
        if (activeClips != null && activeClips.Length > 0)
        {
            AnimationClip activeLongest = null;
            for (int i = 0; i < activeClips.Length; i++)
                if (activeClips[i].clip)
                    if (!activeLongest || activeClips[i].clip.length > activeLongest.length)
                        activeLongest = activeClips[i].clip;

            if (activeLongest)
                return activeLongest;
        }

        RuntimeAnimatorController controller = animator ? animator.runtimeAnimatorController : null;
        if (!controller || controller.animationClips == null || controller.animationClips.Length == 0)
            return null;

        AnimationClip longest = null;
        AnimationClip[] clips = controller.animationClips;
        for (int i = 0; i < clips.Length; i++)
            if (clips[i])
                if (!longest || clips[i].length > longest.length)
                    longest = clips[i];

        return longest;
    }

    static Transform FindDeepChild(Transform root, string targetName)
    {
        if (!root || string.IsNullOrEmpty(targetName))
            return null;

        if (root.name == targetName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), targetName);
            if (found)
                return found;
        }

        return null;
    }
}
