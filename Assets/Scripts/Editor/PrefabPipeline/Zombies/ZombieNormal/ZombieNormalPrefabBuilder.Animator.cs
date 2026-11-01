using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using PvZ.Config;
using PvZ.Gameplay.Detection;
using PvZ.Gameplay.Zombies;
using PvZ.Presentation.Rendering;
using PvZ.Gameplay.Presentation.EntityPreviews;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static PvZ.Editor.PrefabPipeline.Common.PrefabHierarchyUtility;

namespace PvZ.Editor.PrefabPipeline.Zombies.ZombieNormal
{
    public static partial class ZombieNormalPrefabBuilder
    {
        private static AnimatorController CreateController(AnimationClip idle, AnimationClip walk, AnimationClip eat)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }
    
            var stateMachine = controller.layers[0].stateMachine;
            var idleState = FindOrCreateState(stateMachine, "idle");
            var walkState = FindOrCreateState(stateMachine, "walk");
            var eatState = FindOrCreateState(stateMachine, "eat");
            idleState.motion = idle;
            walkState.motion = walk;
            eatState.motion = eat;
            idleState.writeDefaultValues = true;
            walkState.writeDefaultValues = true;
            eatState.writeDefaultValues = true;

            var hasEatingParameter = false;
            var parameters = controller.parameters;
            foreach (var parameter in parameters)
            {
                if (parameter.name != "isEating") continue;
                if (parameter.type != AnimatorControllerParameterType.Bool)
                    throw new InvalidOperationException("ZombieNormal isEating parameter must be a bool.");
                parameter.defaultBool = false;
                hasEatingParameter = true;
            }
            controller.parameters = parameters;
            if (!hasEatingParameter)
                controller.AddParameter("isEating", AnimatorControllerParameterType.Bool);

            ConfigureEatingTransition(walkState, eatState, true);
            ConfigureEatingTransition(idleState, eatState, true);
            ConfigureEatingTransition(eatState, walkState, false);
            stateMachine.defaultState = walkState;
            EditorUtility.SetDirty(idleState);
            EditorUtility.SetDirty(walkState);
            EditorUtility.SetDirty(eatState);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void ConfigureEatingTransition(AnimatorState source, AnimatorState destination, bool eating)
        {
            AnimatorStateTransition transition = null;
            foreach (var existing in source.transitions)
            {
                if (existing.destinationState == destination)
                {
                    transition = existing;
                    break;
                }
            }
            if (transition == null) transition = source.AddTransition(destination);
            transition.hasExitTime = false;
            // Switch immediately so the walk clip cannot contribute root motion
            // while eating. Every state explicitly restores its own part visibility.
            transition.hasFixedDuration = true;
            transition.duration = 0f;
            transition.offset = 0f;
            transition.conditions = new[]
            {
                new AnimatorCondition
                {
                    parameter = "isEating",
                    mode = eating ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot
                }
            };
            EditorUtility.SetDirty(transition);
        }

        private static AnimatorState FindOrCreateState(AnimatorStateMachine stateMachine, string stateName)
        {
            foreach (var childState in stateMachine.states)
            {
                if (childState.state.name == stateName) return childState.state;
            }
    
            return stateMachine.AddState(stateName);
        }
    }
}
