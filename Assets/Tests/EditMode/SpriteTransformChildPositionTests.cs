using NUnit.Framework;
using PvZ.Presentation;
using UnityEditor;
using UnityEngine;

namespace Tests.EditMode
{
    public sealed class SpriteTransformChildPositionTests
    {
        [TestCase("Assets/Prefab/Plant/PeaShooterSingle/PeaShooterSingle.prefab", 39.45f, 47.75f)]
        [TestCase("Assets/Prefab/Plant/SunFlower/SunFlower.prefab", 40.4f, 42.6f)]
        [TestCase("Assets/Prefab/Plant/SunShroom/SunShroom.prefab", 42.275f, 45.875f)]
        public void PlantPrefab_BasicOwnsSpritePosition(string prefabPath, float x, float y)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null);

            var basic = prefab.transform.Find("component/basic");
            Assert.That(basic, Is.Not.Null);

            var spriteTransform = basic.GetComponent<SpriteTransform>();
            Assert.That(spriteTransform, Is.Not.Null);
            Assert.That(spriteTransform.providesChildSpritePosition, Is.True);
            Assert.That(spriteTransform.spritePosition, Is.EqualTo(new Vector2(x, y)));
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

        [TestCase("Custom/LightnessSkewShader")]
        [TestCase("Custom/Particle")]
        public void PresentationShaders_ExposeNoGeometryProperties(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            try
            {
                Assert.That(material.HasProperty("_Brightness"), Is.True);
                Assert.That(material.HasProperty("_Alpha"), Is.True);
                Assert.That(material.HasProperty("_SkewX"), Is.False);
                Assert.That(material.HasProperty("_ScaleX"), Is.False);
                Assert.That(material.HasProperty("_AffineRow0"), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        private static void AssertFirstFrame(Transform root, AnimationClip clip)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.type != typeof(SpriteTransform)) continue;

                var target = root.Find(binding.path);
                Assert.That(target, Is.Not.Null, binding.path);
                var spriteTransform = target.GetComponent<SpriteTransform>();
                Assert.That(spriteTransform, Is.Not.Null, binding.path);

                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                Assert.That(curve, Is.Not.Null, $"{binding.path}/{binding.propertyName}");
                var expected = curve.Evaluate(0f);
                var actual = binding.propertyName switch
                {
                    "position.x" => spriteTransform.position.x,
                    "position.y" => spriteTransform.position.y,
                    "scale.x" => spriteTransform.scale.x,
                    "scale.y" => spriteTransform.scale.y,
                    "skew.x" => spriteTransform.skew.x,
                    "skew.y" => spriteTransform.skew.y,
                    "brightness" => spriteTransform.brightness,
                    "alpha" => spriteTransform.alpha,
                    "alphaCoef" => spriteTransform.alphaCoef,
                    _ => float.NaN
                };

                if (float.IsNaN(actual)) continue;
                Assert.That(actual, Is.EqualTo(expected).Within(0.0001f),
                    $"{binding.path}/{binding.propertyName}");
            }
        }

        private static SpriteTransform FindPositionProvider(Transform parent)
        {
            while (parent != null)
            {
                var spriteTransform = parent.GetComponent<SpriteTransform>();
                if (spriteTransform != null && spriteTransform.providesChildSpritePosition)
                {
                    return spriteTransform;
                }

                parent = parent.parent;
            }

            return null;
        }

        private static SpriteTransform FindNativeSourceParent(Transform parent)
        {
            while (parent != null)
            {
                var spriteTransform = parent.GetComponent<SpriteTransform>();
                if (spriteTransform != null && spriteTransform.NativeContent != null &&
                    (spriteTransform.updatePosition ||
                     spriteTransform.providesChildSpritePosition ||
                     spriteTransform.providesChildSpriteAffine))
                {
                    return spriteTransform;
                }

                parent = parent.parent;
            }

            return null;
        }

        private static Vector3 ResolveSourceLocalPosition(
            SpriteTransform child,
            SpriteTransform sourceParent)
        {
            var useStaticReference = sourceParent.providesChildSpriteAffine ||
                                     !sourceParent.updatePosition;
            var parentPosition = useStaticReference
                ? sourceParent.spritePosition
                : sourceParent.position;
            var parentScale = useStaticReference
                ? sourceParent.spriteScale
                : sourceParent.scale;
            var parentSkew = useStaticReference
                ? sourceParent.spriteSkew
                : sourceParent.skew;
            if (parentScale == Vector2.zero) parentScale = new Vector2(100f, 100f);

            NativeAffineDecomposition.BuildSourceMatrix(
                parentScale,
                parentSkew,
                out var m00,
                out var m01,
                out var m10,
                out var m11);
            var determinant = m00 * m11 - m01 * m10;
            Assert.That(Mathf.Abs(determinant), Is.GreaterThan(0.0000001f));

            var delta = child.position - parentPosition;
            var x = delta.x;
            var y = -delta.y;
            return new Vector3(
                (m11 * x - m01 * y) / determinant,
                (-m10 * x + m00 * y) / determinant,
                0f);
        }

        private static Matrix4x4 BuildSourceMatrix(SpriteTransform spriteTransform)
        {
            NativeAffineDecomposition.BuildSourceMatrix(
                spriteTransform.scale,
                spriteTransform.skew,
                out var m00,
                out var m01,
                out var m10,
                out var m11);
            var matrix = Matrix4x4.identity;
            matrix.m00 = m00;
            matrix.m01 = m01;
            matrix.m10 = m10;
            matrix.m11 = m11;
            matrix.m03 = spriteTransform.position.x;
            matrix.m13 = -spriteTransform.position.y;
            return matrix;
        }

        private static void AssertMatrix2D(Matrix4x4 actual, Matrix4x4 expected)
        {
            Assert.That(actual.m00, Is.EqualTo(expected.m00).Within(0.0001f));
            Assert.That(actual.m01, Is.EqualTo(expected.m01).Within(0.0001f));
            Assert.That(actual.m03, Is.EqualTo(expected.m03).Within(0.0001f));
            Assert.That(actual.m10, Is.EqualTo(expected.m10).Within(0.0001f));
            Assert.That(actual.m11, Is.EqualTo(expected.m11).Within(0.0001f));
            Assert.That(actual.m13, Is.EqualTo(expected.m13).Within(0.0001f));
        }

    }
}
