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

    }
}
