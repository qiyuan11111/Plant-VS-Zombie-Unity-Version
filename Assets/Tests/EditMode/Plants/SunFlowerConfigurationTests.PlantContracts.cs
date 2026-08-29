using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PvZ.Gameplay.Plants;
using PvZ.Gameplay.Plants.Types;
using PvZ.Gameplay.Plants.Abilities;
using PvZ.Gameplay.Presentation.Shadows;
using PvZ.Presentation.Rendering;
using PvZ.Gameplay.Presentation.EntityPreviews;
using PvZ.Config;
using PvZ.Gameplay.Board;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace PvZ.Tests.EditMode.Plants
{
    public sealed partial class SunFlowerConfigurationTests
    {
    [Test]
            public void Plants_UseCenteredRootShadowPositions()
            {
                var config = Resources.Load<GameConfigObject>("GameConfigObject");
                Assert.That(config, Is.Not.Null);
                config.Init();
    
                Assert.That(Shadow.GetScale(ShadowSizePreset.Large), Is.EqualTo(1f));
                Assert.That(Shadow.GetScale(ShadowSizePreset.Small), Is.EqualTo(0.5f));
                Assert.That(
                    Shadow.GetScale(ShadowSizePreset.Large),
                    Is.GreaterThan(Shadow.GetScale(ShadowSizePreset.Small)));
    
                var sunShroom = config.GetPlantDefinition(PlantType.SunShroom)
                    .Prefab.GetComponent<PlantEntity>();
                Assert.That(sunShroom.ShadowSize, Is.EqualTo(ShadowSizePreset.Small));
                Assert.That(sunShroom.ShadowLocalPosition,
                    Is.EqualTo(new Vector2(-2.275f, -14.125f)));
    
                foreach (var type in new[]
                         {
                             PlantType.PeaShooterSingle,
                             PlantType.SunFlower
                         })
                {
                    var plant = config.GetPlantDefinition(type).Prefab.GetComponent<PlantEntity>();
                    Assert.That(plant, Is.Not.Null, type.ToString());
                    Assert.That(plant.ShadowSize, Is.EqualTo(ShadowSizePreset.Large), type.ToString());
                    var expected = type == PlantType.PeaShooterSingle
                        ? new Vector2(0.55f, -21.25f)
                        : new Vector2(-0.4f, -26.4f);
                    Assert.That(plant.ShadowLocalPosition, Is.EqualTo(expected), type.ToString());
                }
            }

    [Test]
            public void AllPlantPrefabs_UseCenteredTopLevelRoots()
            {
                var config = Resources.Load<GameConfigObject>("GameConfigObject");
                Assert.That(config, Is.Not.Null);
                config.Init();
    
                var parent = new GameObject("Parent").transform;
                try
                {
                    foreach (PlantType type in System.Enum.GetValues(typeof(PlantType)))
                    {
                        var definition = config.GetPlantDefinition(type);
                        var instance = Object.Instantiate(definition.PresentationPrefab, parent, false);
                        var plant = instance.GetComponent<PlantEntity>();
    
                        Assert.That(plant, Is.Not.Null, type.ToString());
                        var basic = instance.transform.Find("component/basic")
                            ?.GetComponent<SpriteTransform>();
                        Assert.That(basic, Is.Not.Null, type.ToString());
                        Assert.That(instance.transform.localPosition, Is.EqualTo(Vector3.zero), type.ToString());
                        Assert.That(basic.transform.localPosition, Is.EqualTo(Vector3.zero), type.ToString());
                        Assert.That(basic.providesChildSpritePosition, Is.False, type.ToString());
                        Assert.That(basic.providesChildSpriteAffine, Is.False, type.ToString());
                        Assert.That(basic.spritePosition, Is.EqualTo(Vector2.zero), type.ToString());
                    }
                }
                finally
                {
                    Object.DestroyImmediate(parent.gameObject);
                }
            }

    [Test]
            public void BoardPlacement_UsesGridCenterWithoutPlantSpecificCorrection()
            {
                var boardRoot = new GameObject("BoardRoot").transform;
                try
                {
                    var instance = new GameObject("CenteredPlant", typeof(SpriteGroup), typeof(BoxCollider2D));
                    instance.transform.SetParent(boardRoot, false);
                    var plant = instance.AddComponent<CenteredPlacementTestPlant>();
                    plant.ResetRuntimeState();
                    var grid = new BoardCell(
                        new Vector2Int(3, 2),
                        new Vector2(100f, 200f));
                    var plantData = new SerializedObject(plant);
                    plantData.FindProperty("drawsShadow").boolValue = false;
                    plantData.ApplyModifiedPropertiesWithoutUndo();
    
                    plant.EnterBoard(grid);
    
                    Assert.That(plant.transform.localPosition,
                        Is.EqualTo(new Vector3(grid.Position.x, grid.Position.y, 10f)));
                }
                finally
                {
                    Object.DestroyImmediate(boardRoot.gameObject);
                }
            }

    [Test]
            public void PlantShadowSprite_IsCenteredOnItsPrefabOrigin()
            {
                var config = Resources.Load<GameConfigObject>("GameConfigObject");
                Assert.That(config, Is.Not.Null);
    
                var shadowSprite = config.PlantShadow.transform.Find("plantshadow");
                Assert.That(shadowSprite, Is.Not.Null);
                Assert.That(shadowSprite.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(shadowSprite.localScale, Is.EqualTo(Vector3.one));
                var sprite = shadowSprite.GetComponent<SpriteTransform>().VisualRenderer.sprite;
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(86f, 36f)));
            }

    [Test]
            public void PlantDefinition_DefaultCardLayoutUsesCenteredRoot()
            {
                var definition = new PlantDefinition();
                Assert.That(definition.SeedPacketScale, Is.EqualTo(0.5f));
                Assert.That(definition.SeedPacketLocalPosition, Is.EqualTo(Vector2.zero));
            }
    }
}
