using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonsterButtonView : MonoBehaviour
{
    public Image portraitImage;

    public TMP_Text nameText;
    public TMP_Text nameShadowText;

    public GameObject levelBg;
    public TMP_Text levelText;
    public TMP_Text levelShadowText;

    [SerializeField, HideInInspector] bool levelBgDefaultActive;

    public bool LevelBgDefaultActive => levelBgDefaultActive;

    void Awake()
    {
        if (levelBg != null)
            levelBgDefaultActive = levelBg.activeSelf;
    }
}