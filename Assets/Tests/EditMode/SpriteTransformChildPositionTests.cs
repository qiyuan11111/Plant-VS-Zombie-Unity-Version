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

        [Test]
        public void Apply_DoesNotWriteUnityTransformScale()
        {
            var target = new GameObject("sprite");

            try
            {
                target.transform.localScale = Vector3.one;
                var spriteTransform = target.AddComponent<SpriteTransform>();
                spriteTransform.scale = new Vector2(80f, 125f);

                spriteTransform.Apply();

                Assert.That(target.transform.localScale, Is.EqualTo(Vector3.one));
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void ParentScale_IsPushedToChildShaderAroundParentPivot()
        {
            var parent = new GameObject("parent");
            var child = new GameObject("child");
            Material material = null;

            try
            {
                child.transform.SetParent(parent.transform, false);
                child.transform.localPosition = new Vector3(10f, 20f, 0f);

                var parentTransform = parent.AddComponent<SpriteTransform>();
                parentTransform.scale = new Vector2(200f, 50f);

                var shader = Shader.Find("Custom/LightnessSkewShader");
                Assert.That(shader, Is.Not.Null);
                material = new Material(shader);
                child.AddComponent<SpriteRenderer>().sharedMaterial = material;
                var childTransform = child.AddComponent<SpriteTransform>();

                parentTransform.Apply();
                childTransform.Apply();

                var properties = new MaterialPropertyBlock();
                child.GetComponent<SpriteRenderer>().GetPropertyBlock(properties);
                var row0 = properties.GetVector("_AffineRow0");
                var row1 = properties.GetVector("_AffineRow1");

                Assert.That(row0.x, Is.EqualTo(2f).Within(0.0001f));
                Assert.That(row0.y, Is.Zero.Within(0.0001f));
                Assert.That(row0.z, Is.EqualTo(10f).Within(0.0001f));
                Assert.That(row1.x, Is.Zero.Within(0.0001f));
                Assert.That(row1.y, Is.EqualTo(0.5f).Within(0.0001f));
                Assert.That(row1.z, Is.EqualTo(-10f).Within(0.0001f));
                Assert.That(parent.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(child.transform.localScale, Is.EqualTo(Vector3.one));
            }
            finally
            {
                if (material != null) Object.DestroyImmediate(material);
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void LegacyEmptyAttachment_DoesNotCollapseDescendantRenderer()
        {
            var attachment = new GameObject("attachment");
            var child = new GameObject("child");
            Material material = null;

            try
            {
                child.transform.SetParent(attachment.transform, false);
                var attachmentTransform = attachment.AddComponent<SpriteTransform>();
                attachmentTransform.scale = Vector2.zero;
                attachmentTransform.brightness = 0f;
                attachmentTransform.alpha = 0f;
                attachmentTransform.alphaCoef = 0f;

                var shader = Shader.Find("Custom/LightnessSkewShader");
                Assert.That(shader, Is.Not.Null);
                material = new Material(shader);
                child.AddComponent<SpriteRenderer>().sharedMaterial = material;
                var childTransform = child.AddComponent<SpriteTransform>();

                attachmentTransform.Apply();
                childTransform.Apply();

                var properties = new MaterialPropertyBlock();
                child.GetComponent<SpriteRenderer>().GetPropertyBlock(properties);
                var row0 = properties.GetVector("_AffineRow0");
                var row1 = properties.GetVector("_AffineRow1");

                Assert.That(attachmentTransform.scale, Is.EqualTo(new Vector2(100f, 100f)));
                Assert.That(row0, Is.EqualTo(new Vector4(1f, 0f, 0f, 0f)));
                Assert.That(row1, Is.EqualTo(new Vector4(0f, 1f, 0f, 0f)));
            }
            finally
            {
                if (material != null) Object.DestroyImmediate(material);
                Object.DestroyImmediate(attachment);
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

    }
}
