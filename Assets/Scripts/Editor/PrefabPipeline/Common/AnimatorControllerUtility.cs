using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace PvZ.Editor.PrefabPipeline.Common
{
    internal static class AnimatorControllerUtility
    {
        public static AnimatorControllerLayer GetOrCreateLayer(
            AnimatorController controller,
            string layerName)
        {
            foreach (var layer in controller.layers)
            {
                if (layer.name == layerName) return layer;
            }

            var stateMachine = new AnimatorStateMachine { name = layerName };
            AssetDatabase.AddObjectToAsset(stateMachine, controller);
            var layerToAdd = new AnimatorControllerLayer
            {
                name = layerName,
                defaultWeight = 1f,
                stateMachine = stateMachine
            };
            controller.AddLayer(layerToAdd);
            return layerToAdd;
        }

        public static AnimatorState GetOrCreateState(
            AnimatorStateMachine stateMachine,
            string stateName)
        {
            foreach (var childState in stateMachine.states)
            {
                if (childState.state.name == stateName) return childState.state;
            }

            return stateMachine.AddState(stateName);
        }

        public static void SaveLayer(
            AnimatorController controller,
            AnimatorControllerLayer layer)
        {
            var layers = controller.layers;
            for (var index = 0; index < layers.Length; index++)
            {
                if (layers[index].stateMachine != layer.stateMachine) continue;
                layers[index] = layer;
                controller.layers = layers;
                return;
            }
        }

        public static void ClearTransitions(AnimatorState state)
        {
            foreach (var transition in state.transitions)
            {
                state.RemoveTransition(transition);
                Object.DestroyImmediate(transition, true);
            }
        }
    }
}
