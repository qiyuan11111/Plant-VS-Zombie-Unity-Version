using PvZ.Presentation;
using UnityEditor;
using UnityEngine;
using ComponentUtility = UnityEditorInternal.ComponentUtility;

public static class SpriteTransformHierarchyMigration
{
    private const string ScaleX = "_ScaleX";
    private const string ScaleY = "_ScaleY";
    private const string SkewX = "_SkewX";
    private const string SkewY = "_SkewY";
    private static readonly string[] LegacyFloatProperties =
        { ScaleX, ScaleY, SkewX, SkewY };
    private static readonly string[] LegacyVectorProperties =
        { "_AffineRow0", "_AffineRow1" };

    [MenuItem("Tools/PvZ/Migrate All SpriteTransforms To Native Hierarchy")]
    public static void MigrateAllPrefabs()
    {
        var changedPrefabCount = 0;
        var changedTransformCount = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var spriteTransforms = root.GetComponentsInChildren<SpriteTransform>(true);
                if (spriteTransforms.Length == 0) continue;

                var changed = false;
                foreach (var spriteTransform in spriteTransforms)
                {
                    if (!EnsureNativeHierarchy(spriteTransform)) continue;

                    changed = true;
                    changedTransformCount++;
                }

                if (!changed) continue;

                foreach (var spriteTransform in spriteTransforms)
                {
                    spriteTransform.RefreshPositionReference();
                    spriteTransform.Apply();
                    EditorUtility.SetDirty(spriteTransform);
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                changedPrefabCount++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        var cleanedMaterialCount = CleanupLegacyMaterialProperties();
        AssetDatabase.SaveAssets();
        Debug.Log(
            $"Migrated {changedTransformCount} SpriteTransforms in " +
            $"{changedPrefabCount} prefabs to the native hierarchy backend; " +
            $"cleaned {cleanedMaterialCount} materials.");
    }

    public static void MigrateAllPrefabsFromCommandLine()
    {
        MigrateAllPrefabs();
    }

    public static bool EnsureNativeHierarchy(SpriteTransform spriteTransform)
    {
        if (spriteTransform == null || spriteTransform.NativeContent != null) return false;

        ImportLegacyMaterialGeometry(spriteTransform);
        CreateNativeContent(spriteTransform);
        return true;
    }

    private static void ImportLegacyMaterialGeometry(SpriteTransform spriteTransform)
    {
        var renderer = spriteTransform.GetComponent<SpriteRenderer>();
        var material = renderer != null ? renderer.sharedMaterial : null;
        if (material == null)
        {
            if (spriteTransform.scale == Vector2.zero)
            {
                spriteTransform.scale = new Vector2(100f, 100f);
            }
            return;
        }

        if (TryGetSavedFloat(material, ScaleX, out var scaleX) &&
            TryGetSavedFloat(material, ScaleY, out var scaleY))
        {
            spriteTransform.scale = new Vector2(scaleX, scaleY);
        }
        else if (spriteTransform.scale == Vector2.zero)
        {
            spriteTransform.scale = new Vector2(100f, 100f);
        }

        if (TryGetSavedFloat(material, SkewX, out var skewX) &&
            TryGetSavedFloat(material, SkewY, out var skewY))
        {
            spriteTransform.skew = new Vector2(skewX, skewY);
        }
    }

    private static bool TryGetSavedFloat(Material material, string propertyName, out float value)
    {
        if (material.HasProperty(propertyName))
        {
            value = material.GetFloat(propertyName);
            return true;
        }

        var serializedMaterial = new SerializedObject(material);
        var floats = serializedMaterial.FindProperty("m_SavedProperties.m_Floats");
        if (floats != null && floats.isArray)
        {
            for (var index = 0; index < floats.arraySize; index++)
            {
                var pair = floats.GetArrayElementAtIndex(index);
                var key = pair.FindPropertyRelative("first");
                var savedValue = pair.FindPropertyRelative("second");
                if (key != null && savedValue != null && key.stringValue == propertyName)
                {
                    value = savedValue.floatValue;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static int CleanupLegacyMaterialProperties()
    {
        var changedMaterialCount = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null || material.shader == null ||
                (material.shader.name != "Custom/LightnessSkewShader" &&
                 material.shader.name != "Custom/Particle"))
            {
                continue;
            }

            var serializedMaterial = new SerializedObject(material);
            var changed = RemoveSavedProperties(
                serializedMaterial.FindProperty("m_SavedProperties.m_Floats"),
                LegacyFloatProperties);
            changed |= RemoveSavedProperties(
                serializedMaterial.FindProperty("m_SavedProperties.m_Colors"),
                LegacyVectorProperties);
            if (!changed) continue;

            serializedMaterial.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(material);
            changedMaterialCount++;
        }

        return changedMaterialCount;
    }

    private static bool RemoveSavedProperties(SerializedProperty properties, string[] names)
    {
        if (properties == null || !properties.isArray) return false;

        var changed = false;
        for (var index = properties.arraySize - 1; index >= 0; index--)
        {
            var key = properties.GetArrayElementAtIndex(index).FindPropertyRelative("first");
            if (key == null || System.Array.IndexOf(names, key.stringValue) < 0) continue;

            properties.DeleteArrayElementAtIndex(index);
            changed = true;
        }

        return changed;
    }

    private static void CreateNativeContent(SpriteTransform spriteTransform)
    {
        var pivot = spriteTransform.transform;
        var content = pivot.Find(SpriteTransform.NativeContentName);
        if (content == null)
        {
            content = new GameObject(SpriteTransform.NativeContentName)
            {
                layer = pivot.gameObject.layer
            }.transform;
            content.SetParent(pivot, false);
        }

        content.localPosition = Vector3.zero;
        content.localRotation = Quaternion.identity;
        content.localScale = Vector3.one;

        var pivotRenderer = pivot.GetComponent<SpriteRenderer>();
        if (pivotRenderer != null)
        {
            var contentRenderer = content.GetComponent<SpriteRenderer>();
            ComponentUtility.CopyComponent(pivotRenderer);
            if (contentRenderer == null)
            {
                ComponentUtility.PasteComponentAsNew(content.gameObject);
            }
            else
            {
                ComponentUtility.PasteComponentValues(contentRenderer);
            }
            Object.DestroyImmediate(pivotRenderer);
        }

        spriteTransform.ConfigureNativeHierarchy(content);
    }
}
