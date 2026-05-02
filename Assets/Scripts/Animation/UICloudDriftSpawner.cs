using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class UICloudDriftSpawner : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] RectTransform animationPanel;
    [SerializeField] bool addRectMaskIfMissing = true;

    [Header("Cloud Template")]
    [SerializeField] Image cloudImageTemplate;
    [SerializeField] Sprite[] cloudSprites;
    [SerializeField] bool preserveSpriteAspect = true;
    [SerializeField] bool hideTemplateOnPlay = true;
    [SerializeField] bool fitOversizedCloudsVertically = true;

    [Header("Spawn Timing")]
    [SerializeField] bool spawnImmediately = true;
    [SerializeField] Vector2 spawnIntervalRange = new(3f, 6f);

    [Header("Motion")]
    [SerializeField] Vector2 speedRange = new(12f, 28f);
    [SerializeField, Min(0f)] float horizontalSpawnPadding = 8f;
    [SerializeField, Min(0f)] float verticalPadding = 0f;
    [SerializeField] bool useUnscaledTime = true;
    [SerializeField] bool clearCloudsOnDisable = true;

    readonly List<GameObject> _spawnedClouds = new();
    bool _warnedInvalidTemplate;

    void Reset()
    {
        animationPanel = (RectTransform)transform;
    }

    void Awake()
    {
        EnsurePanel();
        EnsureMask();
        HideTemplate();
    }

    void OnEnable()
    {
        EnsurePanel();
        EnsureMask();
        HideTemplate();

        StartCoroutine(SpawnLoop());
    }

    void OnDisable()
    {
        StopAllCoroutines();

        if (clearCloudsOnDisable)
            ClearSpawnedClouds();
    }

    void OnValidate()
    {
        spawnIntervalRange.x = Mathf.Max(0.01f, spawnIntervalRange.x);
        spawnIntervalRange.y = Mathf.Max(spawnIntervalRange.x, spawnIntervalRange.y);

        speedRange.x = Mathf.Max(0.01f, speedRange.x);
        speedRange.y = Mathf.Max(speedRange.x, speedRange.y);
    }

    public void SpawnCloud()
    {
        if (!animationPanel || !HasUsableTemplate() || cloudSprites == null || cloudSprites.Length == 0)
            return;

        Rect panelRect = animationPanel.rect;
        if (panelRect.width <= 0f || panelRect.height <= 0f)
            return;

        Sprite cloudSprite = cloudSprites[Random.Range(0, cloudSprites.Length)];
        if (!cloudSprite)
            return;

        Image cloudImage = Instantiate(cloudImageTemplate, animationPanel);
        RectTransform cloudRect = cloudImage.rectTransform;
        Vector2 cloudSize = ResolveCloudSize(cloudRect, cloudSprite, panelRect);

        cloudImage.name = $"{cloudImageTemplate.name} Drift";
        cloudImage.sprite = cloudSprite;
        cloudImage.preserveAspect = preserveSpriteAspect;
        cloudImage.raycastTarget = false;

        cloudRect.anchorMin = new Vector2(0.5f, 0.5f);
        cloudRect.anchorMax = new Vector2(0.5f, 0.5f);
        cloudRect.pivot = new Vector2(0.5f, 0.5f);
        cloudRect.localScale = Vector3.one;
        cloudRect.localRotation = Quaternion.identity;
        cloudRect.sizeDelta = cloudSize;

        float startX = (-panelRect.width * 0.5f) - (cloudSize.x * 0.5f) - horizontalSpawnPadding;
        float endX = (panelRect.width * 0.5f) + (cloudSize.x * 0.5f) + horizontalSpawnPadding;
        float y = PickVerticalPosition(panelRect.height, cloudSize.y);

        cloudRect.anchoredPosition = new Vector2(startX, y);
        cloudImage.gameObject.SetActive(true);
        cloudImage.transform.SetSiblingIndex(cloudImageTemplate.transform.GetSiblingIndex());

        _spawnedClouds.Add(cloudImage.gameObject);

        float speed = Random.Range(speedRange.x, speedRange.y);
        StartCoroutine(DriftCloud(cloudRect, endX, speed));
    }

    public void ClearSpawnedClouds()
    {
        for (int i = _spawnedClouds.Count - 1; i >= 0; i--)
        {
            GameObject cloud = _spawnedClouds[i];
            if (cloud)
                Destroy(cloud);
        }

        _spawnedClouds.Clear();
    }

    IEnumerator SpawnLoop()
    {
        yield return null;

        if (spawnImmediately)
            SpawnCloud();

        while (isActiveAndEnabled)
        {
            float delay = Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
            yield return Wait(delay);
            SpawnCloud();
        }
    }

    IEnumerator DriftCloud(RectTransform cloudRect, float endX, float speed)
    {
        while (cloudRect && cloudRect.anchoredPosition.x < endX)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            Vector2 pos = cloudRect.anchoredPosition;
            pos.x = Mathf.Min(endX, pos.x + (speed * dt));
            cloudRect.anchoredPosition = pos;
            yield return null;
        }

        if (cloudRect)
        {
            _spawnedClouds.Remove(cloudRect.gameObject);
            Destroy(cloudRect.gameObject);
        }
    }

    IEnumerator Wait(float seconds)
    {
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

    Vector2 ResolveCloudSize(RectTransform cloudRect, Sprite sprite, Rect panelRect)
    {
        Vector2 size = cloudRect.rect.size;

        if (size.x <= 0f || size.y <= 0f)
            size = cloudRect.sizeDelta;

        if (size.x <= 0f || size.y <= 0f)
            size = sprite.rect.size;

        if (fitOversizedCloudsVertically)
        {
            float maxHeight = Mathf.Max(1f, panelRect.height - (verticalPadding * 2f));
            if (size.y > maxHeight)
                size *= maxHeight / size.y;
        }

        return size;
    }

    float PickVerticalPosition(float panelHeight, float cloudHeight)
    {
        float minY = (-panelHeight * 0.5f) + (cloudHeight * 0.5f) + verticalPadding;
        float maxY = (panelHeight * 0.5f) - (cloudHeight * 0.5f) - verticalPadding;

        if (minY > maxY)
            return 0f;

        return Random.Range(minY, maxY);
    }

    void EnsurePanel()
    {
        if (!animationPanel)
            animationPanel = (RectTransform)transform;
    }

    void EnsureMask()
    {
        if (!addRectMaskIfMissing || !animationPanel)
            return;

        if (!animationPanel.GetComponent<RectMask2D>() && !animationPanel.GetComponent<Mask>())
            animationPanel.gameObject.AddComponent<RectMask2D>();
    }

    void HideTemplate()
    {
        if (!hideTemplateOnPlay || !HasUsableTemplate())
            return;

        if (cloudImageTemplate.gameObject.scene.IsValid())
            cloudImageTemplate.gameObject.SetActive(false);
    }

    bool HasUsableTemplate()
    {
        if (!cloudImageTemplate)
            return false;

        if (animationPanel && cloudImageTemplate.gameObject == animationPanel.gameObject)
        {
            if (!_warnedInvalidTemplate)
            {
                Debug.LogWarning(
                    $"{nameof(UICloudDriftSpawner)} on {name} needs a separate child or prefab Image for the cloud template. The panel Image itself cannot be used as the spawned cloud.",
                    this);
                _warnedInvalidTemplate = true;
            }

            return false;
        }

        return true;
    }
}
