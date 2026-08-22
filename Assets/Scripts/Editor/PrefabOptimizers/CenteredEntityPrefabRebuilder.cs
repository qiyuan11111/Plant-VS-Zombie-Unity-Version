using UnityEditor;
using UnityEngine;

public static class CenteredEntityPrefabRebuilder
{
    [MenuItem("Tools/PvZ/Rebuild Centered Entity Prefabs")]
    public static void Rebuild()
    {
        PeaShooterSinglePrefabOptimizer.OptimizePeaShooterSingle();
        SunFlowerPrefabOptimizer.OptimizeSunFlower();
        SunShroomPrefabOptimizer.OptimizeSunShroom();
        ZombieNormalPrefabBuilder.Build();
        AssetDatabase.SaveAssets();
        Debug.Log("Centered plant and zombie prefabs rebuilt successfully.");
    }

    public static void RebuildFromCommandLine()
    {
        Rebuild();
    }
}
