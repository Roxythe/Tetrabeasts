using UnityEngine;

[CreateAssetMenu(menuName = "Tetrabeasts/Loading Screen Settings")]
public sealed class LoadingScreenSettings : ScriptableObject
{
    public GameObject loadingIconPrefab;
    public Sprite[] backgroundImages;
    public Color backgroundImageTint = Color.white;

    [Min(0f)]
    public float minimumVisibleSeconds = 0.15f;

    [Min(0)]
    public int hideDelayFrames = 1;

    public Vector2 iconMargin = new Vector2(48f, 36f);

    [Range(0f, 1f)]
    public float loadingTextMinAlpha = 0.15f;

    [Min(0.01f)]
    public float loadingTextFadeSeconds = 1.1f;

    [Header("Vignette")]
    public bool vignetteEnabled = true;
    public Color vignetteColor = Color.black;

    [Range(0f, 1f)]
    public float vignetteCenterAlpha = 0f;

    [Range(0f, 1f)]
    public float vignetteEdgeAlpha = 0.9f;

    [Range(0f, 1f)]
    public float vignetteInnerRadius = 0.25f;

    [Range(0.01f, 1.5f)]
    public float vignetteOuterRadius = 1f;

    [Min(4)]
    public int vignetteMeshSubdivisions = 32;
}
