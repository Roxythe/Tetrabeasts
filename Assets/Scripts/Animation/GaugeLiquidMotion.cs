using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public sealed class GaugeLiquidMotion : MonoBehaviour
{
    Slider slider;
    MarathonLiquidSlosh slosh;
    float previous;
    void OnEnable()
    {
        slider = GetComponent<Slider>();
        slosh = slider.fillRect ? slider.fillRect.GetComponent<MarathonLiquidSlosh>() : null;
        previous = slider.normalizedValue;
    }
    void LateUpdate()
    {
        float current = slider.normalizedValue;
        if (slosh && !Mathf.Approximately(current, previous)) slosh.AddImpulse(current - previous);
        previous = current;
    }
}
