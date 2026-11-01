using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using PvZ.Presentation.Rendering;
using UnityEditor;
using UnityEngine;

namespace PvZ.Editor.PrefabPipeline.Zombies.ZombieNormal
{
    public static partial class ZombieNormalPrefabBuilder
    {
        private static HashSet<string> ReadClipPartNames(string xmlPath)
        {
            var document = new XmlDocument();
            document.Load(Path.Combine(Application.dataPath, xmlPath.Substring("Assets/".Length)));
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (XmlNode layer in document.SelectNodes("/animate/layer"))
            {
                var name = layer.Attributes?["name"]?.Value;
                if (name == "_ground") continue;
                if (string.IsNullOrEmpty(name) || !names.Add(ResolveWalkLayerPath(name)))
                    throw new InvalidDataException($"Animation XML has an empty or duplicate part: {xmlPath}");
            }
            return names;
        }

        private static void ConfigurePartVisibility(AnimationClip clip, string xmlPath)
        {
            var visible = ReadClipPartNames(xmlPath);
            // Explicit visibility in every state restores the walking hand after
            // eating, without depending on Animator Write Defaults or last pose.
            foreach (var part in Parts)
            {
                var curve = AnimationCurve.Constant(0f, clip.length, visible.Contains(part.Name) ? 1f : 0f);
                for (var index = 0; index < curve.length; index++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.Constant);
                    AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.Constant);
                }
                AnimationUtility.SetEditorCurve(clip,
                    EditorCurveBinding.FloatCurve(PartPathPrefix + part.Name, typeof(GameObject), "m_IsActive"), curve);
            }
            EditorUtility.SetDirty(clip);
        }

        [MenuItem("Tools/PvZ/Preview Normal Zombie Eating Poses")]
        public static void RenderEatingPreview()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(EatPath);
            if (prefab == null || clip == null)
                throw new MissingReferenceException("Build the normal zombie eating animation before previewing it.");

            var preview = new PreviewRenderUtility();
            var container = new GameObject("ZombieEatingPoses");
            preview.AddSingleGO(container);
            Texture2D image = null;
            try
            {
                var bounds = new Bounds();
                var hasBounds = false;
                for (var pose = 0; pose < 4; pose++)
                {
                    var instance = UnityEngine.Object.Instantiate(prefab, container.transform);
                    instance.GetComponent<Animator>().enabled = false;
                    clip.SampleAnimation(instance, pose * clip.length / 4f);
                    instance.transform.localPosition = new Vector3(pose * 120f, 0f, 0f);
                    foreach (var part in instance.GetComponentsInChildren<SpriteTransform>(true)) part.Apply();
                    foreach (var renderer in instance.GetComponentsInChildren<SpriteRenderer>())
                    {
                        if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                        else bounds.Encapsulate(renderer.bounds);
                    }
                }

                var camera = preview.camera;
                camera.cameraType = CameraType.Preview;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.85f, 0.88f, 0.9f, 1f);
                camera.orthographic = true;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                camera.transform.position = bounds.center - Vector3.forward * 10f;
                camera.transform.rotation = Quaternion.identity;
                camera.orthographicSize = Mathf.Max(bounds.extents.y, bounds.extents.x / 3f) * 1.1f;
                preview.BeginStaticPreview(new Rect(0f, 0f, 768f, 256f));
                camera.Render();
                image = preview.EndStaticPreview();
                var outputDirectory = Path.Combine(Application.dataPath, "../Logs");
                Directory.CreateDirectory(outputDirectory);
                var outputPath = Path.Combine(outputDirectory, "ZombieEatingPreview.png");
                File.WriteAllBytes(outputPath, image.EncodeToPNG());
                Debug.Log($"Normal zombie eating preview rendered: {Path.GetFullPath(outputPath)}");
            }
            finally
            {
                if (image != null) UnityEngine.Object.DestroyImmediate(image);
                preview.Cleanup();
            }
        }
    }
}
