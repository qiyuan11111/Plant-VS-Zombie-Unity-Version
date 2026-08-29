using System;
using System.Globalization;
using LitJson;
using UnityEditor;
using UnityEngine;

namespace PvZ.Editor.PrefabPipeline.Common
{
    internal static class AnimationAssetUtility
    {
        public static AnimationClip GetOrCreateClip(
            string path,
            string clipName,
            float frameRate = 0f)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null) return clip;

            clip = new AnimationClip { name = clipName };
            if (frameRate > 0f) clip.frameRate = frameRate;
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        public static void ClearClip(AnimationClip clip)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                AnimationUtility.SetEditorCurve(clip, binding, null);
            }

            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
            }

            AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
        }

        public static void SetClipLoopTime(AnimationClip clip, bool loopTime)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loopTime;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        public static float ParseFloat(JsonData value)
        {
            return float.Parse(
                value.ToString(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture);
        }
    }
}
