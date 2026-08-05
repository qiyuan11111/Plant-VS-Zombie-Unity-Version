using NUnit.Framework;
using PvZ.Presentation;
using PvZ.Core.Entities;
using UnityEngine;

namespace Tests.EditMode
{
    public sealed class SpriteTransformChildPositionTests
    {
        [Test]
        public void Apply_ConvertsRawFlaPositionRelativeToPlantSpritePosition()
        {
            var root = new GameObject("plant");
            var child = new GameObject("child");

            try
            {
                root.AddComponent<SpriteGroup>();
                var entitySprite = root.AddComponent<TestEntitySprite>();
                entitySprite.SpritePosition = new Vector3(40.2f, 52.25f, 0f);
                child.transform.SetParent(root.transform, false);

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
            var head = new GameObject("head");
            var pod = new GameObject("pod");
            var blink = new GameObject("blink");

            try
            {
                root.AddComponent<SpriteGroup>();
                var entitySprite = root.AddComponent<TestEntitySprite>();
                entitySprite.SpritePosition = new Vector3(40.2f, 52.25f, 0f);

                head.transform.SetParent(root.transform, false);
                var headTransform = head.AddComponent<SpriteTransform>();
                headTransform.position = new Vector2(37.6f, 48.7f);
                headTransform.updatePosition = true;
                headTransform.providesChildSpritePosition = true;
                headTransform.childSpritePosition = Vector2.zero;
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

        private sealed class TestEntitySprite : EntitySprite
        {
        }
    }
}
