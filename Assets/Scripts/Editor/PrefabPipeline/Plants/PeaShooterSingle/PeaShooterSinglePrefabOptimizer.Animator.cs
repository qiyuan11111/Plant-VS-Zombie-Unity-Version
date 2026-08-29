using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LitJson;
using PvZ.Gameplay.Detection;
using PvZ.Gameplay.Plants.Abilities;
using PvZ.Gameplay.Plants.Presentation.Animation;
using PvZ.Gameplay.Plants.Types;
using PvZ.Presentation.Rendering;
using PvZ.Gameplay.Presentation.EntityPreviews;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static PvZ.Editor.PrefabPipeline.Common.AnimatorControllerUtility;
using static PvZ.Editor.PrefabPipeline.Common.PrefabHierarchyUtility;

namespace PvZ.Editor.PrefabPipeline.Plants.PeaShooterSingle
{
    public static partial class PeaShooterSinglePrefabOptimizer
    {
        private static AnimatorController ConfigureController(
            AnimationClip idleClip,
            AnimationClip headIdleClip,
            AnimationClip shootClip,
            AnimationClip blinkClip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }
    
            EnsureTrigger(controller, "shoot");
            EnsureTrigger(controller, "blink");
            ConfigureDefaultState(controller, "PeaShooterSingle", "idle", idleClip, IdleStateSpeed);
            ConfigureDefaultState(controller, "PeaShooterSingle_head", "head_idle", headIdleClip, IdleStateSpeed);
            RemoveShootStateFromHeadIdleLayer(controller);
            ConfigureBlinkOverlayLayer(controller, blinkClip);
            ConfigureShootOverlayLayer(controller, shootClip);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void EnsureTrigger(AnimatorController controller, string parameterName)
        {
            var parameters = controller.parameters;
            for (var index = 0; index < parameters.Length; index++)
            {
                if (parameters[index].name != parameterName) continue;
                if (parameters[index].type == AnimatorControllerParameterType.Trigger) return;
                parameters[index] = new AnimatorControllerParameter
                {
                    name = parameterName,
                    type = AnimatorControllerParameterType.Trigger
                };
                controller.parameters = parameters;
                return;
            }
    
            controller.AddParameter(parameterName, AnimatorControllerParameterType.Trigger);
        }

        private static void ConfigureDefaultState(
            AnimatorController controller,
            string layerName,
            string stateName,
            AnimationClip clip,
            float speed)
        {
            var layer = GetOrCreateLayer(controller, layerName);
            var state = GetOrCreateState(layer.stateMachine, stateName);
            state.motion = clip;
            state.speed = speed;
            state.writeDefaultValues = false;
            layer.stateMachine.defaultState = state;
            SaveLayer(controller, layer);
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(layer.stateMachine);
        }

        private static void RemoveShootStateFromHeadIdleLayer(AnimatorController controller)
        {
            var layer = GetOrCreateLayer(controller, "PeaShooterSingle_head");
            layer.defaultWeight = 1f;
            var stateMachine = layer.stateMachine;
            var idleState = stateMachine.defaultState;
            if (idleState != null)
            {
                foreach (AnimatorStateTransition transition in idleState.transitions)
                {
                    idleState.RemoveTransition(transition);
                }
            }
    
            foreach (AnimatorState shootState in stateMachine.states
                         .Select(childState => childState.state)
                         .Where(state => state.name == "shoot")
                         .ToArray())
            {
                stateMachine.RemoveState(shootState);
            }
    
            SaveLayer(controller, layer);
            EditorUtility.SetDirty(idleState);
            EditorUtility.SetDirty(stateMachine);
        }

        private static void ConfigureBlinkOverlayLayer(
            AnimatorController controller,
            AnimationClip blinkClip)
        {
            var layer = GetOrCreateLayer(controller, "PeaShooterSingle_blink");
            layer.defaultWeight = 1f;
            var stateMachine = layer.stateMachine;
    
            var inactiveState = GetOrCreateState(stateMachine, "blink_idle");
            inactiveState.motion = null;
            inactiveState.speed = 1f;
            inactiveState.cycleOffset = 0f;
            inactiveState.writeDefaultValues = false;
            stateMachine.defaultState = inactiveState;
    
            var blinkState = GetOrCreateState(stateMachine, "blink");
            blinkState.motion = blinkClip;
            blinkState.speed = 1f;
            blinkState.writeDefaultValues = false;
            foreach (var behaviour in blinkState.behaviours.ToArray())
            {
                UnityEngine.Object.DestroyImmediate(behaviour, true);
            }
    
            ClearTransitions(inactiveState);
            ClearTransitions(blinkState);
    
            var enterTransition = inactiveState.AddTransition(blinkState);
            enterTransition.duration = 0f;
            enterTransition.hasFixedDuration = true;
            enterTransition.hasExitTime = false;
            enterTransition.offset = 0f;
            enterTransition.canTransitionToSelf = false;
            enterTransition.AddCondition(AnimatorConditionMode.If, 0f, "blink");
    
            var restartTransition = blinkState.AddTransition(blinkState);
            restartTransition.duration = 0f;
            restartTransition.hasFixedDuration = true;
            restartTransition.hasExitTime = false;
            restartTransition.offset = 0f;
            restartTransition.canTransitionToSelf = true;
            restartTransition.AddCondition(AnimatorConditionMode.If, 0f, "blink");
    
            SaveLayer(controller, layer);
            EditorUtility.SetDirty(inactiveState);
            EditorUtility.SetDirty(blinkState);
            EditorUtility.SetDirty(stateMachine);
        }

