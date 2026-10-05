#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class RunBuffAchievementTests
{
    [UnityTest]
    public IEnumerator CombinedBuffStacksUnlockAtFifteenAndRepairRestoredCounts()
    {
        yield return new EnterPlayMode();

        const BindingFlags instancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        var singleton = typeof(PlayerProgress).GetField("<I>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        var dataField = typeof(PlayerProgress).GetField("_data", instancePrivate);
        var savedProgress = PlayerProgress.I;
        var savedBuffs = new List<RunModifierSO>(RunModsStore.Buffs);
        var savedDebuffs = new List<RunModifierSO>(RunModsStore.Debuffs);
        var savedStoreFields = new Dictionary<FieldInfo, object>();
        foreach (var field in typeof(RunModsStore).GetFields(BindingFlags.Public | BindingFlags.Static))
            if (!field.IsInitOnly) savedStoreFields[field] = field.GetValue(null);
        bool savedDemo = DemoBuildGuardRails.IsDemoBuild;
        int savedDemoLimit = DemoBuildGuardRails.MaxCompletedLevel;
        string savedDemoMessage = DemoBuildGuardRails.PurchaseBlockedMessage;
        GameObject progressObject = null, controllerObject = null;
        RunModifier buff = null;
        PlayerProgress progress = null;
        try
        {
            // A separate, freshly initialized progress object has no Steam/event
            // subscribers. Do not yield until its queued saves have been discarded.
            singleton.SetValue(null, null);
            progressObject = new GameObject("Isolated achievement test progress");
            progress = progressObject.AddComponent<PlayerProgress>();
            dataField.SetValue(progress, Activator.CreateInstance(dataField.FieldType, true));
            typeof(PlayerProgress).GetField("saveDebounceSeconds", instancePrivate).SetValue(progress, float.MaxValue);
            DemoBuildGuardRails.Configure(false);
            controllerObject = new GameObject("Inactive reward sync test controller");
            controllerObject.SetActive(false);
            var controller = controllerObject.AddComponent<GameController>();
            var syncRewards = typeof(GameController).GetMethod("SyncRunModsToStore", instancePrivate);
            buff = ScriptableObject.CreateInstance<RunModifier>();
            RunModsStore.Buffs.Clear();
            RunModsStore.Debuffs.Clear();
            int unlocks = 0;
            progress.AchievementUnlocked += id => { if (id == AchievementSystem.Ach.Buffs_15) unlocks++; };

            // Ten reward buffs and four stone buffs, including repeated stacks.
            for (int i = 0; i < 10; i++) RunModsStore.Buffs.Add(buff);
            syncRewards.Invoke(controller, null);
            for (int i = 0; i < 4; i++) RunModsStore.Buffs.Add(buff);
            for (int i = 0; i < 20; i++) RunModsStore.Debuffs.Add(buff);
            RunModsStore.Buffs.Add(null);
            syncRewards.Invoke(controller, null);
            Assert.AreEqual(14, progress.GetRunInt(AchievementSystem.Stat.RunBuffModsChosen));
            Assert.IsFalse(progress.IsUnlocked(AchievementSystem.Ach.Buffs_15));

            RunModsStore.Buffs.Add(buff);
            syncRewards.Invoke(controller, null);
            Assert.AreEqual(15, progress.GetRunInt(AchievementSystem.Stat.RunBuffModsChosen));
            Assert.IsTrue(progress.IsUnlocked(AchievementSystem.Ach.Buffs_15));
            RunModsStore.Buffs.Add(buff);
            syncRewards.Invoke(controller, null);
            syncRewards.Invoke(controller, null);
            Assert.AreEqual(16, progress.GetRunInt(AchievementSystem.Stat.RunBuffModsChosen));
            Assert.AreEqual(1, unlocks, "Syncing does not double-count or emit duplicate unlocks.");

            // Legacy checkpoints recorded only the ten round rewards. The restored
            // list supplies the missing stone buffs without requiring a new pickup.
            dataField.SetValue(progress, Activator.CreateInstance(dataField.FieldType, true));
            progress.RestoreRunState(new PlayerProgress.RunStateSnapshot
            {
                runInt = new List<PlayerProgress.StringLongEntry>
                {
                    new PlayerProgress.StringLongEntry { key = AchievementSystem.Stat.RunBuffModsChosen, value = 10 }
                }
            }, saveAfterRestore: false);
            RunModsStore.SyncBuffAchievementProgress();
            Assert.AreEqual(16, progress.GetRunInt(AchievementSystem.Stat.RunBuffModsChosen));
            Assert.IsTrue(progress.IsUnlocked(AchievementSystem.Ach.Buffs_15), "Counts above 15 unlock too.");

            dataField.SetValue(progress, Activator.CreateInstance(dataField.FieldType, true));
            progress.RestoreRunState(new PlayerProgress.RunStateSnapshot
            {
                runInt = new List<PlayerProgress.StringLongEntry>
                {
                    new PlayerProgress.StringLongEntry { key = AchievementSystem.Stat.RunBuffModsChosen, value = 16 }
                }
            }, saveAfterRestore: false);
            RunModsStore.SyncBuffAchievementProgress();
            Assert.IsTrue(progress.IsUnlocked(AchievementSystem.Ach.Buffs_15), "Unchanged restored counters are evaluated.");

            dataField.SetValue(progress, Activator.CreateInstance(dataField.FieldType, true));
            RunModsStore.Buffs.Clear();
            RunModsStore.SyncBuffAchievementProgress();
            Assert.AreEqual(0, progress.GetRunInt(AchievementSystem.Stat.RunBuffModsChosen));
            Assert.IsFalse(progress.IsUnlocked(AchievementSystem.Ach.Buffs_15), "Old run stacks do not carry into a new run.");
        }
        finally
        {
            if (progress)
            {
                progress.StopAllCoroutines();
                typeof(PlayerProgress).GetField("saveQueued", instancePrivate).SetValue(progress, false);
            }
            singleton.SetValue(null, savedProgress);
            if (progressObject) Object.DestroyImmediate(progressObject);
            if (controllerObject) Object.DestroyImmediate(controllerObject);
            if (buff) Object.DestroyImmediate(buff);
            RunModsStore.Buffs.Clear();
            RunModsStore.Buffs.AddRange(savedBuffs);
            RunModsStore.Debuffs.Clear();
            RunModsStore.Debuffs.AddRange(savedDebuffs);
            foreach (var pair in savedStoreFields) pair.Key.SetValue(null, pair.Value);
            DemoBuildGuardRails.Configure(savedDemo, savedDemoLimit, savedDemoMessage);
        }
        yield return new ExitPlayMode();
    }
}
#endif
