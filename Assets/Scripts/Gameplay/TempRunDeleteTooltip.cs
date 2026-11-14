using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class TempRunDeleteTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler, IPointerClickHandler, ISubmitHandler
{
    [SerializeField] RectTransform panel;
    [SerializeField] TMP_Text description;
    [SerializeField] float width = 620f;
    const float Padding = 16f;
    Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        Hide();
    }

    void OnDisable() => Hide();

    void LateUpdate()
    {
        if (panel && panel.gameObject.activeSelf && (!button || !button.IsInteractable()))
            Hide();
    }

    public void OnPointerEnter(PointerEventData eventData) => Show();
    public void OnPointerExit(PointerEventData eventData) => Hide();
    public void OnSelect(BaseEventData eventData) => Show();
    public void OnDeselect(BaseEventData eventData) => Hide();
    public void OnPointerClick(PointerEventData eventData) => Hide();
    public void OnSubmit(BaseEventData eventData) => Hide();

    void Show()
    {
        if (!button || !button.IsInteractable() || !panel || !description)
            return;

        var parent = panel.parent as RectTransform;
        var target = transform as RectTransform;
        if (!parent || !target)
            return;

        // The scene owns the artwork and copy; fit the panel to the wrapped text.
        Canvas.ForceUpdateCanvases();
        float panelWidth = Mathf.Min(width, parent.rect.width - Padding * 2f);
        float textWidth = Mathf.Max(1f, panelWidth - Padding * 2f);
        float panelHeight = description.GetPreferredValues(description.text, textWidth, float.PositiveInfinity).y
            + Padding * 2f;
        panel.sizeDelta = new Vector2(panelWidth, panelHeight);

        Vector3 right = parent.InverseTransformPoint(target.TransformPoint(
            new Vector3(target.rect.xMax, target.rect.center.y, 0f)));
        float x = right.x + Padding;
        float y = right.y + panelHeight * 0.5f;
        x = Mathf.Clamp(x, parent.rect.xMin + Padding,
            Mathf.Max(parent.rect.xMin + Padding, parent.rect.xMax - panelWidth - Padding));
        y = Mathf.Clamp(y, Mathf.Min(parent.rect.yMax - Padding, parent.rect.yMin + panelHeight + Padding),
            parent.rect.yMax - Padding);
        panel.localPosition = new Vector3(x, y, 0f);
        panel.SetAsLastSibling();
        panel.gameObject.SetActive(true);
    }

    void Hide()
    {
        if (panel)
            panel.gameObject.SetActive(false);
    }
}
