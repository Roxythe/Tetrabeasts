using UnityEngine;

public class PanelOpener : MonoBehaviour
{
    public GameObject panel; // Assign the panel to be opened here

    public void Toggle()
    {
        if (!panel) return;
        UIPanelTransition.SetVisible(panel, !UIPanelTransition.IsVisible(panel));
    }

    public void Open()
    {
        if (panel) UIPanelTransition.Show(panel);
    }

    public void Close()
    {
        if (panel) UIPanelTransition.Hide(panel);
    }
}
