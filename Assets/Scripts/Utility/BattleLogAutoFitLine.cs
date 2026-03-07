using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TMP_Text))]
[RequireComponent(typeof(LayoutElement))]
public sealed class BattleLogAutoFitLine : MonoBehaviour
{
    [SerializeField, Min(0f)] private float extraPadding = 2f;

    private TMP_Text _tmp;
    private LayoutElement _layout;

    private void Awake()
    {
        _tmp = GetComponent<TMP_Text>();
        _layout = GetComponent<LayoutElement>();
    }

    public void Fit(float availableWidth)
    {
        var rt = (RectTransform)transform;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, availableWidth);

        _tmp.ForceMeshUpdate();
        float preferred = _tmp.GetPreferredValues(_tmp.text, availableWidth, 0f).y + extraPadding;

        _layout.minHeight = preferred;
        _layout.preferredHeight = preferred;
    }
}