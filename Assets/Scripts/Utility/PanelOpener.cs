using UnityEngine;

public class PanelOpener : MonoBehaviour
{
    public GameObject panel; // Assign the panel to be opened here

    public void Toggle()
    {
        if (!panel) return;
        if (UIPanelTransition.IsAnyTransitioning) return;

        UIPanelTransition.SetVisible(panel, !UIPanelTransition.IsVisible(panel));
    }

    public void Open()
    {
        if (UIPanelTransition.IsAnyTransitioning) return;

        if (panel) UIPanelTransition.Show(panel);
    }

    public void Close()
    {
        if (UIPanelTransition.IsAnyTransitioning) return;

        if (panel) UIPanelTransition.Hide(panel);
    }
}
