using System.Collections.Generic;

namespace Script.Model
{
    public interface IToField 
    {
        public T ToField<T>(Dictionary<string, object> param = null) where T : OnFieldEntity;
    }
}