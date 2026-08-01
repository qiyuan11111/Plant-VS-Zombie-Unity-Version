using NUnit.Framework;
using Script;
using UnityEngine;

namespace Tests.EditMode
{
    public sealed class SpriteCoordinateSpaceTests
    {
        [Test]
        public void Apply_ConvertsRawFlaPositionRelativeToNearestSpace()
        {
            var root = new GameObject("root");
            var child = new GameObject("child");

            try
            {
                var space = root.AddComponent<SpriteCoordinateSpace>();
                space.SpritePosition = new Vector2(40.2f, 52.25f);
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
        public void NestedSpace_OwnTransformUsesParentSpace_AndChildrenUseNestedSpace()
        {
            var root = new GameObject("plant");
            var face = new GameObject("face");
            var blink = new GameObject("blink");

            try
            {
                var plantSpace = root.AddComponent<SpriteCoordinateSpace>();
                plantSpace.SpritePosition = new Vector2(40.2f, 52.25f);

                face.transform.SetParent(root.transform, false);
                var faceTransform = face.AddComponent<SpriteTransform>();
                faceTransform.position = new Vector2(37.1f, 35.7f);
                faceTransform.updatePosition = true;
                faceTransform.Apply();

                var faceSpace = face.AddComponent<SpriteCoordinateSpace>();
                faceSpace.SpritePosition = new Vector2(37.1f, 35.7f);

                blink.transform.SetParent(face.transform, false);
                var blinkTransform = blink.AddComponent<SpriteTransform>();
                blinkTransform.position = new Vector2(39.1f, 31.5f);
                blinkTransform.updatePosition = true;
                blinkTransform.Apply();

                Assert.That(face.transform.localPosition.x, Is.EqualTo(-3.1f).Within(0.0001f));
                Assert.That(face.transform.localPosition.y, Is.EqualTo(16.55f).Within(0.0001f));
                Assert.That(blink.transform.localPosition.x, Is.EqualTo(2f).Within(0.0001f));
                Assert.That(blink.transform.localPosition.y, Is.EqualTo(4.2f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
