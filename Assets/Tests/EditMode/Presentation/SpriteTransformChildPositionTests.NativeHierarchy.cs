using NUnit.Framework;
using PvZ.Presentation.Rendering;
using PvZ.Gameplay.Presentation.EntityPreviews;
using UnityEditor;
using UnityEngine;

namespace PvZ.Tests.EditMode.Presentation
{
    public sealed partial class SpriteTransformChildPositionTests
    {
    [Test]
            public void NativeChild_ConvertsGlobalSourceMatrixToParentRelativeLocalMatrix()
            {
                var root = new GameObject("root");
                var provider = new GameObject("head");
                var providerContent = new GameObject(SpriteTransform.NativeContentName);
                var child = new GameObject("blink");
                var childContent = new GameObject(SpriteTransform.NativeContentName);
    
                try
                {
                    root.transform.position = new Vector3(100f, 200f, 0f);
                    root.transform.rotation = Quaternion.Euler(0f, 0f, 17f);
                    root.transform.localScale = new Vector3(1.2f, 0.8f, 1f);
    
                    provider.transform.SetParent(root.transform, false);
                    provider.transform.localPosition = new Vector3(3f, 4f, 0f);
                    providerContent.transform.SetParent(provider.transform, false);
                    var providerTransform = provider.AddComponent<SpriteTransform>();
                    providerTransform.position = new Vector2(38.55f, 34.05f);
                    providerTransform.scale = new Vector2(55.499267578125f, 50f);
                    providerTransform.skew = new Vector2(8f, -5f);
                    providerTransform.updatePosition = false;
                    providerTransform.providesChildSpritePosition = true;
                    providerTransform.providesChildSpriteAffine = true;
                    providerTransform.spritePosition = new Vector2(38.55f, 33.3f);
                    providerTransform.spriteScale = new Vector2(55.54046630859375f, 55.54046630859375f);
                    providerTransform.spriteSkew = new Vector2(11f, -7f);
                    providerTransform.ConfigureNativeHierarchy(providerContent.transform);
                    providerTransform.Apply();
    
                    child.transform.SetParent(providerContent.transform, false);
                    childContent.transform.SetParent(child.transform, false);
                    var childTransform = child.AddComponent<SpriteTransform>();
                    childTransform.position = new Vector2(45.3f, 29.85f);
                    childTransform.scale = providerTransform.spriteScale;
                    childTransform.skew = providerTransform.spriteSkew;
                    childTransform.updatePosition = true;
                    childTransform.ConfigureNativeHierarchy(childContent.transform);
                    childTransform.Apply();
    
                    var sourceDelta = childTransform.position - providerTransform.spritePosition;
                    var expectedLocalPosition = ResolveSourceLocalPosition(
                        childTransform,
                        providerTransform);
                    var expectedWorldPosition = providerContent.transform.TransformPoint(expectedLocalPosition);
    
                    Assert.That(child.transform.position.x,
                        Is.EqualTo(expectedWorldPosition.x).Within(0.0001f));
                    Assert.That(child.transform.position.y,
                        Is.EqualTo(expectedWorldPosition.y).Within(0.0001f));
                    Assert.That(child.transform.localPosition.x,
                        Is.EqualTo(expectedLocalPosition.x).Within(0.0001f));
                    Assert.That(child.transform.localPosition.y,
                        Is.EqualTo(expectedLocalPosition.y).Within(0.0001f));
                    Assert.That(Mathf.Abs(child.transform.localPosition.x - sourceDelta.x),
                        Is.GreaterThan(0.0001f));
                    Assert.That(child.transform.localScale.x, Is.EqualTo(1f).Within(0.0001f));
                    Assert.That(child.transform.localScale.y, Is.EqualTo(1f).Within(0.0001f));
                }
                finally
                {
                    Object.DestroyImmediate(root);
                }
            }

