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
        private const string PrefabPath = "Assets/Prefab/Plant/PeaShooterSingle/PeaShooterSingle.prefab";
        private const string AnimationDirectory = "Assets/Prefab/Plant/PeaShooterSingle/Animation";
        private const string ControllerPath = AnimationDirectory + "/PeaShooterSingle.controller";
        private const string BasicPath = "component/basic";
        private const string BasicVisualPath = BasicPath + "/__AffineContent";
        private const string HeadAttachmentPath = BasicVisualPath + "/head";
        private const string HeadAttachmentVisualPath = HeadAttachmentPath + "/__AffineContent";
        private const string HeadPath = HeadAttachmentVisualPath + "/head";
        private const string MouthPath = HeadAttachmentVisualPath + "/mouth";
        private const string SproutPath = HeadAttachmentVisualPath + "/sprout";
        private const string StalkTopPath = BasicVisualPath + "/stalk/__AffineContent/top";
        private const string StalkBottomPath = BasicVisualPath + "/stalk/__AffineContent/bottom";
        private const string BlinkSpriteDirectory =
            "Assets/Prefab/Plant/PeaShooterSingle/Sprite";
        private static void AssertStepCurve(
            AnimationClip clip,
            string path,
            params float[] expectedValues)
        {
            var binding = AnimationUtility.GetCurveBindings(clip)
                .Single(candidate => candidate.path == path &&
                                     candidate.type == typeof(GameObject) &&
                                     candidate.propertyName == "m_IsActive");
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            Assert.That(curve.keys.Length, Is.EqualTo(expectedValues.Length));
            for (var index = 0; index < expectedValues.Length; index++)
            {
                Assert.That(curve.keys[index].time,
                    Is.EqualTo(index / 12f).Within(0.000001f));
                Assert.That(curve.keys[index].value,
                    Is.EqualTo(expectedValues[index]).Within(0.000001f));
                Assert.That(AnimationUtility.GetKeyLeftTangentMode(curve, index),
                    Is.EqualTo(AnimationUtility.TangentMode.Constant));
                Assert.That(AnimationUtility.GetKeyRightTangentMode(curve, index),
                    Is.EqualTo(AnimationUtility.TangentMode.Constant));
            }
        }

        private static void AssertBlinkPart(
            Transform target,
            string expectedSpriteName,
            SpriteRenderer headRenderer,
            Vector2 expectedPosition,
            Vector2 expectedScale,
            Vector2 expectedLocalPosition,
            Vector2 expectedLocalScale)
        {
            Assert.That(target, Is.Not.Null);
            Assert.That(target.gameObject.activeSelf, Is.False);
            var spriteTransform = target.GetComponent<SpriteTransform>();
            var renderer = spriteTransform.VisualRenderer;
            Assert.That(renderer.sprite.name, Is.EqualTo(expectedSpriteName));
            Assert.That(renderer.sharedMaterial, Is.SameAs(headRenderer.sharedMaterial));
            Assert.That(renderer.sortingLayerID, Is.EqualTo(headRenderer.sortingLayerID));
            Assert.That(renderer.sortingOrder, Is.EqualTo(10));
            Assert.That(spriteTransform.position, Is.EqualTo(expectedPosition));
            Assert.That(spriteTransform.scale, Is.EqualTo(expectedScale));
            Assert.That(target.localPosition.x,
                Is.EqualTo(expectedLocalPosition.x).Within(0.0001f));
            Assert.That(target.localPosition.y,
                Is.EqualTo(expectedLocalPosition.y).Within(0.0001f));
            Assert.That(target.localScale.x,
                Is.EqualTo(expectedLocalScale.x).Within(0.0001f));
            Assert.That(target.localScale.y,
                Is.EqualTo(expectedLocalScale.y).Within(0.0001f));
            Assert.That(spriteTransform.updatePosition, Is.True);
            Assert.That(spriteTransform.NativeContent, Is.Not.Null);
        }
        private static void AssertFirstCurveValue(
            AnimationClip clip,
            string path,
            string propertyName,
            float expectedValue)
        {
            var binding = AnimationUtility.GetCurveBindings(clip)
                .Single(candidate => candidate.path == path && candidate.propertyName == propertyName);
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            Assert.That(curve.keys[0].value, Is.EqualTo(expectedValue).Within(0.000001f),
                $"{path}/{propertyName}, first key");
        }

        private static void AssertCurveValues(
            AnimationClip clip,
            string path,
            string propertyName,
            params float[] expectedValues)
        {
            var binding = AnimationUtility.GetCurveBindings(clip)
                .Single(candidate => candidate.path == path && candidate.propertyName == propertyName);
            var keys = AnimationUtility.GetEditorCurve(clip, binding).keys;
            Assert.That(keys.Length, Is.EqualTo(expectedValues.Length));
            for (var index = 0; index < keys.Length; index++)
            {
                Assert.That(keys[index].value, Is.EqualTo(expectedValues[index]).Within(0.000001f),
                    $"{path}/{propertyName}, key {index}");
            }
        }

        private static void AssertStateMotion(
            AnimatorController controller,
            string layerName,
            string stateName,
            string clipName)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationDirectory}/{clipName}.anim");
            var state = controller.layers
                .Single(layer => layer.name == layerName)
                .stateMachine.states
                .Select(childState => childState.state)
                .Single(candidate => candidate.name == stateName);
            Assert.That(state.motion, Is.SameAs(clip));
        }

        private static AnimatorState FindState(
            AnimatorController controller,
            string layerName,
            string stateName)
        {
            return controller.layers
                .Single(layer => layer.name == layerName)
                .stateMachine.states
                .Select(childState => childState.state)
                .Single(state => state.name == stateName);
        }
    }
}
