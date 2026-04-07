using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class RainOverlayUI : MonoBehaviour
{
    [System.Serializable]
    struct DropState
    {
        public RectTransform rect;
        public float x;
        public float y;
        public float speedScale;
    }

    [SerializeField, Range(8, 128)] int dropCount = 40;
    [SerializeField, Range(40f, 320f)] float dropSpeed = 140f;
    [SerializeField, Range(4f, 48f)] float dropLength = 18f;
    [SerializeField, Range(1f, 8f)] float dropWidth = 2f;
    [SerializeField, Range(-50f, 50f)] float dropAngle = -18f;
    [SerializeField] Color dropTint = new Color(0.85f, 0.93f, 1f, 0.55f);

    readonly List<DropState> _drops = new();
    RectTransform _rect;
    RectMask2D _mask;
    static Sprite _onePx;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _mask = GetComponent<RectMask2D>();
        if (!_mask)
            _mask = gameObject.AddComponent<RectMask2D>();

        RebuildDrops();
    }

    void OnEnable()
    {
        if (_drops.Count == 0)
            RebuildDrops();
    }

    void Update()
    {
        if (_rect == null || _drops.Count == 0)
            return;

        Rect r = _rect.rect;
        float halfW = r.width * 0.5f;
        float halfH = r.height * 0.5f;
        float wrapBottom = -halfH - dropLength;
        float wrapTop = halfH + dropLength;

        for (int i = 0; i < _drops.Count; i++)
        {
            DropState drop = _drops[i];
            if (!drop.rect)
                continue;

            drop.y -= dropSpeed * drop.speedScale * Time.deltaTime;
            if (drop.y < wrapBottom)
            {
                drop.y = wrapTop + Random.Range(0f, dropLength * 2f);
                drop.x = Random.Range(-halfW - dropLength, halfW + dropLength);
            }

            drop.rect.anchoredPosition = new Vector2(drop.x, drop.y);
            _drops[i] = drop;
        }
    }

    public void Configure(int count, float speed, float length, float width, float angle, Color tint)
    {
        dropCount = Mathf.Clamp(count, 8, 128);
        dropSpeed = Mathf.Max(10f, speed);
        dropLength = Mathf.Max(4f, length);
        dropWidth = Mathf.Max(1f, width);
        dropAngle = angle;
        dropTint = tint;

        RebuildDrops();
    }

    void RebuildDrops()
    {
        if (_rect == null)
            _rect = GetComponent<RectTransform>();

        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        _drops.Clear();

        Rect r = _rect.rect;
        float halfW = r.width * 0.5f;
        float halfH = r.height * 0.5f;

        for (int i = 0; i < dropCount; i++)
        {
            var go = new GameObject($"RainDrop_{i}", typeof(Image));
            go.transform.SetParent(transform, false);

            var img = go.GetComponent<Image>();
            img.sprite = OnePx();
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
            img.color = dropTint;

            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(dropWidth, dropLength);
            rt.localRotation = Quaternion.Euler(0f, 0f, dropAngle);

            DropState drop = new DropState
            {
                rect = rt,
                x = Random.Range(-halfW - dropLength, halfW + dropLength),
                y = Random.Range(-halfH - dropLength, halfH + dropLength),
                speedScale = Random.Range(0.85f, 1.25f)
            };

            rt.anchoredPosition = new Vector2(drop.x, drop.y);
            _drops.Add(drop);
        }
    }

    static Sprite OnePx()
    {
        if (_onePx)
            return _onePx;

        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply(false, true);
        _onePx = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _onePx;
    }
}
