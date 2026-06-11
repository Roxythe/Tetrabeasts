using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SpecialBlockTutorialController : MonoBehaviour
{
    [Serializable]
    public class SpecialBlockTutorialEntry
    {
        public SpecialType specialType = SpecialType.Bomb;
        public string tutorialId = "tutorial_special_block_bomb";

        [TextArea(3, 6)]
        public string body =
            "You found a Bomb special block. Drop it to blast a 3x3 area and damage nearby obstacles. (Press [F] to Continue)";
    }

    [Header("Refs")]
    [SerializeField] GameController gameController;
    [SerializeField] TriggeredTutorialPopupController triggeredTutorialPopups;

    [Header("Popup")]
    [SerializeField] TutorialPopupView.PopupAnchorPreset popupAnchorPreset = TutorialPopupView.PopupAnchorPreset.Top;
    [SerializeField] Vector2 popupAnchoredPosition;
    [SerializeField, Range(0.1f, 1f)] float popupAlpha = 1f;

    [Header("Highlight")]
    [SerializeField] Vector2 highlightPadding = new Vector2(12f, 12f);

    [Header("Input Grace")]
    [SerializeField, Min(0f)] float hardDropGraceSeconds = 0.25f;

    [Header("Tutorials")]
    [SerializeField] List<SpecialBlockTutorialEntry> tutorials = new()
    {
        new SpecialBlockTutorialEntry
        {
            specialType = SpecialType.Death,
            tutorialId = "tutorial_special_block_death",
            body = "You found a Death special block. Drop it in a column to destroy all units matching the first monster below it. (Press [F] to Continue)"
        },
        new SpecialBlockTutorialEntry
        {
            specialType = SpecialType.Bomb,
            tutorialId = "tutorial_special_block_bomb",
            body = "You found a Bomb special block. Drop it to blast a 3x3 area and damage nearby obstacles. (Press [F] to Continue)"
        },
        new SpecialBlockTutorialEntry
        {
            specialType = SpecialType.Bolt,
            tutorialId = "tutorial_special_block_bolt",
            body = "You found a Bolt special block. Drop it to strike an entire column. (Press [F] to Continue)"
        },
        new SpecialBlockTutorialEntry
        {
            specialType = SpecialType.Earthquake,
            tutorialId = "tutorial_special_block_earthquake",
            body = "You found an Earthquake special block. Drop it to shake loose unsupported units across the board. (Press [F] to Continue)"
        },
        new SpecialBlockTutorialEntry
        {
            specialType = SpecialType.SlowGravity,
            tutorialId = "tutorial_special_block_slow_gravity",
            body = "You found a Slow Gravity special block. Drop it to slow falling pieces for a short time. (Press [F] to Continue)"
        }
    };

    readonly HashSet<SpecialType> _shownThisRun = new();
    Coroutine _activeRoutine;

    void Awake()
    {
        ResolveReferences();
    }

    public void ResetRunState()
    {
        _shownThisRun.Clear();

        if (_activeRoutine != null)
        {
            StopCoroutine(_activeRoutine);
            _activeRoutine = null;
        }

        if (gameController)
            gameController.SetTutorialPieceInputBlocked(false);
    }

    void OnDisable()
    {
        if (gameController)
            gameController.SetTutorialPieceInputBlocked(false);
    }

    public void TryShowForSpecialBlock(SpecialType specialType, IReadOnlyList<RectTransform> highlightTargets)
    {
        if (specialType == SpecialType.None || _shownThisRun.Contains(specialType))
            return;

        var entry = FindEntry(specialType);
        if (entry == null || string.IsNullOrWhiteSpace(entry.tutorialId) || string.IsNullOrWhiteSpace(entry.body))
            return;

        ResolveReferences();
        if (!triggeredTutorialPopups || triggeredTutorialPopups.IsTutorialCompletedForCurrentMode(entry.tutorialId))
            return;

        if (_activeRoutine != null)
            return;

        _shownThisRun.Add(specialType);
        _activeRoutine = StartCoroutine(ShowRoutine(entry, highlightTargets));
    }

    IEnumerator ShowRoutine(SpecialBlockTutorialEntry entry, IReadOnlyList<RectTransform> highlightTargets)
    {
        ResolveReferences();

        if (!triggeredTutorialPopups)
            yield break;

        if (gameController)
            gameController.SetTutorialPieceInputBlocked(true);

        yield return triggeredTutorialPopups.ShowOnceAndWait(
            entry.tutorialId,
            entry.body,
            popupAnchorPreset,
            popupAnchoredPosition,
            popupAlpha,
            pauseGameplay: true,
            freezePieceGravity: true,
            allowSkip: true,
            highlightTargets,
            highlightPadding);

        if (gameController)
        {
            gameController.SetTutorialPieceInputBlocked(false);
            gameController.BlockHardDropInputFor(hardDropGraceSeconds);
        }

        _activeRoutine = null;
    }

    SpecialBlockTutorialEntry FindEntry(SpecialType specialType)
    {
        if (tutorials == null)
            return null;

        for (int i = 0; i < tutorials.Count; i++)
        {
            var entry = tutorials[i];
            if (entry != null && entry.specialType == specialType)
                return entry;
        }

        return null;
    }

    void ResolveReferences()
    {
        if (!gameController)
            gameController = GetComponent<GameController>();

        if (!gameController)
            gameController = FindFirstObjectByType<GameController>(FindObjectsInactive.Include);

        if (!triggeredTutorialPopups)
            triggeredTutorialPopups = GetComponent<TriggeredTutorialPopupController>();

        if (!triggeredTutorialPopups)
            triggeredTutorialPopups = FindFirstObjectByType<TriggeredTutorialPopupController>(FindObjectsInactive.Include);
    }
}
