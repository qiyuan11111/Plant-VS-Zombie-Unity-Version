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

        public override OnFieldEntity ToField(Dictionary<string, object> param = null)
        {
            return ToField<OnFieldShadow>(param).SetPlaceMode();
        }

        public T ToField<T>(Dictionary<string, object> param = null) where T : OnFieldEntity
        {
            return PrePareToField<OnFieldShadow>() as T;
        }
    }
}
