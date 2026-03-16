using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UILiquidFillAspectDriver : MonoBehaviour
{
    [SerializeField] Graphic targetGraphic;
    [SerializeField] string aspectProperty = "_Aspect";

    Material _runtimeMat;
    RectTransform _rt;

    void Awake()
    {
        if (!targetGraphic) targetGraphic = GetComponent<Graphic>();
        _rt = targetGraphic ? targetGraphic.rectTransform : GetComponent<RectTransform>();

        if (!targetGraphic) return;

        _runtimeMat = Instantiate(targetGraphic.material);
        targetGraphic.material = _runtimeMat;

        Apply();
    }

    void OnEnable() => Apply();
    void OnRectTransformDimensionsChange() => Apply();

    void Apply()
    {
        if (!_runtimeMat || !_rt) return;

        float w = Mathf.Max(1f, _rt.rect.width);
        float h = Mathf.Max(1f, _rt.rect.height);

        _runtimeMat.SetFloat(aspectProperty, w / h);
    }
}