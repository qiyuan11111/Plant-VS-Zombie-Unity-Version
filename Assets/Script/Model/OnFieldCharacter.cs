using Script.Manager;

namespace Script.Model
{
    public abstract class OnFieldCharacter : OnFieldEntity
    {
        protected int Row;
        protected float Height;
        
        public OnFieldCharacter SetRow(int row)
        {
            Row = row;
            return this;
        }
        
        public OnFieldCharacter SetHeight(float height)
        {
            Height = height;
            return this;
        }

        public override OnFieldEntity SetPlaceMode(GridManager.Grid grid)
        {
            base.SetPlaceMode(grid);
            SetRow(grid.Point.y);
            SetHeight(0f);
            return this;
        }
    }
}