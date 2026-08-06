using NUnit.Framework;
using PvZ.Gameplay.Board;
using UnityEngine;

namespace Tests.EditMode
{
    public sealed class BoardGeometryTests
    {
        [Test]
        public void FrontYard_UsesOriginalEightyByOneHundredGrid()
        {
            var first = BoardGeometry.GetCell(BoardTerrain.FrontYard, 0, 0);
            var last = BoardGeometry.GetCell(BoardTerrain.FrontYard, 8, 4);

            Assert.That(first.LogicalOrigin, Is.EqualTo(new Vector2(-440f, 220f)));
            Assert.That(first.Center, Is.EqualTo(new Vector2(-400f, 170f)));
            Assert.That(first.Size, Is.EqualTo(new Vector2(80f, 100f)));
            Assert.That(last.LogicalOrigin, Is.EqualTo(new Vector2(200f, -180f)));
            Assert.That(last.Center, Is.EqualTo(new Vector2(240f, -230f)));
        }

        [Test]
        public void Pool_UsesSixRowsAtEightyFivePixels()
        {
            var lastRow = BoardGeometry.GetCell(BoardTerrain.Pool, 0, 5);

            Assert.That(BoardGeometry.GetRowCount(BoardTerrain.Pool), Is.EqualTo(6));
            Assert.That(lastRow.LogicalOrigin, Is.EqualTo(new Vector2(-440f, -205f)));
            Assert.That(lastRow.Center, Is.EqualTo(new Vector2(-400f, -247.5f)));
            Assert.That(lastRow.Size, Is.EqualTo(new Vector2(80f, 85f)));
        }

        [Test]
        public void Roof_AppliesOriginalColumnSlope()
        {
            var left = BoardGeometry.GetCell(BoardTerrain.Roof, 0, 0);
            var flat = BoardGeometry.GetCell(BoardTerrain.Roof, 5, 0);

            Assert.That(left.LogicalOrigin, Is.EqualTo(new Vector2(-440f, 130f)));
            Assert.That(flat.LogicalOrigin, Is.EqualTo(new Vector2(-40f, 230f)));
            Assert.That(flat.LogicalOrigin.y - left.LogicalOrigin.y, Is.EqualTo(100f));
        }

        [Test]
        public void HighGround_RaisesUnityPositionByThirtyPixels()
        {
            var normal = BoardGeometry.GetCell(BoardTerrain.FrontYard, 2, 2);
            var high = BoardGeometry.GetCell(BoardTerrain.FrontYard, 2, 2, true);

            Assert.That(high.LogicalOrigin.y - normal.LogicalOrigin.y, Is.EqualTo(30f));
            Assert.That(high.Center.y - normal.Center.y, Is.EqualTo(30f));
        }

        [Test]
        public void CursorHotspot_MapsDirectlyToOriginalPlantDrawOrigin()
        {
            Assert.That(
                BoardGeometry.CursorPlantDrawOriginOffset,
                Is.EqualTo(new Vector2(-35f, 60f)));
        }

        [Test]
        public void HitConversion_UsesOriginalFrontYardAndRoofRules()
        {
            Assert.That(
                BoardGeometry.TryLocalPositionToCell(
                    BoardTerrain.FrontYard,
                    new Vector2(-400f, 170f),
                    out var frontPoint),
                Is.True);
            Assert.That(frontPoint, Is.EqualTo(new Vector2Int(0, 0)));

            var roofCell = BoardGeometry.GetCell(BoardTerrain.Roof, 0, 2);
            Assert.That(
                BoardGeometry.TryLocalPositionToCell(BoardTerrain.Roof, roofCell.Center, out var roofPoint),
                Is.True);
            Assert.That(roofPoint, Is.EqualTo(new Vector2Int(0, 2)));

            Assert.That(
                BoardGeometry.TryLocalPositionToCell(
                    BoardTerrain.FrontYard,
                    new Vector2(-441f, 170f),
                    out _),
                Is.False);
        }
    }
}
