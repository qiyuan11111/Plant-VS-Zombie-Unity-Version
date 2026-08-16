using PvZ.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NativeSkewExamplesBuilder
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string RootName = "NativeSkewExamples_ONE_LAYER__SELECT_EACH_EXAMPLE";
    private const string LegacyRootName = "NativeSkewExamples__SELECT_EACH_EXAMPLE";
    private const string HeadSpritePath =
        "Assets/Prefab/Plant/PeaShooterSingle/Sprite/PeaShooterSingle_head.png";
    private const string MouthSpritePath =
        "Assets/Prefab/Plant/PeaShooterSingle/Sprite/PeaShooterSingle_mouth.png";
    private const string SproutSpritePath =
        "Assets/Prefab/Plant/PeaShooterSingle/Sprite/PeaShooterSingle_sprout.png";
    private const string PivotSpritePath =
        "Assets/Prefab/Plant/PeaShooterSingle/Sprite/PeaShooter_blink1.png";

    [InitializeOnLoadMethod]
    private static void RefreshOutdatedOpenExamples()
    {
        var remainingChecks = 120;
        void TryRefresh()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var legacyRoot = GameObject.Find(LegacyRootName);
            if (legacyRoot == null)
            {
                if (--remainingChecks <= 0) EditorApplication.update -= TryRefresh;
                return;
            }

            EditorApplication.update -= TryRefresh;
            var scene = legacyRoot.scene;
            BuildExamples(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        EditorApplication.update += TryRefresh;
    }

    [MenuItem("Tools/PvZ/Build Native Skew Hierarchy Examples")]
    public static void BuildInCurrentScene()
    {
        BuildExamples(SceneManager.GetActiveScene());
    }

    public static void BuildFromCommandLine()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        BuildExamples(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void BuildExamples(Scene scene)
    {
        var oldRoot = GameObject.Find(RootName);
        if (oldRoot != null)
        {
            Object.DestroyImmediate(oldRoot);
        }

        var legacyRoot = GameObject.Find(LegacyRootName);
        if (legacyRoot != null)
        {
            Object.DestroyImmediate(legacyRoot);
        }

        var head = LoadSprite(HeadSpritePath);
        var mouth = LoadSprite(MouthSpritePath);
        var sprout = LoadSprite(SproutSpritePath);
        var pivotSprite = LoadSprite(PivotSpritePath);

        var root = new GameObject(RootName);
        SceneManager.MoveGameObjectToScene(root, scene);
        root.transform.position = new Vector3(0f, -150f, 0f);

        BuildExample(
            root.transform,
            "01_ONE_EXTRA_LAYER__ParentScale__ChildLocalPositionStays_70_0",
            new Vector3(-390f, 0f, 0f),
            new Vector2(70f, 120f),
            Vector2.zero,
            head,
            mouth,
            sprout,
            pivotSprite);
        BuildExample(
            root.transform,
            "02_ONE_EXTRA_LAYER__HorizontalSkew_25deg__PivotPositionStays",
            new Vector3(-130f, 0f, 0f),
            new Vector2(100f, 100f),
            new Vector2(25f, 0f),
            head,
            mouth,
            sprout,
            pivotSprite);
        BuildExample(
            root.transform,
            "03_ONE_EXTRA_LAYER__ScalePlusTwoAxisSkew__AllChildrenFollow",
            new Vector3(130f, 0f, 0f),
            new Vector2(125f, 70f),
            new Vector2(20f, -12f),
            head,
            mouth,
            sprout,
            pivotSprite);

        var nested = BuildExample(
            root.transform,
            "04_ONE_EXTRA_LAYER__NestedSkew__ParentAffectsChildPivot",
            new Vector3(390f, 0f, 0f),
            new Vector2(110f, 90f),
            new Vector2(-18f, 8f),
            head,
            null,
            sprout,
            pivotSprite);
        var childPivot = new GameObject("ChildPivot_LocalPosition_ALWAYS_70_0");
        childPivot.transform.SetParent(nested.Content, false);
        childPivot.transform.localPosition = new Vector3(70f, 0f, 0f);
        var childSkew = childPivot.AddComponent<NativeSkewTransform>();
        childSkew.EnsureHierarchy();
        childSkew.Configure(new Vector2(75f, 120f), new Vector2(0f, 28f));
        CreateSprite(
            childSkew.Content,
            "NestedChildSprite_PivotAtOwnCenter",
            mouth,
            Vector3.zero,
            new Color(1f, 0.75f, 0.35f, 1f),
            32);
        CreateSprite(
            childSkew.Content,
            "GrandChild_LocalPosition_35_25",
            pivotSprite,
            new Vector3(35f, 25f, 0f),
            Color.cyan,
            34);

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("Created NativeSkewExamples. Expand the root and select examples 01-04.");
    }

    private static NativeSkewTransform BuildExample(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector2 scalePercent,
        Vector2 skewDegrees,
        Sprite head,
        Sprite mouth,
        Sprite sprout,
        Sprite pivotSprite)
    {
        var pivot = new GameObject(name);
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = localPosition;
        var nativeSkew = pivot.AddComponent<NativeSkewTransform>();
        var content = nativeSkew.EnsureHierarchy();
        nativeSkew.Configure(scalePercent, skewDegrees);
        if (skewDegrees == Vector2.zero)
        {
            pivot.transform.localRotation = Quaternion.identity;
            pivot.transform.localScale = new Vector3(
                scalePercent.x / 100f,
                scalePercent.y / 100f,
                1f);
            content.localRotation = Quaternion.identity;
            content.localScale = Vector3.one;
        }

        CreateSprite(
            content,
            "ParentSprite_LocalPosition_0_0",
            head,
            Vector3.zero,
            Color.white,
            20);
        if (mouth != null)
        {
            CreateSprite(
                content,
                "ChildA_LocalPosition_70_0__SCRIPT_NEVER_WRITES_IT",
                mouth,
                new Vector3(70f, 0f, 0f),
                new Color(1f, 0.7f, 0.25f, 1f),
                24);
        }

        if (sprout != null)
        {
            CreateSprite(
                content,
                "ChildB_LocalPosition_Minus45_45__SCRIPT_NEVER_WRITES_IT",
                sprout,
                new Vector3(-45f, 45f, 0f),
                new Color(0.45f, 1f, 0.55f, 1f),
                22);
        }

        // Its local center is zero, so it marks the stable root pivot while
        // remaining inside the single Content layer.
        CreateSprite(
            content,
            "MAGENTA_MARKER__CenterIsStableRootPivot",
            pivotSprite,
            Vector3.zero,
            Color.magenta,
            40,
            new Vector3(0.32f, 0.32f, 1f));
        return nativeSkew;
    }

    private static void CreateSprite(
        Transform parent,
        string name,
        Sprite sprite,
        Vector3 localPosition,
        Color color,
        int sortingOrder,
        Vector3? localScale = null)
    {
        if (sprite == null) return;
        var target = new GameObject(name);
        target.transform.SetParent(parent, false);
        target.transform.localPosition = localPosition;
        target.transform.localRotation = Quaternion.identity;
        target.transform.localScale = localScale ?? Vector3.one;
        var renderer = target.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }

    private static Sprite LoadSprite(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            throw new MissingReferenceException($"Missing example sprite: {path}");
        }

        return sprite;
    }
}
