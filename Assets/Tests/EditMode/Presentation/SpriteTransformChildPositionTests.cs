using NUnit.Framework;
using PvZ.Presentation.Rendering;
using PvZ.Gameplay.Presentation.EntityPreviews;
using UnityEditor;
using UnityEngine;

namespace PvZ.Tests.EditMode.Presentation
{
    public sealed partial class SpriteTransformChildPositionTests
    {
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