    [Test]
            public void NativeHierarchy_ConvertsEveryPlantGlobalMatrixToItsUnityLocalMatrix()
            {
                var plant = new GameObject("plant");
                var parent = new GameObject("parent");
                var parentContent = new GameObject(SpriteTransform.NativeContentName);
                var child = new GameObject("child");
                var childContent = new GameObject(SpriteTransform.NativeContentName);
    
                try
                {
                    plant.transform.position = new Vector3(120f, -35f, 0f);
                    plant.transform.rotation = Quaternion.Euler(0f, 0f, 23f);
                    plant.transform.localScale = new Vector3(1.3f, 0.75f, 1f);
    
                    parent.transform.SetParent(plant.transform, false);
                    parentContent.transform.SetParent(parent.transform, false);
                    var parentTransform = parent.AddComponent<SpriteTransform>();
                    parentTransform.position = new Vector2(32f, 40f);
                    parentTransform.scale = new Vector2(120f, 80f);
                    parentTransform.skew = new Vector2(15f, -8f);
                    parentTransform.updatePosition = true;
                    parentTransform.ConfigureNativeHierarchy(parentContent.transform);
                    parentTransform.Apply();
    
                    child.transform.SetParent(parentContent.transform, false);
                    childContent.transform.SetParent(child.transform, false);
                    var childTransform = child.AddComponent<SpriteTransform>();
                    childTransform.position = new Vector2(45f, 31f);
                    childTransform.scale = new Vector2(70f, 110f);
                    childTransform.skew = new Vector2(-12f, 5f);
                    childTransform.updatePosition = true;
                    childTransform.ConfigureNativeHierarchy(childContent.transform);
                    childTransform.Apply();
    
                    var expectedParentGlobal = BuildSourceMatrix(parentTransform);
                    var expectedChildGlobal = BuildSourceMatrix(childTransform);
                    var expectedChildLocal = expectedParentGlobal.inverse * expectedChildGlobal;
                    var actualChildLocal = parentContent.transform.worldToLocalMatrix *
                                           childContent.transform.localToWorldMatrix;
                    var actualChildGlobal = plant.transform.worldToLocalMatrix *
                                            childContent.transform.localToWorldMatrix;
    
                    AssertMatrix2D(actualChildLocal, expectedChildLocal);
                    AssertMatrix2D(actualChildGlobal, expectedChildGlobal);
                }
                finally
                {
                    Object.DestroyImmediate(plant);
                }
            }

    [Test]
            public void Apply_WritesScaleOnlyToNativeHierarchy()
            {
                var target = new GameObject("sprite");
                var content = new GameObject(SpriteTransform.NativeContentName);
    
                try
                {
                    content.transform.SetParent(target.transform, false);
                    target.transform.localScale = Vector3.one;
                    var spriteTransform = target.AddComponent<SpriteTransform>();
                    spriteTransform.ConfigureNativeHierarchy(content.transform);
                    spriteTransform.scale = new Vector2(80f, 125f);
    
                    spriteTransform.Apply();
    
                    Assert.That(target.transform.localScale.x, Is.EqualTo(0.8f).Within(0.0001f));
                    Assert.That(target.transform.localScale.y, Is.EqualTo(1.25f).Within(0.0001f));
                    Assert.That(content.transform.localScale, Is.EqualTo(Vector3.one));
                }
                finally
                {
                    Object.DestroyImmediate(target);
                }
            }

