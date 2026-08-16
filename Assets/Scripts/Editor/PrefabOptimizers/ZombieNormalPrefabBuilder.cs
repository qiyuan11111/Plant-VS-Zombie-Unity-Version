using System;
using System.Collections.Generic;
using System.IO;
using PvZ.Config;
using PvZ.Gameplay.Zombies;
using PvZ.Presentation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class ZombieNormalPrefabBuilder
{
    private const string BasePath = "Assets/Prefab/Zombie/ZombieNormal";
    private const string SpritePath = BasePath + "/Sprite";
    private const string AnimationPath = BasePath + "/Animation";
    private const string SourceIdlePath = "Assets/StreamingAssets/generator/idle.anim";
    private const string IdlePath = AnimationPath + "/idle.anim";
    private const string ControllerPath = AnimationPath + "/ZombieNormal.controller";
    private const string PrefabPath = BasePath + "/ZombieNormal.prefab";
    private const string SharedMaterialPath = "Assets/Material/LightnessSkew.mat";
    private const string GameConfigPath = "Assets/Resources/GameConfigObject.asset";
    private const string SortingLayer = "zombie-0";

    private readonly struct Part
    {
        public Part(string name, int sortingOrder)
        {
            Name = name;
            SortingOrder = sortingOrder;
        }

        public string Name { get; }
        public int SortingOrder { get; }
    }

    // Reanimation track order is also its back-to-front draw order.
    private static readonly Part[] Parts =
    {
        new("Zombie_innerarm_hand", 12),
        new("Zombie_innerarm_lower", 13),
        new("Zombie_innerarm_upper", 14),
        new("Zombie_head", 18),
        new("Zombie_innerleg_upper", 19),
        new("Zombie_innerleg_lower", 20),
        new("Zombie_innerleg_foot", 21),
        new("Zombie_outerleg_upper", 22),
        new("Zombie_outerleg_foot", 23),
        new("Zombie_outerleg_lower", 24),
        new("Zombie_body", 25),
        new("Zombie_tie", 28),
        new("Zombie_jaw", 29),
        new("Zombie_outerarm_hand", 35),
        new("Zombie_outerarm_hand2", 35),
        new("Zombie_outerarm_upper", 36),
        new("Zombie_outerarm_lower", 38),
        new("Zombie_hair", 39)
    };

    [MenuItem("Tools/PvZ/Build Normal Zombie Prefab")]
    public static void Build()
    {
        EnsureFolders();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureTextureImporters();

        var idle = CreateIdleClip();
        var controller = CreateController(idle);
        var prefab = CreatePrefab(idle, controller);
        ConnectGameConfig(prefab);
        Validate(prefab, idle, controller);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log($"Normal zombie prefab built successfully: {PrefabPath}", prefab);
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Prefab", "Zombie");
        EnsureFolder("Assets/Prefab/Zombie", "ZombieNormal");
        EnsureFolder(BasePath, "Sprite");
        EnsureFolder(BasePath, "Animation");
    }

    private static void EnsureFolder(string parent, string child)
    {
        var path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
    }

    private static void ConfigureTextureImporters()
    {
        foreach (var part in Parts)
        {
            var path = $"{SpritePath}/{part.Name}.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new MissingReferenceException($"Missing zombie sprite: {path}");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 1f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    private static AnimationClip CreateIdleClip()
    {
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(IdlePath) == null)
        {
            var sourceAbsolutePath = Path.Combine(
                Application.dataPath,
                SourceIdlePath.Substring("Assets/".Length));
            var destinationAbsolutePath = Path.Combine(
                Application.dataPath,
                IdlePath.Substring("Assets/".Length));
            if (!File.Exists(sourceAbsolutePath))
            {
                throw new FileNotFoundException("Missing generated idle animation source.", sourceAbsolutePath);
            }

            File.Copy(sourceAbsolutePath, destinationAbsolutePath, false);
        }

        AssetDatabase.ImportAsset(IdlePath, ImportAssetOptions.ForceSynchronousImport);
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdlePath);
        if (clip == null) throw new MissingReferenceException($"Failed to import idle clip: {IdlePath}");

        clip.frameRate = 12f;
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController CreateController(AnimationClip idle)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        var stateMachine = controller.layers[0].stateMachine;
        AnimatorState idleState = null;
        foreach (var childState in stateMachine.states)
        {
            if (childState.state.name == "idle")
            {
                idleState = childState.state;
                break;
            }
        }

        idleState ??= stateMachine.AddState("idle");
        idleState.motion = idle;
        idleState.writeDefaultValues = true;
        stateMachine.defaultState = idleState;
        EditorUtility.SetDirty(idleState);
        EditorUtility.SetDirty(stateMachine);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static GameObject CreatePrefab(AnimationClip idle, RuntimeAnimatorController controller)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(SharedMaterialPath);
        if (material == null) throw new MissingReferenceException($"Missing sprite material: {SharedMaterialPath}");

        var zombieLayer = LayerMask.NameToLayer("Zombie");
        if (zombieLayer < 0) throw new InvalidOperationException("The project has no Zombie layer.");

        var root = new GameObject("ZombieNormal") { layer = zombieLayer };
        try
        {
            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            root.AddComponent<ZombieNormal>();
            root.AddComponent<SpriteGroup>();

            var collider = root.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.offset = new Vector2(35f, -65f);
            collider.size = new Vector2(45f, 105f);

            foreach (var part in Parts)
            {
                CreatePart(root.transform, part, zombieLayer, material);
            }

            // Apply the authored first frame so the prefab thumbnail and scene view
            // match the state that the Animator produces at runtime.
            idle.SampleAnimation(root, 0f);
            foreach (var spriteTransform in root.GetComponentsInChildren<SpriteTransform>(true))
            {
                spriteTransform.Apply();
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            if (prefab == null) throw new InvalidOperationException($"Failed to save prefab: {PrefabPath}");
            return prefab;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void CreatePart(Transform root, Part part, int layer, Material material)
    {
        var spritePath = $"{SpritePath}/{part.Name}.png";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null) throw new MissingReferenceException($"Sprite was not imported: {spritePath}");

        var pivot = new GameObject(part.Name) { layer = layer };
        pivot.transform.SetParent(root, false);

        var spriteTransform = pivot.AddComponent<SpriteTransform>();
        spriteTransform.position = Vector2.zero;
        spriteTransform.scale = new Vector2(100f, 100f);
        spriteTransform.skew = Vector2.zero;
        spriteTransform.brightness = 1f;
        spriteTransform.alpha = 1f;
        spriteTransform.alphaCoef = 1f;
        spriteTransform.updatePosition = true;

        var content = new GameObject(SpriteTransform.NativeContentName) { layer = layer };
        content.transform.SetParent(pivot.transform, false);
        var renderer = content.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = material;
        renderer.sortingLayerName = SortingLayer;
        renderer.sortingOrder = part.SortingOrder;
        renderer.drawMode = SpriteDrawMode.Simple;

        spriteTransform.ConfigureNativeHierarchy(content.transform);
    }

    private static void ConnectGameConfig(GameObject prefab)
    {
        var config = AssetDatabase.LoadAssetAtPath<GameConfigObject>(GameConfigPath);
        if (config == null) throw new MissingReferenceException($"Missing game config: {GameConfigPath}");

        config.ZombieNormal = prefab;
        EditorUtility.SetDirty(config);
    }

    private static void Validate(
        GameObject prefab,
        AnimationClip idle,
        RuntimeAnimatorController controller)
    {
        if (prefab.GetComponent<ZombieNormal>() == null) throw new MissingComponentException("ZombieNormal component is missing.");
        if (prefab.GetComponent<SpriteGroup>() == null) throw new MissingComponentException("SpriteGroup component is missing.");
        if (prefab.GetComponent<Animator>()?.runtimeAnimatorController != controller)
        {
            throw new MissingReferenceException("ZombieNormal Animator Controller is not assigned.");
        }

        var expectedPaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var binding in AnimationUtility.GetCurveBindings(idle))
        {
            if (string.IsNullOrEmpty(binding.path)) continue;
            expectedPaths.Add(binding.path);
        }

        foreach (var path in expectedPaths)
        {
            var target = prefab.transform.Find(path);
            if (target == null) throw new MissingReferenceException($"Idle animation target is missing: {path}");
            if (target.GetComponent<SpriteTransform>() == null)
            {
                throw new MissingComponentException($"Idle animation target has no SpriteTransform: {path}");
            }
        }

        if (prefab.GetComponentsInChildren<SpriteRenderer>(true).Length != Parts.Length)
        {
            throw new InvalidOperationException("ZombieNormal does not contain the expected number of sprite renderers.");
        }
    }
}
