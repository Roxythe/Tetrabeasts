using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TitleMenuUI : MonoBehaviour
{
    [Header("Scenes")]
    [Tooltip("Exact name of your gameplay scene (as it appears in Build Settings).")]
    public string gameplaySceneName = "GameplayScene";

    [Header("Panels (optional)")]
    public GameObject highScorePanel;   // set if you have a HS panel in the title scene
    public GameObject settingsPanel;    // reuse your Volume panel here if you like
    public MonsterSelectUI monsterSelectPanel;
    public HighScoreUI highScoreUI;
    public CharacterSelectUI characterSelectPanel;

    [SerializeField] CharacterSelectUI characterSelectUI; // drag from scene
    [SerializeField] MonsterSelectUI monsterSelectUI;   // drag from scene


    [Header("Pause While Panels Open?")]
    public bool pauseOnPanels = false;  // usually false for title screen

    [Header("Defaults")]
    public PlayerCharacterData defaultCharacter;
    public MonsterData defaultA;
    public MonsterData defaultB;

    [Header("Audio (Title)")]
    public AudioClip titleBGM;   
    [Range(0f, 1f)] public float titleBGMVolume = 0.5f;

    public AudioClip startGameSFX;
    public AudioClip uiClickSFX; 
    public AudioClip uiHoverSFX;

    void Awake()
    {
        if (characterSelectUI && characterSelectUI.roster != null && characterSelectUI.roster.Length > 0)
        {
            var savedChar = SelectedCharacterStore.ResolveFromRoster(characterSelectUI.roster);
            if (savedChar != null)
                SelectedCharacterStore.Current = savedChar;
            else
                SelectedCharacterStore.Save(characterSelectUI.roster[0]); // write a default once

            characterSelectUI.RefreshPreview();
        }

        if (monsterSelectUI && monsterSelectUI.roster != null && monsterSelectUI.roster.Length > 0)
        {
            var resolved = SelectedMonstersStore.ResolveFromRoster(monsterSelectUI.roster);
            if (resolved != null && resolved.Count >= 2)
            {
                SelectedMonstersStore.Active = resolved;
            }
            else
            {
                var fallbacks = new List<MonsterData>();
                if (monsterSelectUI.roster.Length >= 1 && monsterSelectUI.roster[0]) fallbacks.Add(monsterSelectUI.roster[0]);
                if (monsterSelectUI.roster.Length >= 2 && monsterSelectUI.roster[1]) fallbacks.Add(monsterSelectUI.roster[1]);
                SelectedMonstersStore.Active = fallbacks;
                SelectedMonstersStore.SaveNames(fallbacks); // persist once so it sticks next run
            }

            monsterSelectUI.RefreshAllUI();
        }
    }

    void Start()
    {
        // Start title BGM
        if (AudioManager.I)
        {
            var clip = titleBGM ? titleBGM : AudioManager.I.bgmLoop; // fallback to default loop
            AudioManager.I.PlayMusic(clip, loop: true, vol: titleBGMVolume);
        }

        // Auto-hook all Buttons under this menu for click/hover sounds
        HookAllButtonsForSFX();
    }


    // --- Button hooks ---
    public void OnStartGame()
    {
        // Stop title BGM
        if (AudioManager.I)
        {
            var clip = startGameSFX ? startGameSFX : AudioManager.I.sfxRestart;
            AudioManager.I.PlaySFX(clip);
            AudioManager.I.StopMusic(); // fade/stop title BGM
        }

        // Ensure we have a character before loading Gameplay
        if (SelectedCharacterStore.Current == null)
        {
            if (defaultCharacter != null)
                SelectedCharacterStore.Current = defaultCharacter;
            else if (characterSelectPanel != null &&
                     characterSelectPanel.roster != null &&
                     characterSelectPanel.roster.Length > 0)
                SelectedCharacterStore.Current = characterSelectPanel.roster[0];
            else
                Debug.LogWarning("No character selected and no default/roster configured. Gameplay will use its own fallback if set.");
        }

        if (SelectedMonstersStore.Active.Count < 2)
        {
            // Fall back to the first two defaults
            SelectedMonstersStore.Active = new List<MonsterData> { defaultA, defaultB };
        }

        // Also truncate to 4 if user picked more.
        while (SelectedMonstersStore.Active.Count > 4) SelectedMonstersStore.Active.RemoveAt(SelectedMonstersStore.Active.Count - 1);

        if (AudioManager.I) AudioManager.I.PlaySFX(AudioManager.I.sfxRestart);

        RunModsStore.ResetAll();

        if (!string.IsNullOrEmpty(gameplaySceneName))
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameplaySceneName);
        else
            Debug.LogError("TitleMenuUI: gameplaySceneName is empty.");
    }

    public void OnToggleHighScore()
    {
        if (!highScorePanel) return;
        bool show = !highScorePanel.activeSelf;

        if (show)
        {
            HighScoreManager.EnsureInitialized(10);
            highScoreUI?.ShowReadOnly();        // build & show table, no input
        }

        highScorePanel.SetActive(show);
        if (pauseOnPanels) Time.timeScale = show ? 0f : 1f;
    }

    public void OnToggleSelectCharacter()
    {
        if (!characterSelectPanel) return;

        var go = characterSelectPanel.gameObject;
        bool show = !go.activeSelf;
        go.SetActive(show);

        if (show)
            characterSelectPanel.RefreshPreview();  // show the currently stored pick

        if (pauseOnPanels) Time.timeScale = show ? 0f : 1f;
    }

    public void OnToggleSelectMonster()
    {
        if (!monsterSelectPanel) return;

        var go = monsterSelectPanel.gameObject;
        bool show = !go.activeSelf;
        go.SetActive(show);

        if (show)
            monsterSelectPanel.RefreshAllUI();

        if (pauseOnPanels) Time.timeScale = show ? 0f : 1f;
    }


    public void OnToggleSettings()
    {
        if (!settingsPanel) return;
        bool show = !settingsPanel.activeSelf;
        settingsPanel.SetActive(show);
        if (pauseOnPanels) Time.timeScale = show ? 0f : 1f;
    }

    public void OnQuitGame()
    {
        // Save anything needed, then quit
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Optional: ESC closes panels
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel && settingsPanel.activeSelf) { OnToggleSettings(); return; }
            if (highScorePanel && highScorePanel.activeSelf) { OnToggleHighScore(); return; }
        }
    }

    // --- SFX hooking ---
    void HookAllButtonsForSFX()
    {
        var buttons = GetComponentsInChildren<Button>(includeInactive: true);
        foreach (var b in buttons)
        {
            // click
            b.onClick.AddListener(() => { if (AudioManager.I) AudioManager.I.PlaySFX(uiClickSFX); });

            // hover (optional)
            var hover = b.gameObject.GetComponent<UIButtonSFX>();
            if (!hover) hover = b.gameObject.AddComponent<UIButtonSFX>();
            hover.hoverClip = uiHoverSFX;
        }
    }

    public sealed class UIButtonSFX : MonoBehaviour, IPointerEnterHandler
    {
        public AudioClip hoverClip;
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hoverClip && AudioManager.I) AudioManager.I.PlaySFX(hoverClip, 1f, 1f);
        }
    }
}