    [Test]
            public void ParentScale_IsInheritedThroughNativeHierarchy()
            {
                var parent = new GameObject("parent");
                var parentContent = new GameObject(SpriteTransform.NativeContentName);
                var child = new GameObject("child");
                var childContent = new GameObject(SpriteTransform.NativeContentName);
                Material material = null;
    
                try
                {
                    parentContent.transform.SetParent(parent.transform, false);
                    child.transform.SetParent(parentContent.transform, false);
                    child.transform.localPosition = new Vector3(10f, 20f, 0f);
                    childContent.transform.SetParent(child.transform, false);
    
                    var parentTransform = parent.AddComponent<SpriteTransform>();
                    parentTransform.ConfigureNativeHierarchy(parentContent.transform);
                    parentTransform.scale = new Vector2(200f, 50f);
    
                    var shader = Shader.Find("Custom/LightnessSkewShader");
                    Assert.That(shader, Is.Not.Null);
                    material = new Material(shader);
                    childContent.AddComponent<SpriteRenderer>().sharedMaterial = material;
                    var childTransform = child.AddComponent<SpriteTransform>();
                    childTransform.ConfigureNativeHierarchy(childContent.transform);
    
                    parentTransform.Apply();
                    childTransform.Apply();
    
                    Assert.That(parent.transform.localScale.x, Is.EqualTo(2f).Within(0.0001f));
                    Assert.That(parent.transform.localScale.y, Is.EqualTo(0.5f).Within(0.0001f));
                    Assert.That(child.transform.localPosition, Is.EqualTo(new Vector3(10f, 20f, 0f)));
                    Assert.That(child.transform.position.x, Is.EqualTo(20f).Within(0.0001f));
                    Assert.That(child.transform.position.y, Is.EqualTo(10f).Within(0.0001f));
                    Assert.That(child.transform.localScale, Is.EqualTo(Vector3.one));
                    Assert.That(material.HasProperty("_AffineRow0"), Is.False);
                    Assert.That(material.HasProperty("_ScaleX"), Is.False);
                }
                finally
                {
                    if (material != null) Object.DestroyImmediate(material);
                    Object.DestroyImmediate(parent);
                }
            }

    [Test]
            public void EmptySerializedAttachment_RestoresIdentityHierarchyScale()
            {
                var attachment = new GameObject("attachment");
                var attachmentContent = new GameObject(SpriteTransform.NativeContentName);
                var child = new GameObject("child");
                var childContent = new GameObject(SpriteTransform.NativeContentName);
                Material material = null;
    
                try
                {
                    attachmentContent.transform.SetParent(attachment.transform, false);
                    child.transform.SetParent(attachmentContent.transform, false);
                    childContent.transform.SetParent(child.transform, false);
                    var attachmentTransform = attachment.AddComponent<SpriteTransform>();
                    attachmentTransform.ConfigureNativeHierarchy(attachmentContent.transform);
                    attachmentTransform.scale = Vector2.zero;
                    attachmentTransform.brightness = 0f;
                    attachmentTransform.alpha = 0f;
                    attachmentTransform.alphaCoef = 0f;
    
                    var shader = Shader.Find("Custom/LightnessSkewShader");
                    Assert.That(shader, Is.Not.Null);
                    material = new Material(shader);
                    childContent.AddComponent<SpriteRenderer>().sharedMaterial = material;
                    var childTransform = child.AddComponent<SpriteTransform>();
                    childTransform.ConfigureNativeHierarchy(childContent.transform);
    
                    attachmentTransform.Apply();
                    childTransform.Apply();
    
                    Assert.That(attachmentTransform.scale, Is.EqualTo(new Vector2(100f, 100f)));
                    Assert.That(attachment.transform.localScale, Is.EqualTo(Vector3.one));
                    Assert.That(child.transform.localScale, Is.EqualTo(Vector3.one));
                }
                finally
                {
                    if (material != null) Object.DestroyImmediate(material);
                    Object.DestroyImmediate(attachment);
                }
            }

    [Test]
            public void EveryPrefabSpriteTransform_UsesNativeContentOnly()
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null) continue;
    
                    foreach (var spriteTransform in
                             prefab.GetComponentsInChildren<SpriteTransform>(true))
                    {
                        var objectPath = AnimationUtility.CalculateTransformPath(
                            spriteTransform.transform, prefab.transform);
                        Assert.That(spriteTransform.NativeContent, Is.Not.Null,
                            $"{path}: {objectPath}");
                        Assert.That(spriteTransform.NativeContent.parent,
                            Is.SameAs(spriteTransform.transform), $"{path}: {objectPath}");
                        Assert.That(spriteTransform.GetComponent<SpriteRenderer>(), Is.Null,
                            $"{path}: {objectPath}");
                    }
                }
            }
    }
}
