using System.Collections.Generic;

namespace Script.Model
{
    public interface IToField 
    {
        Entity ToField(Dictionary<string, object> param = null);
    }
}
