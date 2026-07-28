using Script.Manager;

namespace Script.Model
{
    public abstract class Character : Entity
    {
        protected int Row;
        protected float Height;
        
        public Character SetRow(int row)
        {
            Row = row;
            return this;
        }
        
        public Character SetHeight(float height)
        {
            Height = height;
            return this;
        }

        public override Entity SetPlaceMode(GridManager.Grid grid)
        {
            base.SetPlaceMode(grid);
            
            this.SetRow(grid.Point.y)
                .SetHeight(0f);
            return this;
        }
    }
}