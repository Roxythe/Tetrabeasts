using UnityEngine;

public static class RunSummaryStats
{
    public readonly struct Snapshot
    {
        public readonly int linesCleared;
        public readonly int specialUsedCount;
        public readonly int obstaclesDestroyed;
        public readonly int highestCombo;
        public readonly int highestSingleAttackDamage;
        public readonly int unitsDied;
        public readonly float totalHealingDone;
        public readonly int totalDamageDealt;
        public readonly float clearTimeSeconds;
        public readonly int finalScore;

        public Snapshot(int linesCleared, int specialUsedCount, int obstaclesDestroyed, int highestCombo,
                        int highestSingleAttackDamage, int unitsDied, float totalHealingDone,
                        int totalDamageDealt, float clearTimeSeconds, int finalScore)
        {
            this.linesCleared = linesCleared;
            this.specialUsedCount = specialUsedCount;
            this.obstaclesDestroyed = obstaclesDestroyed;
            this.highestCombo = highestCombo;
            this.highestSingleAttackDamage = highestSingleAttackDamage;
            this.unitsDied = unitsDied;
            this.totalHealingDone = totalHealingDone;
            this.totalDamageDealt = totalDamageDealt;
            this.clearTimeSeconds = clearTimeSeconds;
            this.finalScore = finalScore;
        }
    }

    static int _linesCleared;
    static int _specialUsedCount;
    static int _obstaclesDestroyed;
    static int _highestCombo;
    static int _highestSingleAttackDamage;
    static int _unitsDied;
    static float _totalHealingDone;
    static int _totalDamageDealt;
    static float _clearTimeSeconds;
    static int _finalScore;

    public static void BeginRun()
    {
        _linesCleared = 0;
        _specialUsedCount = 0;
        _obstaclesDestroyed = 0;
        _highestCombo = 0;
        _highestSingleAttackDamage = 0;
        _unitsDied = 0;
        _totalHealingDone = 0f;
        _totalDamageDealt = 0;
        _clearTimeSeconds = 0f;
        _finalScore = 0;
    }

    public static void AddActiveTime(float deltaSeconds)
    {
        if (deltaSeconds <= 0f) return;
        _clearTimeSeconds += deltaSeconds;
    }

    public static void AddLinesCleared(int amount)
    {
        if (amount <= 0) return;
        _linesCleared += amount;
    }

    public static void AddSpecialUsed(int amount = 1)
    {
        if (amount <= 0) return;
        _specialUsedCount += amount;
    }

    public static void AddObstaclesDestroyed(int amount = 1)
    {
        if (amount <= 0) return;
        _obstaclesDestroyed += amount;
    }

    public static void RecordCombo(int comboCount)
    {
        if (comboCount <= _highestCombo) return;
        _highestCombo = comboCount;
    }

    public static void AddUnitsDied(int amount = 1)
    {
        if (amount <= 0) return;
        _unitsDied += amount;
    }

    public static void AddHealingDone(float amount)
    {
        if (amount <= 0f) return;
        _totalHealingDone += amount;
    }

    public static void RecordDamageDealt(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (amount <= 0) return;

        _totalDamageDealt += amount;
        _highestSingleAttackDamage = Mathf.Max(_highestSingleAttackDamage, amount);
    }

    public static void SetFinalScore(int score)
    {
        _finalScore = Mathf.Max(0, score);
    }

    public static Snapshot GetSnapshot()
    {
        return new Snapshot(
            _linesCleared,
            _specialUsedCount,
            _obstaclesDestroyed,
            _highestCombo,
            _highestSingleAttackDamage,
            _unitsDied,
            _totalHealingDone,
            _totalDamageDealt,
            _clearTimeSeconds,
            _finalScore);
    }
}
