using System.Collections.Generic;
using NUnit.Framework;
using PvZ.Gameplay.Board;
using PvZ.Gameplay.Plants;
using UnityEngine;

namespace PvZ.Tests.EditMode.Board
{
    public sealed class BoardCellOccupancyTests
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
        public void Cell_AllowsOnlyItsOccupantToReleaseTheCell()
        {
            var cell = new BoardCell(new Vector2Int(2, 3), new Vector2(20f, 30f));
            var firstPlant = CreatePlant("First");
            var otherPlant = CreatePlant("Other");

            Assert.That(cell.IsOccupied, Is.False);
            Assert.That(cell.TryOccupy(null), Is.False);
            Assert.That(cell.TryOccupy(firstPlant), Is.True);
            Assert.That(cell.Occupant, Is.SameAs(firstPlant));
            Assert.That(cell.TryOccupy(otherPlant), Is.False);
            Assert.That(cell.TryRelease(otherPlant), Is.False);
            Assert.That(cell.Occupant, Is.SameAs(firstPlant));
            Assert.That(cell.TryRelease(firstPlant), Is.True);
            Assert.That(cell.IsOccupied, Is.False);
            Assert.That(cell.Occupant, Is.Null);
        }

        [Test]
        public void Cell_CanBeOccupiedAgainAfterRelease()
        {
            var cell = new BoardCell(Vector2Int.zero, Vector2.zero);
            var firstPlant = CreatePlant("First");
            var secondPlant = CreatePlant("Second");

            Assert.That(cell.TryOccupy(firstPlant), Is.True);
            Assert.That(cell.TryRelease(firstPlant), Is.True);
            Assert.That(cell.TryOccupy(secondPlant), Is.True);
            Assert.That(cell.Occupant, Is.SameAs(secondPlant));
        }

        [Test]
        public void SentinelCell_RejectsOccupancy()
        {
            var plant = CreatePlant("Plant");

            Assert.That(BoardCell.None.TryOccupy(plant), Is.False);
            Assert.That(BoardCell.None.IsOccupied, Is.False);
            Assert.That(BoardCell.None.Occupant, Is.Null);
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
