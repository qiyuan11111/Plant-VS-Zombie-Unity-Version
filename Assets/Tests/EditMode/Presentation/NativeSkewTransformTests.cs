using NUnit.Framework;
using PvZ.Presentation.Rendering;
using PvZ.Gameplay.Presentation.EntityPreviews;
using UnityEngine;

namespace PvZ.Tests.EditMode.Presentation
{
    public sealed class NativeSkewTransformTests
    {
        [TestCase(70f, 120f, 0f, 0f)]
        [TestCase(100f, 100f, 25f, 0f)]
        [TestCase(125f, 70f, 20f, -12f)]
        [TestCase(110f, 90f, -18f, 8f)]
        [TestCase(75f, 120f, 0f, 28f)]
        public void NativeHierarchy_MatchesRequestedScaleAndSkew(
            float scaleX,
            float scaleY,
            float skewX,
            float skewY)
        {
            var root = new GameObject("NativeSkewTest");
            try
            {
                var nativeSkew = root.AddComponent<NativeSkewTransform>();
                nativeSkew.EnsureHierarchy();
                nativeSkew.Configure(
                    new Vector2(scaleX, scaleY),
                    new Vector2(skewX, skewY));

                var expected = nativeSkew.GetExpectedLocalMatrix();
                var actual = nativeSkew.GetNativeLocalMatrix();
                AssertMatrix2D(expected, actual);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Apply_DoesNotRewritePivotOrContentChildPosition()
        {
            var root = new GameObject("NativeSkewTest");
            try
            {
                root.transform.localPosition = new Vector3(13f, 27f, 0f);
                var nativeSkew = root.AddComponent<NativeSkewTransform>();
                nativeSkew.EnsureHierarchy();
                var child = new GameObject("Child");
                child.transform.SetParent(nativeSkew.Content, false);
                child.transform.localPosition = new Vector3(70f, 25f, 0f);

                nativeSkew.Configure(
                    new Vector2(125f, 70f),
                    new Vector2(20f, -12f));

                Assert.That(root.transform.localPosition, Is.EqualTo(new Vector3(13f, 27f, 0f)));
                Assert.That(child.transform.localPosition, Is.EqualTo(new Vector3(70f, 25f, 0f)));
                Assert.That(nativeSkew.Content.parent, Is.SameAs(root.transform));
                Assert.That(root.transform.childCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ScaleOnly_UsesDirectScaleWithoutEquivalentRotations()
        {
            var root = new GameObject("NativeSkewTest");
            try
            {
                var nativeSkew = root.AddComponent<NativeSkewTransform>();
                nativeSkew.EnsureHierarchy();
                nativeSkew.Configure(new Vector2(70f, 120f), Vector2.zero);

                Assert.That(root.transform.localRotation, Is.EqualTo(Quaternion.identity));
                Assert.That(root.transform.localScale,
                    Is.EqualTo(new Vector3(0.7f, 1.2f, 1f)));
                Assert.That(nativeSkew.Content.localRotation, Is.EqualTo(Quaternion.identity));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void AssertMatrix2D(Matrix4x4 expected, Matrix4x4 actual)
        {
            Assert.That(actual.m00, Is.EqualTo(expected.m00).Within(0.0001f));
            Assert.That(actual.m01, Is.EqualTo(expected.m01).Within(0.0001f));
            Assert.That(actual.m10, Is.EqualTo(expected.m10).Within(0.0001f));
            Assert.That(actual.m11, Is.EqualTo(expected.m11).Within(0.0001f));
            Assert.That(actual.m03, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(actual.m13, Is.EqualTo(0f).Within(0.0001f));
        }
    }
}
