using UnityEditor;
using UnityEngine;
using PvZ.Editor.PrefabPipeline.Plants.PeaShooterSingle;
using PvZ.Editor.PrefabPipeline.Plants.SunFlower;
using PvZ.Editor.PrefabPipeline.Plants.SunShroom;
using PvZ.Editor.PrefabPipeline.Zombies.ZombieNormal;

namespace PvZ.Editor.PrefabPipeline
{
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
}
