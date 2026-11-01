using System.Collections;
using NUnit.Framework;
using PvZ.Config;
using PvZ.Presentation.Rendering;
using UnityEngine;
using UnityEngine.TestTools;

namespace PvZ.Tests.PlayMode.Zombies
{
    public sealed class ZombieEatingAnimationTests
    {
        private const string PartPath = "component/basic/__AffineContent/";
        private const float FrameDuration = 1f / 12f;

        [UnityTest]
        public IEnumerator EatingAtNonZeroPosition_LoopsInPlaceAndRepeatedlyResumesWalking()
        {
            var config = Resources.Load<GameConfigObject>("GameConfigObject");
            Assert.That(config, Is.Not.Null);
            Assert.That(config.ZombieNormal, Is.Not.Null);
            var instance = Object.Instantiate(config.ZombieNormal, new Vector3(243f, -87f, 10f), Quaternion.identity);
            try
            {
                var animator = instance.GetComponent<Animator>();
                Assert.That(animator, Is.Not.Null);
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.Update(0f);
                AssertPosition(instance.transform.position, new Vector3(243f, -87f, 10f),
                    "The walking prefab must initialize at the requested non-zero position.");

                var shadowAnchor = instance.transform.Find("component/anchors/shadow");
                var jaw = instance.transform.Find(PartPath + "Zombie_jaw").GetComponent<SpriteTransform>();
                var originalHand = instance.transform.Find(PartPath + "Zombie_outerarm_hand");
                var eatingHand = instance.transform.Find(PartPath + "Zombie_outerarm_hand2");
                Assert.That(shadowAnchor, Is.Not.Null);
                Assert.That(jaw, Is.Not.Null);
                Assert.That(originalHand, Is.Not.Null);
                Assert.That(eatingHand, Is.Not.Null);
                var anchorLocalPosition = shadowAnchor.localPosition;
                Assert.That(anchorLocalPosition, Is.EqualTo(new Vector3(12.75f, -50f, 0f)));
                Assert.That(animator.GetBool("isEating"), Is.False);

                // Evaluate the real Animator in deterministic 12 fps steps, without
                // waiting in wall-clock time for two complete chewing cycles.
                for (var toggle = 0; toggle < 3; toggle++)
                {
                    var rootBeforeEating = instance.transform.position;
                    animator.SetBool("isEating", true);
                    animator.Update(0f);
                    animator.Update(0f);
                    Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("eat"), Is.True);
                    AssertPosition(instance.transform.position, rootBeforeEating,
                        "Entering eat must not snap the existing world position to the animation origin.");
                    Assert.That(originalHand.gameObject.activeSelf, Is.False);
                    Assert.That(eatingHand.gameObject.activeSelf, Is.True);

                    var startNormalizedTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                    var initialJawPosition = jaw.position;
                    var initialJawSkew = jaw.skew;
                    var jawMoved = false;
                    var frames = toggle == 0 ? 80 : 6;
                    for (var frame = 0; frame < frames; frame++)
                    {
                        animator.Update(FrameDuration);
                        jawMoved |= (jaw.position - initialJawPosition).sqrMagnitude > 0.0001f ||
                                    (jaw.skew - initialJawSkew).sqrMagnitude > 0.0001f;
                        Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("eat"), Is.True);
                        AssertPosition(instance.transform.position, rootBeforeEating,
                            $"Eating must remain in place, toggle {toggle}, frame {frame}.");
                        AssertPosition(shadowAnchor.localPosition, anchorLocalPosition,
                            "The ground anchor must not inherit the chewing body motion.");
                    }

                    Assert.That(jawMoved, Is.True, "The jaw must animate while the root stays still.");
                    if (toggle == 0)
                    {
                        Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime - startNormalizedTime,
                            Is.GreaterThan(2f), "Eating should keep playing for more than two complete loops.");
                        // Allow a normal PlayMode frame to evaluate the presentation scheduler too.
                        yield return null;
                        AssertPosition(instance.transform.position, rootBeforeEating,
                            "Eating must also stay in place during normal frame evaluation.");
                    }

                    var beforeWalking = instance.transform.position;
                    animator.SetBool("isEating", false);
                    animator.Update(0f);
                    animator.Update(0f);
                    Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("walk"), Is.True);
                    AssertPosition(instance.transform.position, beforeWalking,
                        "Returning to walk must retain the current world position.");
                    Assert.That(originalHand.gameObject.activeSelf, Is.True);
                    Assert.That(eatingHand.gameObject.activeSelf, Is.False);
                    for (var frame = 0; frame < 12; frame++) animator.Update(FrameDuration);

                    Assert.That(instance.transform.position.x, Is.LessThan(beforeWalking.x - 5f),
                        "The same Animator must resume leftward walking root motion after eating.");
                    Assert.That(instance.transform.position.y, Is.EqualTo(beforeWalking.y).Within(0.001f));
                    Assert.That(instance.transform.position.z, Is.EqualTo(beforeWalking.z).Within(0.001f));
                    AssertPosition(shadowAnchor.localPosition, anchorLocalPosition,
                        "Resuming walk must not change the ground anchor.");
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void AssertPosition(Vector3 actual, Vector3 expected, string message)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(0.001f), message);
        }
    }
}
