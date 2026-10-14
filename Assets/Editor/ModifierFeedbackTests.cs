#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class ModifierFeedbackTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    [Test]
    public void BoulderScalingAndPacingAreBounded()
    {
        Assert.AreEqual(15f, BoulderController.CalculateHitDamage(100, .1f));
        Assert.AreEqual(20f, BoulderController.CalculateHitDamage(100, .15f));
        Assert.AreEqual(155f, BoulderController.CalculateHitDamage(1000, .15f));
        Assert.AreEqual(1f, BoulderController.SpawnIntervalScale(0));
        Assert.Less(BoulderController.SpawnIntervalScale(90), BoulderController.SpawnIntervalScale(30));
        Assert.AreEqual(.4f, BoulderController.SpawnIntervalScale(180), .0001f);
        Assert.AreEqual(.4f, BoulderController.SpawnIntervalScale(99999), .0001f);
    }
    [Test]
    public void FeedbackAssetsAreAssignedAndShakeStacksWithinBounds()
    {
        foreach (string name in new[] { "BoulderImpact", "GhostSpawn", "GhostMerge", "GhostDestroyed", "BombardmentImpact", "InversionActivate", "BoardFlip" })
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/LevelModifier/" + name + ".wav"));
        var inversion = AssetDatabase.LoadAssetAtPath<LevelModifierSO>("Assets/ScriptableObjects/LevelModifier/Inversion.asset");
        Assert.IsNotNull(inversion.inversionBoardSprite); Assert.IsNotNull(inversion.inversionSfx);
        var ghost = AssetDatabase.LoadAssetAtPath<LevelModifierSO>("Assets/ScriptableObjects/LevelModifier/Hallucination.asset");
        Assert.IsNotNull(ghost.hallucinationSpawnSfx); Assert.IsNotNull(ghost.hallucinationMergeSfx); Assert.IsNotNull(ghost.hallucinationDeathSfx);
        Assert.AreEqual("BoulderImpact", GameModeAssets.Load().boulderImpactSfx.name);
        var target = new GameObject("Shake test");
        try
        {
            ImpactShake.Play(target.transform, .3f, 6);
            var shake = target.GetComponent<ImpactShake>();
            float first = (float)typeof(ImpactShake).GetField("strength", Private).GetValue(shake);
            ImpactShake.Play(target.transform, .3f, 6);
            Assert.Greater((float)typeof(ImpactShake).GetField("strength", Private).GetValue(shake), first);
            for (int i = 0; i < 100; i++) ImpactShake.Play(target.transform, .3f, 6);
            Assert.LessOrEqual((float)typeof(ImpactShake).GetField("strength", Private).GetValue(shake), 12f);
            typeof(ImpactShake).GetField("remaining", Private).SetValue(shake, 0f);
            typeof(ImpactShake).GetField("lastTriggerTime", Private).SetValue(shake, Time.time - .3f);
            ImpactShake.Play(target.transform, .25f, 6);
            Assert.Greater((float)typeof(ImpactShake).GetField("strength", Private).GetValue(shake), first, "Closely spaced completed shakes also accumulate.");
            ImpactShake.Stop(target.transform);
            Assert.AreEqual(0f, typeof(ImpactShake).GetField("remaining", Private).GetValue(shake));
        }
        finally { Object.DestroyImmediate(target); }
    }
    [UnityTest]
    public IEnumerator ModifierAudioUsesLiveMasterAndSfxSliders()
    {
        yield return new EnterPlayMode();
        var audio = AudioManager.I;
        bool created = !audio;
        if (!audio) audio = new GameObject("Feedback audio test").AddComponent<AudioManager>();
        float master = audio.masterVolume, sfx = audio.sfxVolume;
        try
        {
            audio.SetMasterVolume(.5f, false); audio.SetSFXVolume(.4f, false);
            audio.PlaySFX(GameModeAssets.Load().boulderImpactSfx);
            var sources = (AudioSource[])typeof(AudioManager).GetField("sfxOneShotSources", Private).GetValue(audio);
            foreach (var source in sources) Assert.AreEqual(.2f, source.volume, .0001f);
            audio.SetSFXVolume(0, false);
            foreach (var source in sources) Assert.AreEqual(0f, source.volume);
            audio.SetSFXVolume(1, false); audio.SetMasterVolume(0, false);
            foreach (var source in sources) Assert.AreEqual(0f, source.volume);
        }
        finally
        {
            audio.SetMasterVolume(master, false); audio.SetSFXVolume(sfx, false);
            if (created) Object.Destroy(audio.gameObject);
        }
        yield return null;
        yield return new ExitPlayMode();
    }
}
#endif
