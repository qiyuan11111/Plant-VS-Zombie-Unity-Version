using System.Collections.Generic;
using NUnit.Framework;
using PvZ.Gameplay.Board;
using PvZ.Gameplay.Plants;
using UnityEngine;

namespace Tests.EditMode
{
    public sealed class GridOccupancyTests
    {
        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var gameObject in _objects)
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }

            _objects.Clear();
        }

        [Test]
        public void Grid_AllowsOnlyItsOccupantToReleaseTheCell()
        {
            var grid = new GridManager.Grid(new Vector2Int(2, 3), new Vector2(20f, 30f));
            var firstPlant = CreatePlant("First");
            var otherPlant = CreatePlant("Other");

            Assert.That(grid.IsOccupied(), Is.False);
            Assert.That(grid.TryOccupy(null), Is.False);
            Assert.That(grid.TryOccupy(firstPlant), Is.True);
            Assert.That(grid.Occupant, Is.SameAs(firstPlant));
            Assert.That(grid.TryOccupy(otherPlant), Is.False);
            Assert.That(grid.TryRelease(otherPlant), Is.False);
            Assert.That(grid.Occupant, Is.SameAs(firstPlant));
            Assert.That(grid.TryRelease(firstPlant), Is.True);
            Assert.That(grid.IsOccupied(), Is.False);
            Assert.That(grid.Occupant, Is.Null);
        }

        [Test]
        public void Grid_CanBeOccupiedAgainAfterRelease()
        {
            var grid = new GridManager.Grid(Vector2Int.zero, Vector2.zero);
            var firstPlant = CreatePlant("First");
            var secondPlant = CreatePlant("Second");

            Assert.That(grid.TryOccupy(firstPlant), Is.True);
            Assert.That(grid.TryRelease(firstPlant), Is.True);
            Assert.That(grid.TryOccupy(secondPlant), Is.True);
            Assert.That(grid.Occupant, Is.SameAs(secondPlant));
        }

        [Test]
        public void NoneGrid_RejectsOccupancy()
        {
            var plant = CreatePlant("Plant");

            Assert.That(GridManager.Grid.None.TryOccupy(plant), Is.False);
            Assert.That(GridManager.Grid.None.IsOccupied(), Is.False);
            Assert.That(GridManager.Grid.None.Occupant, Is.Null);
        }

        private TestPlant CreatePlant(string name)
        {
            var gameObject = new GameObject(name);
            _objects.Add(gameObject);
            return gameObject.AddComponent<TestPlant>();
        }
    }

    internal sealed class TestPlant : PlantEntity
    {
        public override string GetChineseName()
        {
            return "测试植物";
        }

        public override string GetEnglishName()
        {
            return "TestPlant";
        }
    }
}
