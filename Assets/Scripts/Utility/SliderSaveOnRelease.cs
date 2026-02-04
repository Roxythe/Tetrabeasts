using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SliderSaveOnRelease : MonoBehaviour, IPointerUpHandler
{
    public Slider slider;

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!slider) slider = GetComponent<Slider>();
        if (!slider) return;

        SettingsStore.SaveCursorScale(slider.value);
    }
}
