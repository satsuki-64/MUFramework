using UnityEngine;
using XLua;

namespace MUFramework.Runtime.Utilities.Example
{
    public class XLuaTest : MonoBehaviour
    {
        private void Start()
        {
            // LuaManager.DoLuaFile("Test");
            // LuaManager.DoLuaFile("Tes2");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                LuaManager.Instance().DoLuaFile("Test");
                LuaManager.Instance().DoLuaFile("Test2");
            }
        }
    }
}