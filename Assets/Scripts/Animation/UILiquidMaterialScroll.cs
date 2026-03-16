using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UILiquidMaterialScroll : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Graphic targetGraphic;

    [Header("Shader property names")]
    [SerializeField] string noiseSpeedProperty = "_NoiseSpeed";
    [SerializeField] string bubbleSpeedProperty = "_BubbleSpeed";

    [Header("Animation")]
    [SerializeField] Vector2 noiseSpeed = new(0.18f, 0.05f);
    [SerializeField] Vector2 bubbleSpeed = new(0.00f, 0.45f);

    Material _runtimeMat;

    void Awake()
    {
        if (!targetGraphic) targetGraphic = GetComponent<Graphic>();
        if (!targetGraphic || !targetGraphic.material) return;

        _runtimeMat = Instantiate(targetGraphic.material);
        targetGraphic.material = _runtimeMat;

        Apply();
    }

    void OnEnable() => Apply();

    void Apply()
    {
        if (!_runtimeMat) return;

        _runtimeMat.SetVector(noiseSpeedProperty, new Vector4(noiseSpeed.x, noiseSpeed.y, 0f, 0f));
        _runtimeMat.SetVector(bubbleSpeedProperty, new Vector4(bubbleSpeed.x, bubbleSpeed.y, 0f, 0f));
    }

    void Update()
    {
        if (!_runtimeMat) return;

        _runtimeMat.SetVector(noiseSpeedProperty, new Vector4(noiseSpeed.x, noiseSpeed.y, 0f, 0f));
        _runtimeMat.SetVector(bubbleSpeedProperty, new Vector4(bubbleSpeed.x, bubbleSpeed.y, 0f, 0f));
    }
}