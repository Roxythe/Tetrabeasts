#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class AnimationPlaybackTests
{
    [Test]
    public void HallucinationUsesAllNineteenFramesAndReversesWithoutRepeatingEndpoints()
    {
        var modifier = AssetDatabase.LoadAssetAtPath<LevelModifierSO>("Assets/ScriptableObjects/LevelModifier/Hallucination.asset");
        Assert.AreEqual(19, modifier.hallucinationIdleFrames.Length);
        for (int i = 0; i < 19; i++)
        {
            var expected = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/Animation/Hallucination/Hallucination_Animation ({i}).png");
            Assert.IsNotNull(expected);
            Assert.AreEqual(expected, modifier.hallucinationIdleFrames[i], "Numeric frame order");
        }
        var go = new GameObject("Ping-pong test", typeof(RectTransform), typeof(Image));
        try
        {
            var image = go.GetComponent<Image>();
            var animator = go.AddComponent<UIImagePingPongAnimator>();
            animator.enabled = false;
            animator.Configure(image, modifier.hallucinationIdleFrames, 10f);
            Assert.AreEqual(modifier.hallucinationIdleFrames[0], image.sprite);
            for (int i = 1; i <= 36; i++)
            {
                animator.Advance(0.1f);
                Assert.AreEqual(modifier.hallucinationIdleFrames[i <= 18 ? i : 36 - i], image.sprite);
            }
            animator.Stop();
            animator.Advance(1f);
            Assert.AreEqual(modifier.hallucinationIdleFrames[0], image.sprite);
            animator.Play();
            animator.Advance(0.1f);
            Assert.AreEqual(modifier.hallucinationIdleFrames[1], image.sprite);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [UnityTest]
    public IEnumerator MaoAndQuixelFinishWithLastFrameAtFasterSpeedDuringGameplayPause()
    {
        yield return new EnterPlayMode();
        float oldTimeScale = Time.timeScale;
        GameObject root = null;
        try
        {
            Time.timeScale = 0f;
            foreach (string name in new[] { "Mao", "Quixel" })
            {
                var character = AssetDatabase.LoadAssetAtPath<PlayerCharacterData>($"Assets/ScriptableObjects/Commanders/{name}.asset");
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"Assets/Animations/{name}Special.anim");
                Assert.AreEqual(1.25f, character.specialAbilityAnimationPlaybackSpeed);
                Assert.IsTrue(character.specialAbilityAnimationFinishOnLastFrame);
                var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).First(b => b.propertyName == "m_Sprite");
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                var lastSprite = keys[keys.Length - 1].value as Sprite;

                root = new GameObject("Special animation test", typeof(RectTransform), typeof(Canvas));
                var popup = root.AddComponent<SpecialAbilityPopup>();
                var slot = new GameObject("SpecialAbility_Animation", typeof(RectTransform));
                slot.transform.SetParent(root.transform, false);
                float started = 0f, closing = 0f;
                yield return popup.Play(character,
                    onCharacterAnimationStart: () => started = Time.realtimeSinceStartup,
                    onClosingStarted: _ => closing = Time.realtimeSinceStartup);
                float elapsed = Time.realtimeSinceStartup - started;
                float expected = clip.length / 1.25f;
                Assert.That(elapsed, Is.InRange(expected, expected + 0.4f), name + " must not wait through an extra outro after the clip.");
                Assert.That(closing - started, Is.InRange(expected - 1.2f, expected - 0.6f));
                var animator = root.GetComponentsInChildren<Animator>().Single();
                Assert.AreEqual(lastSprite, animator.GetComponent<Image>().sprite);
                Assert.AreEqual(0f, animator.speed);
                Object.Destroy(root);
                root = null;
            }
        }
        finally
        {
            Time.timeScale = oldTimeScale;
            if (root) Object.Destroy(root);
        }
        yield return new ExitPlayMode();
    }
}
#endif