        private static void ConfigureShootOverlayLayer(
            AnimatorController controller,
            AnimationClip shootClip)
        {
            var layer = GetOrCreateLayer(controller, "PeaShooterSingle_head_shoot");
            layer.defaultWeight = 0f;
            var stateMachine = layer.stateMachine;
    
            var inactiveState = GetOrCreateState(stateMachine, "shoot_idle");
            inactiveState.motion = null;
            inactiveState.speed = 1f;
            inactiveState.writeDefaultValues = false;
            stateMachine.defaultState = inactiveState;
    
            var shootState = GetOrCreateState(stateMachine, "shoot");
            shootState.motion = shootClip;
            shootState.speed = ShootStateSpeed;
            shootState.writeDefaultValues = false;
            if (!shootState.behaviours.OfType<PeaShooterShootOverlayStateBehaviour>().Any())
            {
                shootState.AddStateMachineBehaviour<PeaShooterShootOverlayStateBehaviour>();
            }
    
            var enterTransition = inactiveState.transitions.FirstOrDefault(transition =>
                transition.destinationState == shootState &&
                transition.conditions.Length == 1 &&
                transition.conditions[0].parameter == "shoot");
            if (enterTransition == null)
            {
                enterTransition = inactiveState.AddTransition(shootState);
                enterTransition.AddCondition(AnimatorConditionMode.If, 0f, "shoot");
            }
            foreach (AnimatorStateTransition transition in inactiveState.transitions)
            {
                if (transition != enterTransition) inactiveState.RemoveTransition(transition);
            }
            enterTransition.duration = 0f;
            enterTransition.hasFixedDuration = true;
            enterTransition.hasExitTime = false;
            enterTransition.offset = 0f;
            enterTransition.canTransitionToSelf = false;
    
            foreach (AnimatorStateTransition transition in shootState.transitions)
            {
                shootState.RemoveTransition(transition);
            }
    
            SaveLayer(controller, layer);
            MoveLayerToLast(controller, layer.stateMachine);
            EditorUtility.SetDirty(inactiveState);
            EditorUtility.SetDirty(shootState);
            EditorUtility.SetDirty(stateMachine);
        }

        private static void MoveLayerToLast(
            AnimatorController controller,
            AnimatorStateMachine stateMachine)
        {
            var layers = controller.layers.ToList();
            int index = layers.FindIndex(layer => layer.stateMachine == stateMachine);
            if (index < 0 || index == layers.Count - 1) return;
    
            var layer = layers[index];
            layers.RemoveAt(index);
            layers.Add(layer);
            controller.layers = layers.ToArray();
        }

        private static bool StateUsesClip(
            AnimatorController controller,
            string layerName,
            string stateName,
            AnimationClip clip)
        {
            if (controller == null || clip == null) return false;
            return controller.layers
                .Where(layer => layer.name == layerName)
                .SelectMany(layer => layer.stateMachine.states)
                .Any(childState => childState.state.name == stateName && childState.state.motion == clip);
        }

