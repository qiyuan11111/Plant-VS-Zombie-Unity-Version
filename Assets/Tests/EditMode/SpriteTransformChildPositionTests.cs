using NUnit.Framework;
using Script;
using Script.Model;
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

        private sealed class TestEntitySprite : EntitySprite
        {
        }
    }
}
