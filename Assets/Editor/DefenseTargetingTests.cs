#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class DefenseTargetingTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Get(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);

    [Test]
    public void ExposedDefendersTakePriorityThenLivingAndDeadColumnsFillTheVolley()
    {
        var root = new GameObject("Target selection fixture");
        root.SetActive(false);
        var defender = ScriptableObject.CreateInstance<MonsterData>();
        var attacker = ScriptableObject.CreateInstance<MonsterData>();
        defender.role = MonsterRole.Defense;
        attacker.role = MonsterRole.Attack;
        var random = UnityEngine.Random.state;
        try
        {
            var gc = root.AddComponent<GameController>();
            var boardGo = new GameObject("Board", typeof(RectTransform));
            boardGo.SetActive(false);
            boardGo.transform.SetParent(root.transform);
            var board = boardGo.AddComponent<Board>();
            board.width = 10; board.height = 16;
            gc.gameBoard = board;
            var placed = (Dictionary<Vector2Int, RectTransform>)Get(board, "placed");
            var monsters = (Dictionary<Vector2Int, Board.MonsterInstance>)Get(board, "monsters");
            void Place(int x, int y, MonsterData data, float hp = 10)
            {
                var cell = new Vector2Int(x, y);
                var tile = new GameObject("Cell", typeof(RectTransform)).GetComponent<RectTransform>();
                tile.SetParent(boardGo.transform);
                placed[cell] = tile;
                if (data) monsters[cell] = new Board.MonsterInstance { data = data, hp = hp, maxHp = 10 };
            }
            for (int x = 0; x < 3; x++) { Place(x, 1, attacker); Place(x, 3, defender); }
            board.SetCraterCell(new Vector2Int(2, 5), true);
            Place(3, 1, defender); Place(3, 3, attacker);
            Place(4, 1, defender); Place(4, 3, null); // Occupied obstacle above the defender.
            Place(5, 3, defender, 0);
            board.SetCraterCell(new Vector2Int(6, 3), true);
            Place(7, 1, attacker);
            Call(gc, "CollectCastleTargetColumns");
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, (IList)Get(gc, "_castleDefenseTargetColumns"));
            CollectionAssert.AreEquivalent(new[] { 3, 4, 7 }, (IList)Get(gc, "_castleAliveTargetColumns"));
            CollectionAssert.AreEquivalent(new[] { 5 }, (IList)Get(gc, "_castleDeadOnlyTargetColumns"));
            foreach (int count in new[] { 0, 1, 3, 5, 10 })
            {
                Call(gc, "CollectCastleTargetColumns");
                var picked = (List<int>)Call(gc, "PickCastleProjectileColumns", Get(gc, "_castleAliveTargetColumns"),
                    Get(gc, "_castleDeadOnlyTargetColumns"), count);
                Assert.AreEqual(Mathf.Min(count, 7), picked.Count);
                Assert.AreEqual(picked.Count, new HashSet<int>(picked).Count, "One projectile per selected column.");
                for (int i = 0; i < Mathf.Min(count, 3); i++) Assert.That(picked[i], Is.InRange(0, 2));
                if (count == 5) Assert.IsFalse(picked.Contains(5), "Living columns precede the dead-only fallback.");
                Assert.IsFalse(picked.Contains(6), "Craters alone are not attack targets.");
            }
            // Removing the front defenders restores the previous living-column fallback.
            for (int x = 0; x < 3; x++) { placed.Remove(new Vector2Int(x, 3)); monsters.Remove(new Vector2Int(x, 3)); }
            Call(gc, "CollectCastleTargetColumns");
            Assert.IsEmpty((IList)Get(gc, "_castleDefenseTargetColumns"));
            var fallback = (List<int>)Call(gc, "PickCastleProjectileColumns", Get(gc, "_castleAliveTargetColumns"),
                Get(gc, "_castleDeadOnlyTargetColumns"), 3);
            Assert.AreEqual(3, fallback.Count);
            Assert.IsFalse(fallback.Contains(5));
        }
        finally
        {
            UnityEngine.Random.state = random;
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(defender);
            Object.DestroyImmediate(attacker);
        }
    }

    [Test]
    public void SongPopupDefaultsOnAndItsSavedChoiceRoundTripsThroughAccountSettings()
    {
        bool existed = PlayerPrefs.HasKey(SettingsStore.KeySongPopup);
        int previous = PlayerPrefs.GetInt(SettingsStore.KeySongPopup);
        try
        {
            PlayerPrefs.DeleteKey(SettingsStore.KeySongPopup);
            Assert.IsTrue(SettingsStore.LoadSongPopupEnabled());
            SettingsStore.SaveSongPopupEnabled(false);
            Assert.IsFalse(SettingsStore.LoadSongPopupEnabled());
            var service = typeof(SteamCloudSaveService);
            var snapshotType = service.GetNestedType("CloudSaveSnapshot", BindingFlags.NonPublic);
            var snapshot = Activator.CreateInstance(snapshotType);
            service.GetMethod("CapturePlayerPrefs", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { snapshot });
            var entries = (IList)snapshotType.GetField("intPrefs").GetValue(snapshot);
            object savedEntry = null;
            foreach (var entry in entries)
                if ((string)entry.GetType().GetField("key").GetValue(entry) == SettingsStore.KeySongPopup) savedEntry = entry;
            Assert.IsNotNull(savedEntry, "The new preference is part of the account settings snapshot.");
            var restore = Activator.CreateInstance(snapshotType);
            ((IList)snapshotType.GetField("intPrefs").GetValue(restore)).Add(savedEntry);
            SettingsStore.SaveSongPopupEnabled(true);
            service.GetMethod("ApplySnapshot", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { restore });
            Assert.IsFalse(SettingsStore.LoadSongPopupEnabled());
        }
        finally
        {
            if (existed) PlayerPrefs.SetInt(SettingsStore.KeySongPopup, previous);
            else PlayerPrefs.DeleteKey(SettingsStore.KeySongPopup);
            PlayerPrefs.Save();
        }
    }
}
#endif