        private static bool ControllerIsCurrent(
            AnimatorController controller,
            AnimationClip idleClip,
            AnimationClip headIdleClip,
            AnimationClip shootClip,
            AnimationClip blinkClip)
        {
            var bodyIdleState = FindState(controller, "PeaShooterSingle", "idle");
            var headIdleState = FindState(controller, "PeaShooterSingle_head", "head_idle");
            if (!StateUsesClip(controller, "PeaShooterSingle", "idle", idleClip) ||
                !StateUsesClip(controller, "PeaShooterSingle_head", "head_idle", headIdleClip) ||
                !StateUsesClip(controller, "PeaShooterSingle_head_shoot", "shoot", shootClip) ||
                !StateUsesClip(controller, "PeaShooterSingle_blink", "blink", blinkClip) ||
                bodyIdleState == null || bodyIdleState.writeDefaultValues ||
                !Mathf.Approximately(bodyIdleState.speed, IdleStateSpeed) ||
                headIdleState == null || headIdleState.writeDefaultValues ||
                !Mathf.Approximately(headIdleState.speed, IdleStateSpeed) ||
                !controller.parameters.Any(parameter =>
                    parameter.name == "shoot" && parameter.type == AnimatorControllerParameterType.Trigger) ||
                !controller.parameters.Any(parameter =>
                    parameter.name == "blink" && parameter.type == AnimatorControllerParameterType.Trigger) ||
                !BlinkLayerIsCurrent(controller, blinkClip))
            {
                return false;
            }
    
            var headLayer = controller.layers.SingleOrDefault(layer => layer.name == "PeaShooterSingle_head");
            if (headLayer == null || headLayer.stateMachine.defaultState != headIdleState ||
                headIdleState.transitions.Length != 0 ||
                headLayer.stateMachine.states.Any(childState => childState.state.name == "shoot"))
            {
                return false;
            }
    
            var shootLayer = controller.layers.SingleOrDefault(layer => layer.name == "PeaShooterSingle_head_shoot");
            if (shootLayer == null ||
                controller.layers[controller.layers.Length - 1].stateMachine != shootLayer.stateMachine ||
                !Mathf.Approximately(shootLayer.defaultWeight, 0f))
            {
                return false;
            }
    
            var inactiveState = shootLayer.stateMachine.defaultState;
            var shootState = shootLayer.stateMachine.states
                .Select(childState => childState.state)
                .SingleOrDefault(state => state.name == "shoot");
            if (inactiveState == null || inactiveState.name != "shoot_idle" ||
                inactiveState.motion != null || inactiveState.writeDefaultValues ||
                shootState == null || shootState == inactiveState || shootState.writeDefaultValues ||
                !Mathf.Approximately(shootState.speed, ShootStateSpeed) ||
                shootState.behaviours.OfType<PeaShooterShootOverlayStateBehaviour>().Count() != 1)
            {
                return false;
            }
    
            var enterTransition = inactiveState.transitions.SingleOrDefault();
            return enterTransition != null && enterTransition.destinationState == shootState &&
                   !enterTransition.hasExitTime && enterTransition.hasFixedDuration &&
                   Mathf.Approximately(enterTransition.duration, 0f) &&
                   enterTransition.conditions.Length == 1 &&
                   enterTransition.conditions[0].parameter == "shoot" &&
                   enterTransition.conditions[0].mode == AnimatorConditionMode.If &&
                   shootState.transitions.Length == 0;
        }

        private static bool BlinkLayerIsCurrent(
            AnimatorController controller,
            AnimationClip blinkClip)
        {
            var layer = controller.layers.SingleOrDefault(
                candidate => candidate.name == "PeaShooterSingle_blink");
            if (layer == null || !Mathf.Approximately(layer.defaultWeight, 1f))
            {
                return false;
            }
    
            var inactiveState = layer.stateMachine.defaultState;
            var blinkState = layer.stateMachine.states
                .Select(childState => childState.state)
                .SingleOrDefault(state => state.name == "blink");
            if (inactiveState == null || inactiveState.name != "blink_idle" ||
                inactiveState.motion != null || inactiveState.writeDefaultValues ||
                !Mathf.Approximately(inactiveState.speed, 1f) ||
                !Mathf.Approximately(inactiveState.cycleOffset, 0f) ||
                blinkState == null || blinkState.motion != blinkClip ||
                blinkState.writeDefaultValues || !Mathf.Approximately(blinkState.speed, 1f) ||
                blinkState.behaviours.Length != 0)
            {
                return false;
            }
    
            var enterTransition = inactiveState.transitions.SingleOrDefault();
            var restartTransition = blinkState.transitions.SingleOrDefault();
            return enterTransition != null &&
                   enterTransition.destinationState == blinkState &&
                   !enterTransition.hasExitTime &&
                   enterTransition.hasFixedDuration &&
                   Mathf.Approximately(enterTransition.duration, 0f) &&
                   enterTransition.conditions.Length == 1 &&
                   enterTransition.conditions[0].parameter == "blink" &&
                   enterTransition.conditions[0].mode == AnimatorConditionMode.If &&
                   restartTransition != null &&
                   restartTransition.destinationState == blinkState &&
                   !restartTransition.hasExitTime &&
                   restartTransition.hasFixedDuration &&
                   restartTransition.canTransitionToSelf &&
                   Mathf.Approximately(restartTransition.duration, 0f) &&
                   restartTransition.conditions.Length == 1 &&
                   restartTransition.conditions[0].parameter == "blink" &&
                   restartTransition.conditions[0].mode == AnimatorConditionMode.If;
        }

        private static AnimatorState FindState(
            AnimatorController controller,
            string layerName,
            string stateName)
        {
            if (controller == null) return null;
            return controller.layers
                .Where(layer => layer.name == layerName)
                .SelectMany(layer => layer.stateMachine.states)
                .Select(childState => childState.state)
                .SingleOrDefault(state => state.name == stateName);
        }
    }
}
