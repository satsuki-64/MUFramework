using System.Collections.Generic;
using UnityEngine;

namespace XLua
{
    public class XLuaPathConfig : ScriptableObject
    {
        [SerializeField]
        public List<string> luaPaths = new List<string>();
        
        [SerializeField]
        public string defaultLuaFile = "Main";
    }
}