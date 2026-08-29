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
        private static AnimatorController CreateController(AnimationClip idle, AnimationClip walk)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }
    
            var stateMachine = controller.layers[0].stateMachine;
            var idleState = FindOrCreateState(stateMachine, "idle");
            var walkState = FindOrCreateState(stateMachine, "walk");
            idleState.motion = idle;
            walkState.motion = walk;
            idleState.writeDefaultValues = true;
            walkState.writeDefaultValues = true;
            stateMachine.defaultState = walkState;
            EditorUtility.SetDirty(idleState);
            EditorUtility.SetDirty(walkState);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            return controller;
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
