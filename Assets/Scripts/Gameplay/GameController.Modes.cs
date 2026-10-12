using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameController
{
    BoulderController boulders;
    GameModeAssets modeAssets;
    MarathonGaugeUI marathonGauge;
    MarathonRewardUI marathonRewards;
    float marathonDamage;
    bool marathonRewardActive;
    float marathonResumeTimeScale = 1f;
    public bool IsMarathon => GameModeStore.Current == RunGameMode.Marathon;
    public float MarathonDamage => marathonDamage;
    public float MarathonRequiredDamage => RequiredMarathonDamage(currentLevel,
        modeAssets ? modeAssets.marathonStartingDamage : 500f,
        modeAssets ? modeAssets.marathonDamageGrowth : 1.18f);
    public static float RequiredMarathonDamage(int completedLevels, float initial, float growth)
    {
        int level = Mathf.Max(0, completedLevels);
        float rate = Mathf.Clamp(growth, 1.01f, 2f);
        // Continue growing after very long runs without overflowing to infinite HP.
        return Mathf.Max(1f, initial) * Mathf.Pow(rate, Mathf.Min(level, 60)) *
            (1f + Mathf.Max(0, level - 60) * (rate - 1f));
    }

    void InitializeModeRuntime()
    {
        modeAssets = GameModeAssets.Load();
        boulders = GetComponent<BoulderController>();
        if (!boulders) boulders = gameObject.AddComponent<BoulderController>();
        if (modeAssets) boulders.boulderSprite = modeAssets.boulderSprite;
    }
    void EnsureMarathonUI()
    {
        if (!IsMarathon || marathonGauge || !modeAssets || !gameBoard) return;
        var canvas = gameBoard.GetComponentInParent<Canvas>()?.rootCanvas;
        if (!canvas) return;
        marathonGauge = Instantiate(modeAssets.marathonGaugePrefab, canvas.transform);
        marathonRewards = Instantiate(modeAssets.marathonRewardPrefab, canvas.transform);
        marathonRewards.gameObject.SetActive(false);
        // Reflect the ultimate gauge's center across the board center in canvas space.
        var target = (RectTransform)marathonGauge.transform;
        var canvasRect = (RectTransform)canvas.transform;
        Vector3 boardCenter = canvasRect.InverseTransformPoint(gameBoard.gridRoot.TransformPoint(gameBoard.gridRoot.rect.center));
        var reference = specialGaugeSlider ? specialGaugeSlider.transform as RectTransform : null;
        if (reference)
        {
            Vector3 ultimateCenter = canvasRect.InverseTransformPoint(reference.TransformPoint(reference.rect.center));
            target.localPosition = new Vector3(2f * boardCenter.x - ultimateCenter.x, ultimateCenter.y, 0f);
            target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, reference.rect.height * reference.lossyScale.y / canvasRect.lossyScale.y);
        }
        else target.localPosition = boardCenter + Vector3.left * (gameBoard.gridRoot.rect.width * 0.5f + 70f);
    }
    void TickGameMode()
    {
        if (!IsMarathon) return;
        EnsureMarathonUI();
        RefreshMarathonGauge();
        if (IsRoundActive && !marathonRewardActive && marathonDamage >= MarathonRequiredDamage)
            StartCoroutine(MarathonLevelUp());
    }
    void RefreshMarathonGauge()
    {
        if (marathonGauge) marathonGauge.Refresh(currentLevel + 1, marathonDamage, MarathonRequiredDamage);
        if (IsMarathon && enemyCastleUI)
            enemyCastleUI.SetMarathonProgress(Mathf.Clamp01(marathonDamage / MarathonRequiredDamage), IsFinalMarathonBoss());
    }
    List<CastleData> MarathonBosses()
    {
        var bosses = new List<CastleData>();
        if (castlesByLevel != null)
            foreach (var castle in castlesByLevel)
                if (castle && castle.isBossLevel && !bosses.Contains(castle)) bosses.Add(castle);
        if (postFinalSurvivalCastle && !bosses.Contains(postFinalSurvivalCastle)) bosses.Add(postFinalSurvivalCastle);
        return bosses;
    }
    bool IsFinalMarathonBoss()
    {
        var bosses = MarathonBosses();
        return bosses.Count > 0 && currentLevel + 1 >= bosses.Count * 3;
    }
    CastleData ResolveMarathonCastle(int levelIndex)
    {
        var bosses = MarathonBosses();
        int level = levelIndex + 1;
        if (bosses.Count > 0 && (level % 3 == 0 || level >= bosses.Count * 3))
            return bosses[Mathf.Min(bosses.Count - 1, level / 3 - 1)];
        var normal = new List<CastleData>();
        if (castlesByLevel != null)
            foreach (var castle in castlesByLevel) if (castle && !castle.isBossLevel) normal.Add(castle);
        return normal.Count > 0 ? normal[levelIndex % normal.Count] : (bosses.Count > 0 ? bosses[0] : null);
    }
    IEnumerator MarathonLevelUp()
    {
        var options = MarathonRewardUI.RollPairs(buffPool, debuffPool);
        if (!marathonRewards || options.Length != 3)
        {
            Debug.LogError("Marathon requires its reward prefab and at least three matching-rarity reward pairs.");
            yield break;
        }
        marathonRewardActive = true;
        marathonResumeTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        EnterUICursorMode();
        // Carryover can earn several levels; offer a separate confirmed choice for each.
        while (marathonDamage >= MarathonRequiredDamage && !gameOver)
        {
            bool keepFinalBoss = IsFinalMarathonBoss();
            marathonDamage -= MarathonRequiredDamage;
            var xp = ComputeRoundWinXp(currentLevel + 1);
            foreach (var award in xp.perMonsterAwardXp) RunMonsterProgress.AddRunXp(award.Key, award.Value);
            currentLevel++;
            bool confirmed = false;
            options = MarathonRewardUI.RollPairs(buffPool, debuffPool);
            marathonRewards.Show(currentLevel + 1, options, (buff, debuff) =>
            {
                ApplyChosenRunMods(buff, debuff);
                confirmed = true;
            });
            RefreshMarathonGauge();
            while (!confirmed && !gameOver) yield return null;
            if (gameOver) break;
            if (!keepFinalBoss) InitLevel(currentLevel, spawnEnvironment: false);
            else
            {
                // The final boss and her ability timers survive subsequent level-ups.
                ResetLevelTimerAndDrop(currentLevel);
                _maxComboThisLevel = 0; _obstaclesDestroyedThisLevel = 0;
                _levelStartReserveUnits = unitLives; _levelStartMaxLives = EffectiveMaxUnitLives;
                if (levelText) levelText.text = "Level: " + (currentLevel + 1);
            }
            RefreshMarathonGauge();
            CacheAndWriteTempRunCheckpoint();
        }
        marathonRewardActive = false;
        Time.timeScale = marathonResumeTimeScale;
        if (!gameOver) EnterGameplayCursorMode();
    }
    void ResetModeProgress()
    {
        if (marathonRewardActive) Time.timeScale = marathonResumeTimeScale;
        marathonRewardActive = false;
        marathonDamage = 0f;
        if (marathonRewards) marathonRewards.gameObject.SetActive(false);
        boulders?.Clear();
    }
}
