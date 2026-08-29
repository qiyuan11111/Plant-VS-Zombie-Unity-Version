using System.Linq;
using NUnit.Framework;
using PvZ.Editor.PrefabPipeline.Plants.PeaShooterSingle;
using PvZ.Gameplay.Plants.Presentation.Animation;
using PvZ.Gameplay.Plants.Abilities;
using PvZ.Gameplay.Plants.Types;
using PvZ.Presentation.Rendering;
using PvZ.Gameplay.Presentation.EntityPreviews;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;

namespace PvZ.Tests.EditMode.Plants
{
    public sealed partial class PeaShooterSingleOptimizerTests
    {
    [Test]
            public void Prefab_HasHeadRelativeBlinkSpritesAndBlinkAbility()
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                var animator = prefab.GetComponent<Animator>();
                var shooter = prefab.GetComponent<PeaShooterSingle>();
                var blink = prefab.GetComponent<Blink>();
                Assert.That(shooter, Is.Not.Null);
                Assert.That(blink, Is.Not.Null);
    
                var shooterData = new SerializedObject(shooter);
                var blinkData = new SerializedObject(blink);
                Assert.That(shooterData.FindProperty("blink").objectReferenceValue, Is.SameAs(blink));
                Assert.That(blinkData.FindProperty("animator").objectReferenceValue, Is.SameAs(animator));
                Assert.That(blinkData.FindProperty("minimumIntervalSeconds").floatValue,
                    Is.EqualTo(2.5f).Within(0.000001f));
                Assert.That(blinkData.FindProperty("maximumIntervalSeconds").floatValue,
                    Is.EqualTo(5.5f).Within(0.000001f));
    
                var head = prefab.transform.Find(HeadPath);
                var headTransform = head.GetComponent<SpriteTransform>();
                var headRenderer = headTransform.VisualRenderer;
                Assert.That(headTransform.providesChildSpritePosition, Is.True);
                Assert.That(headTransform.providesChildSpriteAffine, Is.True);
                Assert.That(headTransform.spritePosition, Is.EqualTo(new Vector2(-0.9f, -13.7f)));
                Assert.That(headTransform.spriteScale,
                    Is.EqualTo(new Vector2(55.499268f, 50f)));
                Assert.That(headTransform.spriteSkew, Is.EqualTo(Vector2.zero));
    
                var blink1 = head.Find("__AffineContent/blink/PeaShooter_blink1");
                var blink2 = head.Find("__AffineContent/blink/PeaShooter_blink2");
                AssertBlinkPart(
                    blink1,
                    "PeaShooter_blink1",
                    headRenderer,
                    new Vector2(5.85f, -17.9f),
                    new Vector2(55.540466f, 55.540466f),
                    new Vector2(12.162323f, 8.4f),
                    new Vector2(1.0007423f, 1.1108093f));
                AssertBlinkPart(
                    blink2,
                    "PeaShooter_blink2",
                    headRenderer,
                    new Vector2(5.7699f, -17.967585f),
                    new Vector2(55.499268f, 55.499268f),
                    new Vector2(12.017996f, 8.53517f),
                    new Vector2(1f, 1.1099854f));
            }

    [Test]
            public void BlinkSprites_UseImageCenterAsUnityPivot()
            {
                foreach (var spriteName in new[] { "PeaShooter_blink1", "PeaShooter_blink2" })
                {
                    var path = $"{BlinkSpriteDirectory}/{spriteName}.png";
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    Assert.That(importer, Is.Not.Null, path);
                    Assert.That(sprite, Is.Not.Null, path);
                    Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(1f).Within(0.000001f), path);
                    Assert.That(sprite.pivot.x, Is.EqualTo(sprite.rect.width * 0.5f).Within(0.000001f), path);
                    Assert.That(sprite.pivot.y, Is.EqualTo(sprite.rect.height * 0.5f).Within(0.000001f), path);
                }
            }

    [Test]
            public void HeadSubAnimation_InheritsLiveAttachmentMotion()
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                var instance = Object.Instantiate(prefab);
                try
                {
                    var animator = instance.GetComponent<Animator>();
                    if (animator != null) animator.enabled = false;
    
                    var attachment = instance.transform.Find(HeadAttachmentPath)
                        .GetComponent<SpriteTransform>();
                    var head = instance.transform.Find(HeadPath)
                        .GetComponent<SpriteTransform>();
                    attachment.Apply();
                    head.Apply();
                    var localBefore = head.transform.localPosition;
                    var worldBefore = head.transform.position;
    
                    attachment.position += new Vector2(2.5f, -1.75f);
                    attachment.Apply();
                    head.Apply();
                    var expectedWorld = attachment.NativeContent.TransformPoint(localBefore);
    
                    Assert.That(head.transform.localPosition.x,
                        Is.EqualTo(localBefore.x).Within(0.0001f));
                    Assert.That(head.transform.localPosition.y,
                        Is.EqualTo(localBefore.y).Within(0.0001f));
                    Assert.That(head.transform.position.x,
                        Is.EqualTo(expectedWorld.x).Within(0.0001f));
                    Assert.That(head.transform.position.y,
                        Is.EqualTo(expectedWorld.y).Within(0.0001f));
                    Assert.That(Vector3.Distance(head.transform.position, worldBefore),
                        Is.GreaterThan(0.1f));
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }

    [Test]
            public void Prefab_UsesOneNativeContentLayerPerSpriteTransform()
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                var spriteTransforms = prefab.GetComponentsInChildren<SpriteTransform>(true);
    
                Assert.That(spriteTransforms, Is.Not.Empty);
                Assert.That(prefab.transform.Find(BasicPath)?.GetComponent<SpriteTransform>(), Is.Not.Null);
                var headContent = prefab.transform.Find(HeadAttachmentVisualPath);
                Assert.That(headContent, Is.Not.Null);
                Assert.That(headContent.Find("pod"), Is.Null);
                Assert.That(prefab.transform.Find(HeadPath)?.parent, Is.SameAs(headContent));
                Assert.That(prefab.transform.Find(MouthPath)?.parent, Is.SameAs(headContent));
                Assert.That(prefab.transform.Find(SproutPath)?.parent, Is.SameAs(headContent));
                foreach (var spriteTransform in spriteTransforms)
                {
                    var path = AnimationUtility.CalculateTransformPath(
                        spriteTransform.transform, prefab.transform);
                    Assert.That(spriteTransform.NativeContent, Is.Not.Null,
                        path);
                    Assert.That(spriteTransform.NativeContent.parent, Is.SameAs(spriteTransform.transform),
                        path);
                    Assert.That(spriteTransform.NativeContent.localPosition, Is.EqualTo(Vector3.zero),
                        path);
                    Assert.That(spriteTransform.GetComponent<SpriteRenderer>(), Is.Null,
                        path);
                    Assert.That(spriteTransform.transform.childCount, Is.EqualTo(1), path);
                }
            }
    }
}
