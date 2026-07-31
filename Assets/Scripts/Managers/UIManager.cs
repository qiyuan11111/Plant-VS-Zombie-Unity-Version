using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Script.Manager
{
    public class UIManager : MonoBehaviour
    {
        // Start is called before the first frame update
   
        public Dropdown dropDown;
        public List<Dropdown.OptionData> listOptions = new();
        public Resolution[] resolutions;
        void Start()
        {
            // 设置监听
            dropDown = GetComponent<Dropdown>();
        
            resolutions = Screen.resolutions;
            // foreach (var resolution in resolutions)
            listOptions.Add(new Dropdown.OptionData("1600*1200"));
            listOptions.Add(new Dropdown.OptionData("2160*1440"));
            // listOptions.Add(new Dropdown.OptionData("16:9"));
            // listOptions.Add(new Dropdown.OptionData("Option 1"));
            dropDown.ClearOptions();
            dropDown.AddOptions(listOptions);
            dropDown.onValueChanged.AddListener((value)=> {
                OnValueChange(value);
            });
            // SetDropDownAddListener();
            // SetDropDownItemValue(1);
        }
 
        /// <summary>
        /// 当点击后值改变是触发 (切换下拉选项)
        /// </summary>
        /// <param name="v">是点击的选项在OptionData下的索引值</param>
        void OnValueChange(int v)
        {
            //切换选项 时处理其他的逻辑...
            if(v == 0)
                Screen.SetResolution(1600, 1200, true);
            else
                Screen.SetResolution(2160, 1440, true);
        
            Screen.fullScreen = true;
        }
 
    }
}
