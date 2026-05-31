using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIButtonTargetVisual : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public enum ControllerArrowPlacement
    {
        None,
        Right,
        Above
    }

    const string FireBorderAnimationName = "FireBorder_Animation";
    const string FireBorderAnimationAltName = "FireBorderAnimation";

    static readonly Dictionary<GameObject, HashSet<UIButtonTargetVisual>> FireBorderClaims = new();
    static Sprite sharedControllerArrowSprite;

    [SerializeField] ControllerArrowPlacement controllerArrowPlacement;
    [SerializeField] Vector2 controllerArrowSize = new Vector2(20f, 16f);
    [SerializeField] float controllerArrowSideOffset = 12f;
    [SerializeField, Min(1f)] float controllerArrowPulseScale = 1.12f;
    [SerializeField, Min(0f)] float controllerArrowPulseSpeed = 2.5f;
    [SerializeField, Min(1f)] float targetPulseScale = 1.08f;
    [SerializeField, Min(0f)] float targetPulseSpeed = 5.75f;
    [SerializeField] Color persistentFireBorderTint = new Color(0.55f, 0.9f, 1f, 1f);

    AudioClip hoverSfx;
    Transform visualSearchRoot;
    GameObject fireBorderAnimation;
    Image controllerArrow;
    bool pointerInside;
    bool selected;
    bool fireBorderClaimed;
    bool persistentFireBorder;
    Graphic[] fireBorderGraphics;
    Color[] fireBorderGraphicBaseColors;
    SpriteRenderer[] fireBorderSpriteRenderers;
    Color[] fireBorderSpriteBaseColors;
    Selectable selectable;
    Vector3 targetBaseScale = Vector3.one;
    bool targetBaseScaleCaptured;
    bool targetPulseActive;
    bool targetCurrentlyTargeted;
    bool targetPulsePlaying;
    float targetPulseStartedAt;

    public static UIButtonTargetVisual Ensure(GameObject target)
    {
        if (!target)
            return null;

        var visual = target.GetComponent<UIButtonTargetVisual>();
        if (!visual)
            visual = target.AddComponent<UIButtonTargetVisual>();

        return visual;
    }

    public void SetPersistentFireBorder(bool active, Color tint)
    {
        persistentFireBorder = active;
        persistentFireBorderTint = tint;
        RefreshVisuals();
    }

    public void Configure(AudioClip hoverClip, bool enableControllerArrow, Transform fireBorderSearchRoot = null)
    {
        Configure(
            hoverClip,
            enableControllerArrow ? ControllerArrowPlacement.Right : ControllerArrowPlacement.None,
            fireBorderSearchRoot);
    }

    public void Configure(
        AudioClip hoverClip,
        ControllerArrowPlacement arrowPlacement,
        Transform fireBorderSearchRoot = null)
    {
        hoverSfx = hoverClip;
        controllerArrowPlacement = arrowPlacement;
        visualSearchRoot = fireBorderSearchRoot ? fireBorderSearchRoot : transform;
        RepositionControllerArrow();
        ResolveFireBorderAnimation();
        RefreshVisuals();
    }

    void Awake()
    {
        selectable = GetComponent<Selectable>();

        if (!visualSearchRoot)
            visualSearchRoot = transform;

        CaptureTargetBaseScale();
        ResolveFireBorderAnimation();
        RefreshVisuals();
    }

    void OnEnable()
    {
        if (!selectable)
            selectable = GetComponent<Selectable>();

        CaptureTargetBaseScale();
        selected = EventSystem.current && EventSystem.current.currentSelectedGameObject == gameObject;
        ResolveFireBorderAnimation();
        RefreshVisuals();
    }

    void OnDisable()
    {
        pointerInside = false;
        selected = false;
        targetCurrentlyTargeted = false;
        targetPulsePlaying = false;
        SetFireBorderDesired(false);
        SetControllerArrowVisible(false);
        RestoreTargetPulseScale();
    }

    void Update()
    {
        bool nowSelected = EventSystem.current && EventSystem.current.currentSelectedGameObject == gameObject;
        if (selected != nowSelected)
            selected = nowSelected;

        if (!selected && !UICursorController.IsPointerTargetMode)
            pointerInside = false;

        RefreshVisuals();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsTargetInteractable())
        {
            ClearTransientTargeting();
            return;
        }

        pointerInside = true;

        if (hoverSfx && AudioManager.I)
            AudioManager.I.PlaySFX(hoverSfx);

        RefreshVisuals();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (!IsTargetInteractable())
        {
            ClearTransientTargeting();
            return;
        }

        if (!UICursorController.IsPointerTargetMode && !selected)
            return;

        pointerInside = true;
        RefreshVisuals();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        RefreshVisuals();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!IsTargetInteractable())
        {
            ClearTransientTargeting();
            return;
        }

        selected = true;
        RefreshVisuals();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
        RefreshVisuals();
    }

    void RefreshVisuals()
    {
        bool canTarget = IsTargetInteractable();
        if (!canTarget)
        {
            pointerInside = false;
            selected = false;
        }

        bool navigationTargeted = selected && UICursorController.IsButtonNavigationMode;
        bool pointerTargeted = pointerInside && UICursorController.IsPointerTargetMode;
        bool showFireBorder = (canTarget && (pointerTargeted || navigationTargeted)) || persistentFireBorder;
        SetFireBorderDesired(showFireBorder);
        ApplyFireBorderTint(showFireBorder && persistentFireBorder);
        UpdateTargetPulse(canTarget && (pointerTargeted || navigationTargeted));

        bool showControllerArrow = canTarget && controllerArrowPlacement != ControllerArrowPlacement.None && navigationTargeted;
        SetControllerArrowVisible(showControllerArrow);
        UpdateControllerArrowPulse(showControllerArrow);
    }

    public void ClearTransientTargeting()
    {
        pointerInside = false;
        selected = false;
        targetCurrentlyTargeted = false;
        targetPulsePlaying = false;
        SetFireBorderDesired(persistentFireBorder);
        SetControllerArrowVisible(false);
        RestoreTargetPulseScale();
    }

    bool IsTargetInteractable()
    {
        if (!selectable)
            selectable = GetComponent<Selectable>();

        return !selectable || selectable.IsInteractable();
    }

    void CaptureTargetBaseScale()
    {
        if (targetBaseScaleCaptured)
            return;

        targetBaseScale = transform.localScale;
        targetBaseScaleCaptured = true;
    }

    void UpdateTargetPulse(bool active)
    {
        CaptureTargetBaseScale();

        if (!active)
        {
            targetCurrentlyTargeted = false;
            targetPulsePlaying = false;
            RestoreTargetPulseScale();
            return;
        }

        if (!targetCurrentlyTargeted)
        {
            targetCurrentlyTargeted = true;
            targetPulsePlaying = targetPulseSpeed > 0f && targetPulseScale > 1f;
            targetPulseStartedAt = Time.unscaledTime;
        }

        if (!active || targetPulseSpeed <= 0f || targetPulseScale <= 1f)
        {
            RestoreTargetPulseScale();
            return;
        }

        if (!targetPulsePlaying)
        {
            RestoreTargetPulseScale();
            return;
        }

        float duration = 1f / Mathf.Max(0.01f, targetPulseSpeed);
        float normalized = (Time.unscaledTime - targetPulseStartedAt) / duration;
        if (normalized >= 1f)
        {
            targetPulsePlaying = false;
            RestoreTargetPulseScale();
            return;
        }

        float t = Mathf.Sin(Mathf.Clamp01(normalized) * Mathf.PI);
        float pulse = Mathf.Lerp(1f, targetPulseScale, t);
        transform.localScale = targetBaseScale * pulse;
        targetPulseActive = true;
    }

    void RestoreTargetPulseScale()
    {
        if (!targetPulseActive || !targetBaseScaleCaptured)
            return;

        transform.localScale = targetBaseScale;
        targetPulseActive = false;
    }

    void ResolveFireBorderAnimation()
    {
        Transform searchRoot = visualSearchRoot ? visualSearchRoot : transform;
        var found = FindDeep(searchRoot, FireBorderAnimationName);
        if (!found)
            found = FindDeep(searchRoot, FireBorderAnimationAltName);

        GameObject next = found ? found.gameObject : null;
        if (next == fireBorderAnimation)
            return;

        ApplyFireBorderTint(false);
        SetFireBorderDesired(false);
        fireBorderAnimation = next;
        ClearFireBorderColorCache();

        if (fireBorderAnimation)
            fireBorderAnimation.SetActive(false);
    }

    void SetFireBorderDesired(bool desired)
    {
        if (!fireBorderAnimation)
            return;

        if (!desired)
            ApplyFireBorderTint(false);

        if (fireBorderClaimed == desired)
        {
            if (desired && !fireBorderAnimation.activeSelf)
                fireBorderAnimation.SetActive(true);
            return;
        }

        fireBorderClaimed = desired;

        if (desired)
        {
            if (!FireBorderClaims.TryGetValue(fireBorderAnimation, out var claims))
            {
                claims = new HashSet<UIButtonTargetVisual>();
                FireBorderClaims[fireBorderAnimation] = claims;
            }

            claims.Add(this);
            fireBorderAnimation.SetActive(true);
            return;
        }

        if (!FireBorderClaims.TryGetValue(fireBorderAnimation, out var existingClaims))
        {
            fireBorderAnimation.SetActive(false);
            return;
        }

        existingClaims.Remove(this);
        if (existingClaims.Count > 0)
            return;

        FireBorderClaims.Remove(fireBorderAnimation);
        fireBorderAnimation.SetActive(false);
    }

    void ApplyFireBorderTint(bool tinted)
    {
        if (!fireBorderAnimation)
            return;

        CacheFireBorderColors();

        if (fireBorderGraphics != null)
        {
            for (int i = 0; i < fireBorderGraphics.Length; i++)
            {
                var graphic = fireBorderGraphics[i];
                if (!graphic)
                    continue;

                Color color = tinted ? persistentFireBorderTint : fireBorderGraphicBaseColors[i];
                color.a = fireBorderGraphicBaseColors[i].a;
                graphic.color = color;
            }
        }

        if (fireBorderSpriteRenderers != null)
        {
            for (int i = 0; i < fireBorderSpriteRenderers.Length; i++)
            {
                var spriteRenderer = fireBorderSpriteRenderers[i];
                if (!spriteRenderer)
                    continue;

                Color color = tinted ? persistentFireBorderTint : fireBorderSpriteBaseColors[i];
                color.a = fireBorderSpriteBaseColors[i].a;
                spriteRenderer.color = color;
            }
        }
    }

    void CacheFireBorderColors()
    {
        if (!fireBorderAnimation || fireBorderGraphics != null || fireBorderSpriteRenderers != null)
            return;

        fireBorderGraphics = fireBorderAnimation.GetComponentsInChildren<Graphic>(true);
        fireBorderGraphicBaseColors = new Color[fireBorderGraphics.Length];
        for (int i = 0; i < fireBorderGraphics.Length; i++)
            fireBorderGraphicBaseColors[i] = fireBorderGraphics[i] ? fireBorderGraphics[i].color : Color.white;

        fireBorderSpriteRenderers = fireBorderAnimation.GetComponentsInChildren<SpriteRenderer>(true);
        fireBorderSpriteBaseColors = new Color[fireBorderSpriteRenderers.Length];
        for (int i = 0; i < fireBorderSpriteRenderers.Length; i++)
            fireBorderSpriteBaseColors[i] = fireBorderSpriteRenderers[i] ? fireBorderSpriteRenderers[i].color : Color.white;
    }

    void ClearFireBorderColorCache()
    {
        fireBorderGraphics = null;
        fireBorderGraphicBaseColors = null;
        fireBorderSpriteRenderers = null;
        fireBorderSpriteBaseColors = null;
    }

    void SetControllerArrowVisible(bool visible)
    {
        if (visible)
            EnsureControllerArrow();

        if (controllerArrow && controllerArrow.gameObject.activeSelf != visible)
            controllerArrow.gameObject.SetActive(visible);

        if (!visible && controllerArrow)
            controllerArrow.rectTransform.localScale = Vector3.one;
    }

    void UpdateControllerArrowPulse(bool visible)
    {
        if (!visible || !controllerArrow)
            return;

        float pulse = 1f;
        if (controllerArrowPulseSpeed > 0f && controllerArrowPulseScale > 1f)
        {
            float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * controllerArrowPulseSpeed * Mathf.PI * 2f);
            pulse = Mathf.Lerp(1f, controllerArrowPulseScale, t);
        }

        controllerArrow.rectTransform.localScale = Vector3.one * pulse;
    }

    void EnsureControllerArrow()
    {
        if (controllerArrow)
            return;

        var go = new GameObject("ControllerTarget_Arrow", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);

        controllerArrow = go.GetComponent<Image>();
        controllerArrow.raycastTarget = false;
        controllerArrow.sprite = GetControllerArrowSprite();
        controllerArrow.color = Color.white;

        RepositionControllerArrow();
        go.SetActive(false);
    }

    void RepositionControllerArrow()
    {
        if (!controllerArrow)
            return;

        var rt = controllerArrow.rectTransform;
        rt.sizeDelta = controllerArrowSize;
        rt.localScale = Vector3.one;

        if (controllerArrowPlacement == ControllerArrowPlacement.Above)
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, controllerArrowSideOffset);
            rt.localEulerAngles = Vector3.zero;
            return;
        }

        rt.anchorMin = new Vector2(1f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(controllerArrowSideOffset, 0f);
        rt.localEulerAngles = new Vector3(0f, 0f, -90f);
    }

    static Sprite GetControllerArrowSprite()
    {
        if (sharedControllerArrowSprite)
            return sharedControllerArrowSprite;

        const int width = 20;
        const int height = 16;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "Runtime_RedSliderSelectionArrow",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color red = new Color(1f, 0.04f, 0.02f, 1f);
        Color edge = new Color(0.35f, 0f, 0f, 1f);
        float center = (width - 1) * 0.5f;

        for (int y = 0; y < height; y++)
        {
            float halfWidth = Mathf.Lerp(1f, center, y / (float)(height - 1));
            for (int x = 0; x < width; x++)
            {
                float distance = Mathf.Abs(x - center);
                if (distance > halfWidth)
                {
                    texture.SetPixel(x, y, clear);
                    continue;
                }

                texture.SetPixel(x, y, distance > halfWidth - 1.5f ? edge : red);
            }
        }

        texture.Apply();
        sharedControllerArrowSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0f),
            100f);
        sharedControllerArrowSprite.name = "Runtime_RedSliderSelectionArrow";
        return sharedControllerArrowSprite;
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (!root)
            return null;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
                return child;
        }

        return null;
    }
}
