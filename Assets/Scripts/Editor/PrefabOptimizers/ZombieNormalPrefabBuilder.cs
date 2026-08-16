using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
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
    private const string IdleXmlPath = "Assets/StreamingAssets/Zombie_anim_idle1.xml";
    private const string WalkXmlPath = "Assets/StreamingAssets/Zombie_anim_walk1.xml";
    private const string IdlePath = AnimationPath + "/idle.anim";
    private const string WalkPath = AnimationPath + "/walk.anim";
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
        new("Zombie_outerarm_upper", 36),
        new("Zombie_outerarm_lower", 38),
    };

    [MenuItem("Tools/PvZ/Build Normal Zombie Prefab")]
    public static void Build()
    {
        EnsureFolders();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureTextureImporters();

        var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdlePath);
        if (idle == null) throw new MissingReferenceException($"Missing idle clip: {IdlePath}");
        var walk = CreateReanimationClip(WalkXmlPath, WalkPath, "walk", true);
        var controller = CreateController(idle, walk);
        var prefab = CreatePrefab(idle, controller);
        ConnectGameConfig(prefab);
        Validate(prefab, idle, walk, controller);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log($"Normal zombie prefab built successfully: {PrefabPath}", prefab);
    }

    [MenuItem("Tools/PvZ/Verify Normal Zombie Walk Root Motion")]
    public static void VerifyWalkRootMotionPlayback()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) throw new MissingReferenceException($"Missing prefab: {PrefabPath}");

        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null) throw new InvalidOperationException("Failed to instantiate ZombieNormal for verification.");

        try
        {
            var animator = instance.GetComponent<Animator>();
            if (animator == null) throw new MissingComponentException("ZombieNormal Animator is missing.");

            animator.Rebind();
            animator.Play("walk", 0, 0f);
            animator.Update(0f);
            var startX = instance.transform.position.x;
            for (var frame = 0; frame < 48; frame++)
            {
                animator.Update(1f / 12f);
            }

            var endX = instance.transform.position.x;
            if (endX >= startX - 45f)
            {
                throw new InvalidOperationException(
                    $"Walk root motion did not accumulate across the loop: {startX} -> {endX}.");
            }

            Debug.Log($"ZombieNormal walk root motion verified: {startX} -> {endX} after 48 frames.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
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
        var pixelsPerUnit = ReadWalkPixelsPerUnit();
        foreach (var part in Parts)
        {
            var path = $"{SpritePath}/{part.Name}.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new MissingReferenceException($"Missing zombie sprite: {path}");
            if (!pixelsPerUnit.TryGetValue(part.Name, out var partPixelsPerUnit))
            {
                throw new InvalidDataException($"Walk XML has no size for zombie part: {part.Name}");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = partPixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    private static Dictionary<string, float> ReadWalkPixelsPerUnit()
    {
        var xmlAbsolutePath = Path.Combine(Application.dataPath, WalkXmlPath.Substring("Assets/".Length));
        if (!File.Exists(xmlAbsolutePath))
        {
            throw new FileNotFoundException("Missing normal zombie walk XML.", xmlAbsolutePath);
        }

        var document = new XmlDocument();
        document.Load(xmlAbsolutePath);
        var layers = document.SelectNodes("/animate/layer");
        if (layers == null) throw new InvalidDataException($"Walk XML has no layers: {WalkXmlPath}");

        var result = new Dictionary<string, float>(StringComparer.Ordinal);
        foreach (XmlNode layer in layers)
        {
            var layerName = layer.Attributes?["name"]?.Value;
            if (string.IsNullOrEmpty(layerName) || layerName == "_ground") continue;

            var targetName = ResolveWalkLayerPath(layerName);
            var texturePath = $"{SpritePath}/{targetName}.png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null) throw new MissingReferenceException($"Missing zombie texture: {texturePath}");

            var authoredWidth = ParseXmlFloat(layer, "width");
            var authoredHeight = ParseXmlFloat(layer, "height");
            if (authoredWidth <= 0f || authoredHeight <= 0f)
            {
                throw new InvalidDataException($"Walk XML layer has an invalid size: {layerName}");
            }

            var widthRatio = texture.width / authoredWidth;
            var heightRatio = texture.height / authoredHeight;
            var ppu = ChoosePixelsPerUnit(targetName, widthRatio, heightRatio);
            if (result.TryGetValue(targetName, out var existing) && !Mathf.Approximately(existing, ppu))
            {
                throw new InvalidDataException($"Walk XML gives conflicting sizes for {targetName}.");
            }
            result[targetName] = ppu;
        }
        return result;
    }

    private static float ChoosePixelsPerUnit(string partName, float widthRatio, float heightRatio)
    {
        if (Mathf.Abs(widthRatio - heightRatio) <= 0.0001f)
        {
            return (widthRatio + heightRatio) * 0.5f;
        }

        var widthSimple = NearestSimpleFraction(widthRatio);
        var heightSimple = NearestSimpleFraction(heightRatio);
        var widthError = Mathf.Abs(widthRatio - widthSimple);
        var heightError = Mathf.Abs(heightRatio - heightSimple);
        var chosen = widthError <= heightError ? widthSimple : heightSimple;
        Debug.LogWarning(
            $"{partName} has mismatched PPU ratios ({widthRatio} vs {heightRatio}); " +
            $"using the more plausible simple value {chosen}.");
        return chosen;
    }

    private static float NearestSimpleFraction(float value)
    {
        var best = value;
        var bestError = float.PositiveInfinity;
        for (var denominator = 1; denominator <= 12; denominator++)
        {
            var numerator = Mathf.RoundToInt(value * denominator);
            var candidate = (float)numerator / denominator;
            var error = Mathf.Abs(value - candidate);
            if (error < bestError - 0.000001f)
            {
                best = candidate;
                bestError = error;
            }
        }
        return best;
    }
    private static AnimationClip CreateAnimationClip(string sourcePath, string destinationPath)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(destinationPath) == null)
        {
            var sourceAbsolutePath = Path.Combine(
                Application.dataPath,
                sourcePath.Substring("Assets/".Length));
            var destinationAbsolutePath = Path.Combine(
                Application.dataPath,
                destinationPath.Substring("Assets/".Length));
            if (!File.Exists(sourceAbsolutePath))
            {
                throw new FileNotFoundException("Missing generated idle animation source.", sourceAbsolutePath);
            }

            File.Copy(sourceAbsolutePath, destinationAbsolutePath, false);
        }

        AssetDatabase.ImportAsset(destinationPath, ImportAssetOptions.ForceSynchronousImport);
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(destinationPath);
        if (clip == null) throw new MissingReferenceException($"Failed to import animation clip: {destinationPath}");

        clip.frameRate = 12f;
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimationClip CreateReanimationClip(
        string xmlPath,
        string destinationPath,
        string clipName,
        bool includeRootMotion)
    {
        var xmlAbsolutePath = Path.Combine(Application.dataPath, xmlPath.Substring("Assets/".Length));
        if (!File.Exists(xmlAbsolutePath))
        {
            throw new FileNotFoundException("Missing normal zombie walk XML.", xmlAbsolutePath);
        }

        var document = new XmlDocument();
        document.Load(xmlAbsolutePath);
        var layers = document.SelectNodes("/animate/layer");
        if (layers == null || layers.Count == 0)
        {
            throw new InvalidDataException($"Walk XML has no animation layers: {xmlPath}");
        }

        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(destinationPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, destinationPath);
        }

        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
        {
            AnimationUtility.SetEditorCurve(clip, binding, null);
        }
        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
        }

        clip.name = clipName;
        clip.frameRate = 12f;
        foreach (XmlNode layer in layers)
        {
            var layerName = layer.Attributes?["name"]?.Value;
            var frames = layer.SelectNodes("frame");
            if (string.IsNullOrEmpty(layerName) || frames == null || frames.Count == 0)
            {
                throw new InvalidDataException("Walk XML contains an unnamed or empty layer.");
            }

            if (layerName == "_ground")
            {
                if (!includeRootMotion)
                {
                    throw new InvalidDataException($"Unexpected _ground layer in {xmlPath}.");
                }
                var initialGroundX = ParseXmlFloat(frames[0], "posx");
                SetLinearCurve(
                    clip,
                    string.Empty,
                    typeof(Transform),
                    "m_LocalPosition.x",
                    CreateXmlCurve(frames, "posx", value => -(value - initialGroundX)));
                continue;
            }

            var targetPath = ResolveWalkLayerPath(layerName);
            SetLinearCurve(clip, targetPath, typeof(SpriteTransform), "position.x", CreateXmlCurve(frames, "posx"));
            SetLinearCurve(clip, targetPath, typeof(SpriteTransform), "position.y", CreateXmlCurve(frames, "posy"));
            SetLinearCurve(clip, targetPath, typeof(SpriteTransform), "scale.x", CreateXmlCurve(frames, "scalex"));
            SetLinearCurve(clip, targetPath, typeof(SpriteTransform), "scale.y", CreateXmlCurve(frames, "scaley"));
            SetLinearCurve(clip, targetPath, typeof(SpriteTransform), "skew.x", CreateXmlCurve(frames, "skewx"));
            SetLinearCurve(clip, targetPath, typeof(SpriteTransform), "skew.y", CreateXmlCurve(frames, "skewy"));
        }

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssetIfDirty(clip);
        return clip;
    }

    private static AnimationCurve CreateXmlCurve(
        XmlNodeList frames,
        string attribute,
        Func<float, float> convert = null)
    {
        var keys = new Keyframe[frames.Count];
        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames[i];
            var time = ParseXmlFloat(frame, "index") / 12f;
            var value = ParseXmlFloat(frame, attribute);
            keys[i] = new Keyframe(time, convert == null ? value : convert(value));
        }
        return new AnimationCurve(keys);
    }

    private static float ParseXmlFloat(XmlNode node, string attribute)
    {
        var text = node.Attributes?[attribute]?.Value;
        if (string.IsNullOrEmpty(text) ||
            !float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            throw new InvalidDataException($"Walk XML frame is missing a valid '{attribute}' value.");
        }
        return value;
    }

    private static void SetLinearCurve(
        AnimationClip clip,
        string path,
        Type type,
        string propertyName,
        AnimationCurve curve)
    {
        for (var i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
        }
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, type, propertyName), curve);
    }

    private static string ResolveWalkLayerPath(string layerName)
    {
        return layerName switch
        {
            "anim_head1" => "Zombie_head",
            "anim_head2" => "Zombie_jaw",
            "anim_innerarm1" => "Zombie_innerarm_upper",
            "anim_innerarm2" => "Zombie_innerarm_lower",
            "anim_innerarm3" => "Zombie_innerarm_hand",
            _ => layerName
        };
    }
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

    private static GameObject CreatePrefab(AnimationClip idle, RuntimeAnimatorController controller)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            return UpdateExistingPrefab(idle, controller);
        }

        var material = AssetDatabase.LoadAssetAtPath<Material>(SharedMaterialPath);
        if (material == null) throw new MissingReferenceException($"Missing sprite material: {SharedMaterialPath}");

        var zombieLayer = LayerMask.NameToLayer("Zombie");
        if (zombieLayer < 0) throw new InvalidOperationException("The project has no Zombie layer.");

        var root = new GameObject("ZombieNormal") { layer = zombieLayer };
        try
        {
            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = true;
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

    private static GameObject UpdateExistingPrefab(
        AnimationClip idle,
        RuntimeAnimatorController controller)
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var animator = root.GetComponent<Animator>();
            if (animator == null) throw new MissingComponentException("ZombieNormal Animator is missing.");

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            SynchronizeParts(root.transform);
            idle.SampleAnimation(root, 0f);
            foreach (var spriteTransform in root.GetComponentsInChildren<SpriteTransform>(true))
            {
                spriteTransform.Apply();
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            if (prefab == null) throw new InvalidOperationException($"Failed to update prefab: {PrefabPath}");
            return prefab;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void SynchronizeParts(Transform root)
    {
        var expectedNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var part in Parts) expectedNames.Add(part.Name);

        for (var index = root.childCount - 1; index >= 0; index--)
        {
            var child = root.GetChild(index);
            if (child.GetComponent<SpriteTransform>() != null && !expectedNames.Contains(child.name))
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        var material = AssetDatabase.LoadAssetAtPath<Material>(SharedMaterialPath);
        if (material == null) throw new MissingReferenceException($"Missing sprite material: {SharedMaterialPath}");
        var zombieLayer = LayerMask.NameToLayer("Zombie");
        if (zombieLayer < 0) throw new InvalidOperationException("The project has no Zombie layer.");

        foreach (var part in Parts)
        {
            if (root.Find(part.Name) == null) CreatePart(root, part, zombieLayer, material);
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
        AnimationClip walk,
        RuntimeAnimatorController controller)
    {
        if (prefab.GetComponent<ZombieNormal>() == null) throw new MissingComponentException("ZombieNormal component is missing.");
        if (prefab.GetComponent<SpriteGroup>() == null) throw new MissingComponentException("SpriteGroup component is missing.");
        if (prefab.GetComponent<Animator>()?.runtimeAnimatorController != controller)
        {
            throw new MissingReferenceException("ZombieNormal Animator Controller is not assigned.");
        }
        if (!prefab.GetComponent<Animator>().applyRootMotion)
        {
            throw new InvalidOperationException("ZombieNormal must apply the walk clip's root motion.");
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

        var expectedPartNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var part in Parts) expectedPartNames.Add(part.Name);
var walkPaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var binding in AnimationUtility.GetCurveBindings(walk))
        {
            if (!string.IsNullOrEmpty(binding.path) && binding.type == typeof(SpriteTransform))
            {
                walkPaths.Add(binding.path);
            }
        }
        if (!expectedPartNames.SetEquals(walkPaths))
        {
            throw new InvalidOperationException("Walk XML bindings do not exactly match the prefab part set.");
        }

        if (prefab.GetComponentsInChildren<SpriteTransform>(true).Length != Parts.Length)
        {
            throw new InvalidOperationException("ZombieNormal does not contain exactly the XML part count.");
        }
        ValidateWalkRootMotion(walk);

        if (prefab.GetComponentsInChildren<SpriteRenderer>(true).Length != Parts.Length)
        {
            throw new InvalidOperationException("ZombieNormal does not contain the expected number of sprite renderers.");
        }
    }

    private static void ValidateWalkRootMotion(AnimationClip walk)
    {
        AnimationCurve rootMotion = null;
        foreach (var binding in AnimationUtility.GetCurveBindings(walk))
        {
            if (string.IsNullOrEmpty(binding.path) &&
                binding.type == typeof(Transform) &&
                binding.propertyName == "m_LocalPosition.x")
            {
                rootMotion = AnimationUtility.GetEditorCurve(walk, binding);
                break;
            }
        }

        if (rootMotion == null || rootMotion.length < 2)
        {
            throw new MissingReferenceException("Walk animation has no root m_LocalPosition.x curve.");
        }

        var start = rootMotion.keys[0].value;
        var end = rootMotion.keys[rootMotion.length - 1].value;
        if (Mathf.Abs(start) > 0.001f || end >= -0.001f)
        {
            throw new InvalidOperationException(
                $"Walk root motion must start at zero and move left, but is {start} -> {end}.");
        }
    }
}
