using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PvZ.Gameplay.Plants;
using PvZ.Gameplay.Plants.Types;
using PvZ.Gameplay.Plants.Abilities;
using PvZ.Gameplay.Presentation.Shadows;
using PvZ.Presentation.Rendering;
using PvZ.Gameplay.Presentation.EntityPreviews;
using PvZ.Config;
using PvZ.Gameplay.Board;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace PvZ.Tests.EditMode.Plants
{
    public sealed partial class SunFlowerConfigurationTests
    {
        private const string BasicPath = "component/basic";
        private const string BasicVisualPath = BasicPath + "/__AffineContent";
        private const string HeadPath = BasicVisualPath + "/head/SunFlower_head";
        private const string HeadVisualPath = HeadPath + "/__AffineContent";
 static float GetAnimatedValue(SpriteTransform spriteTransform, string propertyName)
        {
            switch (propertyName)
            {
                case "position.x": return spriteTransform.position.x;
                case "position.y": return spriteTransform.position.y;
                case "scale.x": return spriteTransform.scale.x;
                case "scale.y": return spriteTransform.scale.y;
                case "skew.x": return spriteTransform.skew.x;
                case "skew.y": return spriteTransform.skew.y;
                case "brightness": return spriteTransform.brightness;
                case "alpha": return spriteTransform.alpha;
                case "alphaCoef": return spriteTransform.alphaCoef;
                default: return float.NaN;
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
    }

    internal sealed class CenteredPlacementTestPlant : PlantEntity
    {
        public override string GetChineseName() => "中心落点测试植物";
        public override string GetEnglishName() => "CenteredPlacementTestPlant";
    }
}
