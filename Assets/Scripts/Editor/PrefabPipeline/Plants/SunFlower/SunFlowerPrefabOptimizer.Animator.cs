using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using PvZ.Gameplay.Plants;
using PvZ.Gameplay.Plants.Types;
using PvZ.Gameplay.Plants.Abilities;
using PvZ.Presentation.Rendering;
using PvZ.Gameplay.Presentation.EntityPreviews;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static PvZ.Editor.PrefabPipeline.Common.AnimatorControllerUtility;
using static PvZ.Editor.PrefabPipeline.Common.PrefabHierarchyUtility;

namespace PvZ.Editor.PrefabPipeline.Plants.SunFlower
{
    public static partial class SunFlowerPrefabOptimizer
    {
        private static void ConfigureAnimatorController(
            AnimationClip idleClip,
            AnimationClip noSunClip,
            AnimationClip sunClip,
            AnimationClip blinkClip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }
    
            ConfigureParameters(controller);
    
            var baseLayer = controller.layers.Length > 0
                ? controller.layers[0]
                : GetOrCreateLayer(controller, "SunFlower");
            baseLayer.name = "SunFlower";
            baseLayer.defaultWeight = 1f;
            var idleState = GetOrCreateState(baseLayer.stateMachine, "idle");
            idleState.motion = idleClip;
            baseLayer.stateMachine.defaultState = idleState;
            SaveLayer(controller, baseLayer);
    
            ConfigureTriggeredLayer(
                controller,
                "SunFlower_sun",
                "nosun",
                noSunClip,
                "sun",
                sunClip,
                "produce");
            ConfigureTriggeredLayer(
                controller,
                "SunFlower_blink",
                "blink_idle",
                null,
                "blink",
                blinkClip,
                "blink");
    
            EditorUtility.SetDirty(controller);
        }

        private static void ConfigureParameters(AnimatorController controller)
        {
            var parameters = new List<AnimatorControllerParameter>();
            foreach (var parameter in controller.parameters)
            {
                if (parameter.name == "produce" || parameter.name == "blink") continue;
                parameters.Add(parameter);
            }
    
            parameters.Add(new AnimatorControllerParameter
            {
                name = "produce",
                type = AnimatorControllerParameterType.Trigger
            });
            parameters.Add(new AnimatorControllerParameter
            {
                name = "blink",
                type = AnimatorControllerParameterType.Trigger
            });
            controller.parameters = parameters.ToArray();
        }

        private static void ConfigureTriggeredLayer(
            AnimatorController controller,
            string layerName,
            string idleStateName,
            Motion idleMotion,
            string activeStateName,
            Motion activeMotion,
            string triggerName)
        {
            var layer = GetOrCreateLayer(controller, layerName);
            layer.defaultWeight = 1f;
            var stateMachine = layer.stateMachine;
            var idleState = GetOrCreateState(stateMachine, idleStateName);
            var activeState = GetOrCreateState(stateMachine, activeStateName);
            idleState.motion = idleMotion;
            activeState.motion = activeMotion;
            stateMachine.defaultState = idleState;
    
            ClearTransitions(idleState);
            ClearTransitions(activeState);
    
            var enterTransition = idleState.AddTransition(activeState);
            enterTransition.duration = 0f;
            enterTransition.hasExitTime = false;
            enterTransition.canTransitionToSelf = false;
            enterTransition.AddCondition(AnimatorConditionMode.If, 0f, triggerName);
    
            var exitTransition = activeState.AddTransition(idleState);
            exitTransition.duration = 0f;
            exitTransition.exitTime = 1f;
            exitTransition.hasExitTime = true;
            exitTransition.canTransitionToSelf = false;
    
            SaveLayer(controller, layer);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(idleState);
            EditorUtility.SetDirty(activeState);
        }

        private static void ConfigureAnchors(Transform root)
        {
            var anchors = root.Find("component/anchors");
            var sunAnchor = anchors != null ? anchors.Find("Sun_Anchor") : null;
            var head = root.Find(HeadPath);
            if (anchors == null || sunAnchor == null || head == null)
            {
                throw new MissingReferenceException("SunFlower production anchor hierarchy is incomplete.");
            }
    
            // The sun is centered on its root Transform. Keep production independent
            // from the animated head, but place its fixed origin at the default head
            // center instead of the old unconverted (0, 25) plant-space value.
            sunAnchor.localPosition = anchors.InverseTransformPoint(head.position);
            sunAnchor.localRotation = Quaternion.identity;
            sunAnchor.localScale = Vector3.one;
            SetLayerRecursively(anchors.gameObject, root.gameObject.layer);
        }
    }
}
