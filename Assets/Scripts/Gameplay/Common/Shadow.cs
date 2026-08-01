using System.Collections.Generic;
using Script.Model;

namespace Prefab.Object.Shadow.Script
{
    public class Shadow : WorldObject
    {
        private float _size;
        
        public override string GetChineseName()
        {
            return "影子";
        }

        public override string GetEnglishName()
        {
            return "Shadow";
        }

        public Shadow SetSize(float size)
        {
            _size = size;
            Transform.localScale *= size;
            return this;
        }

        public Shadow Initialize(float size)
        {
            SetSize(size);
            SetSortingLayer("shadow");
            return this;
        }
    }
}
