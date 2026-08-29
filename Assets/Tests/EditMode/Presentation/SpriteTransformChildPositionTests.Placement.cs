using NUnit.Framework;
using PvZ.Presentation.Rendering;
using PvZ.Gameplay.Presentation.EntityPreviews;
using UnityEditor;
using UnityEngine;

namespace PvZ.Tests.EditMode.Presentation
{
    public sealed partial class SpriteTransformChildPositionTests
    {
    [TestCase("Assets/Prefab/Plant/PeaShooterSingle/PeaShooterSingle.prefab")]
            [TestCase("Assets/Prefab/Plant/SunFlower/SunFlower.prefab")]
            [TestCase("Assets/Prefab/Plant/SunShroom/SunShroom.prefab")]
            public void PlantPrefab_TopLevelAnimationRootIsCentered(string prefabPath)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Assert.That(prefab, Is.Not.Null);
    
                var basic = prefab.transform.Find("component/basic");
                Assert.That(basic, Is.Not.Null);
    
                var spriteTransform = basic.GetComponent<SpriteTransform>();
                Assert.That(spriteTransform, Is.Not.Null);
                Assert.That(prefab.transform.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(basic.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(spriteTransform.providesChildSpritePosition, Is.False);
                Assert.That(spriteTransform.providesChildSpriteAffine, Is.False);
                Assert.That(spriteTransform.spritePosition, Is.EqualTo(Vector2.zero));
            }

    [Test]
            public void PeaShooterPrefab_DefaultPoseMatchesIdleFirstFrame()
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefab/Plant/PeaShooterSingle/PeaShooterSingle.prefab");
                var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    "Assets/Prefab/Plant/PeaShooterSingle/Animation/idle.anim");
                var headIdle = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    "Assets/Prefab/Plant/PeaShooterSingle/Animation/head_idle.anim");
    
                Assert.That(prefab, Is.Not.Null);
                Assert.That(idle, Is.Not.Null);
                Assert.That(headIdle, Is.Not.Null);
    
                AssertFirstFrame(prefab.transform, idle);
                AssertFirstFrame(prefab.transform, headIdle);
    
                foreach (var spriteTransform in prefab.GetComponentsInChildren<SpriteTransform>(true))
                {
                    Assert.That(spriteTransform.scale.x, Is.Not.Zero, spriteTransform.name);
                    Assert.That(spriteTransform.scale.y, Is.Not.Zero, spriteTransform.name);
                    Assert.That(spriteTransform.brightness, Is.EqualTo(1f), spriteTransform.name);
                    Assert.That(spriteTransform.alpha, Is.EqualTo(1f), spriteTransform.name);
                    Assert.That(spriteTransform.alphaCoef, Is.EqualTo(1f), spriteTransform.name);
    
                    if (!spriteTransform.updatePosition) continue;
    
                    var sourceParent = FindNativeSourceParent(spriteTransform.transform.parent);
                    if (sourceParent != null && spriteTransform.NativeContent != null)
                    {
                        var expectedLocal = ResolveSourceLocalPosition(spriteTransform, sourceParent);
                        var expectedWorld = sourceParent.NativeContent.TransformPoint(expectedLocal);
                        Assert.That(spriteTransform.transform.position.x,
                            Is.EqualTo(expectedWorld.x).Within(0.0001f), spriteTransform.name);
                        Assert.That(spriteTransform.transform.position.y,
                            Is.EqualTo(expectedWorld.y).Within(0.0001f), spriteTransform.name);
                        continue;
                    }
    
                    var provider = FindPositionProvider(spriteTransform.transform.parent);
                    var origin = provider != null ? provider.spritePosition : Vector2.zero;
                    var delta = spriteTransform.position - origin;
                    var expected = new Vector3(delta.x, -delta.y, 0f);
                    Assert.That(spriteTransform.transform.localPosition.x,
                        Is.EqualTo(expected.x).Within(0.0001f), spriteTransform.name);
                    Assert.That(spriteTransform.transform.localPosition.y,
                        Is.EqualTo(expected.y).Within(0.0001f), spriteTransform.name);
                }
            }

    [Test]
            public void Apply_ConvertsRawFlaPositionRelativeToSpriteTransformPosition()
            {
                var root = new GameObject("plant");
                var basic = new GameObject("basic");
                var child = new GameObject("child");
    
                try
                {
                    root.AddComponent<SpriteGroup>();
                    basic.transform.SetParent(root.transform, false);
                    var basicTransform = basic.AddComponent<SpriteTransform>();
                    basicTransform.providesChildSpritePosition = true;
                    basicTransform.spritePosition = new Vector2(40.2f, 52.25f);
                    child.transform.SetParent(basic.transform, false);
    
                    var spriteTransform = child.AddComponent<SpriteTransform>();
                    spriteTransform.position = new Vector2(69.65f, 55.5405f);
                    spriteTransform.updatePosition = true;
                    spriteTransform.Apply();
    
                    Assert.That(child.transform.localPosition.x, Is.EqualTo(29.45f).Within(0.0001f));
                    Assert.That(child.transform.localPosition.y, Is.EqualTo(-3.2905f).Within(0.0001f));
                }
                finally
                {
                    Object.DestroyImmediate(root);
                }
            }

    [Test]
            public void ChildPositionProvider_OwnTransformUsesPlant_AndDescendantsUseStaticPosition()
            {
                var root = new GameObject("plant");
                var basic = new GameObject("basic");
                var head = new GameObject("head");
                var pod = new GameObject("pod");
                var blink = new GameObject("blink");
    
                try
                {
                    root.AddComponent<SpriteGroup>();
                    basic.transform.SetParent(root.transform, false);
                    var basicTransform = basic.AddComponent<SpriteTransform>();
                    basicTransform.providesChildSpritePosition = true;
                    basicTransform.spritePosition = new Vector2(40.2f, 52.25f);
    
                    head.transform.SetParent(basic.transform, false);
                    var headTransform = head.AddComponent<SpriteTransform>();
                    headTransform.position = new Vector2(37.6f, 48.7f);
                    headTransform.updatePosition = true;
                    headTransform.providesChildSpritePosition = true;
                    headTransform.spritePosition = Vector2.zero;
                    headTransform.Apply();
    
                    pod.transform.SetParent(head.transform, false);
                    blink.transform.SetParent(pod.transform, false);
                    var blinkTransform = blink.AddComponent<SpriteTransform>();
                    blinkTransform.position = new Vector2(24.2f, -18.6f);
                    blinkTransform.updatePosition = true;
                    blinkTransform.Apply();
    
                    Assert.That(head.transform.localPosition.x, Is.EqualTo(-2.6f).Within(0.0001f));
                    Assert.That(head.transform.localPosition.y, Is.EqualTo(3.55f).Within(0.0001f));
                    Assert.That(blink.transform.localPosition.x, Is.EqualTo(24.2f).Within(0.0001f));
                    Assert.That(blink.transform.localPosition.y, Is.EqualTo(18.6f).Within(0.0001f));
                }
                finally
                {
                    Object.DestroyImmediate(root);
                }
            }
    }
}
