using UnityEngine;

public class PanelOpener : MonoBehaviour
{
    public GameObject panel; // Assign the panel to be opened here

    public void Toggle()
    {
        if (!panel) return;
        panel.SetActive(!panel.activeSelf);
    }
    public void Open() { if (panel) panel.SetActive(true); }
    public void Close() { if (panel) panel.SetActive(false); }
}
