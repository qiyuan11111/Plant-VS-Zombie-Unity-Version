using System.Collections.Generic;
using Script.Model;

namespace Prefab.Object.Shadow.Script
{
    public class Shadow : global::Script.Model.Object, IToField
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

        public override void AfterCreate(Dictionary<string, object> param)
        {
            throw new System.NotImplementedException();
        }

        public override Entity ToField(Dictionary<string, object> param = null)
        {
            SetSortingLayer("shadow");
            return this;
        }
    }
}
