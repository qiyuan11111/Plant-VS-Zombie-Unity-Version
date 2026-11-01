using System;
using PvZ.Config;
using PvZ.Presentation.Rendering;
using UnityEditor;
using UnityEngine;
using static PvZ.Editor.PrefabPipeline.Common.PrefabHierarchyUtility;

namespace PvZ.Editor.PrefabPipeline.Bullets
{
    public static class ProjectilePeaPrefabBuilder
    {
        public const string PrefabPath = "Assets/Prefab/Bullet/ProjectilePea/ProjectilePea.prefab";
        public const string SpritePath = "Assets/Prefab/Bullet/ProjectilePea/Sprite/ProjectilePea.png";
        private const string MaterialPath = "Assets/Material/LightnessSkew.mat";
        private const string ConfigPath = "Assets/Resources/GameConfigObject.asset";
        private const string BasicPath = "component/basic";
        private const string PeaPath = BasicPath + "/__AffineContent/pea";

        // Original images/ProjectilePea.png is 28x28. Projectile::Draw uses scale 1
        // (Projectile.cpp:965, 971-974, 1042-1048). BoardGeometry uses source pixels
        // as units (80 per column), so the complete sprite must occupy 28x28 units.
        private const float PixelsPerUnit = 1f;
        private static readonly Vector2 ImageSize = new(28f, 28f);

        [MenuItem("Tools/PvZ/Build Pea Projectile Prefab")]
        public static void Build()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureTexture();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            var catalog = AssetDatabase.LoadAssetAtPath<GameConfigObject>(ConfigPath);
            var layer = LayerMask.NameToLayer("Bullet");
            if (sprite == null || material == null || catalog == null || layer < 0)
                throw new MissingReferenceException("Pea projectile requires its sprite, shared material, catalog and Bullet layer.");

            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
            var root = existing ? PrefabUtility.LoadPrefabContents(PrefabPath) : new GameObject("ProjectilePea");
            GameObject prefab;
            try
            {
                root.transform.localPosition = Vector3.zero;
                root.transform.localRotation = Quaternion.identity;
                root.transform.localScale = Vector3.one;
                if (root.GetComponent<SpriteGroup>() == null) root.AddComponent<SpriteGroup>();

                var basic = GetOrCreatePath(root.transform, BasicPath);
                var basicContent = GetOrCreateChild(basic, SpriteTransform.NativeContentName);
                GetOrCreatePath(root.transform, "component/anchors");
                var pea = GetOrCreatePath(root.transform, PeaPath);
                var visual = GetOrCreateChild(pea, SpriteTransform.NativeContentName);
                var renderer = visual.GetComponent<SpriteRenderer>();
                if (renderer == null) renderer = visual.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sharedMaterial = material;
                renderer.color = Color.white;
                renderer.sortingLayerName = "bullet";
                renderer.sortingOrder = 0;

                ConfigureTransform(basic, basicContent);
                ConfigureTransform(pea, visual);
                SetLayerRecursively(root, layer);
                prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (prefab == null) throw new InvalidOperationException("Failed to save pea projectile prefab.");
            }
            finally
            {
                if (existing) PrefabUtility.UnloadPrefabContents(root);
                else UnityEngine.Object.DestroyImmediate(root);
            }

            catalog.ProjectilePea = prefab;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Verify();
            Debug.Log($"Pea projectile built: {PrefabPath}; PPU=1, centered 28x28 sprite, scale=(1,1,1).", prefab);
        }

        private static void ConfigureTexture()
        {
            var importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            if (importer == null) throw new MissingReferenceException($"Missing original pea image: {SpritePath}");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(0.5f, 0.5f);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static void ConfigureTransform(Transform pivot, Transform content)
        {
            var spriteTransform = pivot.GetComponent<SpriteTransform>();
            if (spriteTransform == null) spriteTransform = pivot.gameObject.AddComponent<SpriteTransform>();
            spriteTransform.position = Vector2.zero;
            spriteTransform.scale = new Vector2(100f, 100f);
            spriteTransform.skew = Vector2.zero;
            spriteTransform.updatePosition = false;
            spriteTransform.providesChildSpritePosition = false;
            spriteTransform.providesChildSpriteAffine = false;
            spriteTransform.ConfigureNativeHierarchy(content);
        }

        [MenuItem("Tools/PvZ/Verify Pea Projectile Prefab")]
        public static void Verify()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            var importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            var catalog = AssetDatabase.LoadAssetAtPath<GameConfigObject>(ConfigPath);
            if (prefab == null || sprite == null || importer == null || catalog == null)
                throw new MissingReferenceException("Pea projectile assets are incomplete.");

            if (sprite.rect.size != ImageSize || sprite.texture.width != 28 || sprite.texture.height != 28 ||
                !Mathf.Approximately(sprite.pixelsPerUnit, PixelsPerUnit) ||
                !Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit) ||
                ((Vector2)sprite.bounds.size - ImageSize).sqrMagnitude > 0.0001f ||
                sprite.pivot != ImageSize * 0.5f)
                throw new InvalidOperationException("Pea must retain its original 28x28 pixels, PPU=1 and centered pivot.");

            var renderer = prefab.GetComponentInChildren<SpriteRenderer>(true);
            if (prefab.transform.childCount != 1 || prefab.transform.Find("component/anchors") == null ||
                renderer == null || prefab.GetComponentsInChildren<SpriteRenderer>(true).Length != 1 ||
                renderer.transform != prefab.transform.Find(PeaPath + "/__AffineContent") ||
                renderer.sprite != sprite || renderer.sharedMaterial != AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) ||
                renderer.sortingLayerName != "bullet" || catalog.ProjectilePea != prefab || catalog.ProjectilePea == catalog.Sun)
                throw new InvalidOperationException("Pea prefab hierarchy, renderer or catalog reference is invalid.");

            foreach (var child in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (child.localScale != Vector3.one || child.localPosition != Vector3.zero ||
                    Quaternion.Angle(child.localRotation, Quaternion.identity) > 0.001f ||
                    child.gameObject.layer != LayerMask.NameToLayer("Bullet"))
                    throw new InvalidOperationException($"Pea transform must be centered, unscaled and on the Bullet layer: {child.name}");
            }

            if (importer.spriteImportMode != SpriteImportMode.Single || importer.mipmapEnabled ||
                !importer.alphaIsTransparency || importer.filterMode != FilterMode.Point ||
                importer.wrapMode != TextureWrapMode.Clamp || importer.textureCompression != TextureImporterCompression.Uncompressed)
                throw new InvalidOperationException("Pea texture must use a single, uncompressed sprite with point filtering and clamp wrapping.");

            Debug.Log("Pea projectile verified: original 28x28 image; centered bounds 28x28 units; PPU=1; correct material, Bullet layer and catalog reference.");
        }
    }
}