using Script;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SunFlowerProjectPreview
{
    private const string SunFlowerPrefabGuid = "13b7c69b577c547469034415641d0f87";
    private const int PreviewSize = 256;

    private static Texture2D previewTexture;
    private static bool previewRequested;

    static SunFlowerProjectPreview()
    {
        EditorApplication.projectWindowItemOnGUI += DrawProjectPreview;
        EditorApplication.projectChanged += InvalidatePreview;
        AssemblyReloadEvents.beforeAssemblyReload += ReleasePreview;
    }

    private static void DrawProjectPreview(string guid, Rect selectionRect)
    {
        if (guid != SunFlowerPrefabGuid) return;

        if (previewTexture == null && !previewRequested)
        {
            previewRequested = true;
            EditorApplication.delayCall += GeneratePreview;
        }

        if (previewTexture == null) return;

        var iconRect = selectionRect.height > 20f
            ? new Rect(selectionRect.x, selectionRect.y, selectionRect.width, selectionRect.width)
            : new Rect(selectionRect.x, selectionRect.y, selectionRect.height, selectionRect.height);

        GUI.DrawTexture(iconRect, previewTexture, ScaleMode.ScaleToFit, true);
    }

    private static void GeneratePreview()
    {
        previewRequested = false;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += GeneratePreview;
            previewRequested = true;
            return;
        }

        var prefabPath = AssetDatabase.GUIDToAssetPath(SunFlowerPrefabGuid);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return;

        ReleasePreview();
        previewTexture = RenderPreview(prefab, PreviewSize, PreviewSize);
        EditorApplication.RepaintProjectWindow();
    }

    private static Texture2D RenderPreview(GameObject prefab, int width, int height)
    {
        var previewUtility = new PreviewRenderUtility();
        try
        {
            var instance = Object.Instantiate(prefab);
            instance.hideFlags = HideFlags.HideAndDontSave;

            foreach (var animator in instance.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
            }

            foreach (var spriteTransform in instance.GetComponentsInChildren<SpriteTransform>(true))
            {
                spriteTransform.Apply();
            }

            previewUtility.AddSingleGO(instance);

            if (!TryGetRendererBounds(instance, out var bounds)) return null;

            var camera = previewUtility.camera;
            camera.cameraType = CameraType.Preview;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.orthographic = true;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z - 10f);
            camera.transform.rotation = Quaternion.identity;

            var aspect = width / (float)height;
            camera.orthographicSize = Mathf.Max(bounds.extents.y, bounds.extents.x / aspect) * 1.1f;

            previewUtility.BeginStaticPreview(new Rect(0f, 0f, width, height));
            camera.Render();
            return previewUtility.EndStaticPreview();
        }
        finally
        {
            previewUtility.Cleanup();
        }
    }

    private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        bounds = default;
        var hasBounds = false;

        foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!renderer.enabled || renderer.sprite == null) continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds && bounds.extents.sqrMagnitude > 0f;
    }

    private static void InvalidatePreview()
    {
        ReleasePreview();
        EditorApplication.RepaintProjectWindow();
    }

    private static void ReleasePreview()
    {
        if (previewTexture == null) return;

        Object.DestroyImmediate(previewTexture);
        previewTexture = null;
    }
}
