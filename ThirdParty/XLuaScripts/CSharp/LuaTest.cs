using UnityEngine;
using XLua;
using XLua.LuaDLL;
using System.Collections.Generic;

namespace MyLuaTestClass
{
    public class LuaTest : MonoBehaviour
    {
        private CustomCall c1;
        
        private void Start()
        {
            LuaManager.Instance().DoLuaFile("Test4");
            c1 =  LuaManager.Instance().GetLuaEnv().Global.Get<CustomCall>("DoCameraMove");
        }

        private void Update()
        {
            c1();
        }

        private void Test1()
        {
            Debug.Log(LuaManager.Instance().GetLuaEnv().Global.Get<int>("testNumber"));
            Debug.Log(LuaManager.Instance().GetLuaEnv().Global.Get<bool>("testBool"));
            Debug.Log(LuaManager.Instance().GetLuaEnv().Global.Get<string>("testString"));
            Debug.Log(LuaManager.Instance().GetLuaEnv().Global.Get<float>("testFloat"));
        }

        #region Test2

        [CSharpCallLua]
        public delegate void CustomCall();

        [CSharpCallLua]
        public delegate int CustomCall2(int a);

        [CSharpCallLua]
        public delegate int CustomCall3_New(int a,int b,out int c);

        [CSharpCallLua]
        public delegate int CustomCall4(string a, params int[] args);
        private void Test2()
        {
            CustomCall c1 = LuaManager.Instance().GetLuaEnv().Global.Get<CustomCall>("testFun");
            c1();
            
            CustomCall2 c2 = LuaManager.Instance().GetLuaEnv().Global.Get<CustomCall2>("testFun2");
            Debug.Log(c2(123));
            
            CustomCall3_New c3 = LuaManager.Instance().GetLuaEnv().Global.Get<CustomCall3_New>("testFun3");
            int b = 123;
            int c;
            Debug.Log(c3(123,b,out c));
            Debug.Log(c);
            
            CustomCall4 c4 =  LuaManager.Instance().GetLuaEnv().Global.Get<CustomCall4>("testFun4");
            c4("你好",1,2,3,4,54,6,7);
        }

        #endregion
        
        private void Test3()
        {
            // 1. List
            List<int> list1 = LuaManager.Instance().GetLuaEnv().Global.Get<List<int>>("testList");
            foreach (int i in list1)
            {
                Debug.Log(i);
            }
            
            List<object> list2 = LuaManager.Instance().GetLuaEnv().Global.Get<List<object>>("testList2");
            foreach (object o in list2)
            {
                Debug.Log(o.ToString());
            }
            
            // 2. 字典
            Dictionary<string,int> dic1 = LuaManager.Instance().GetLuaEnv().Global.Get<Dictionary<string,int>>("testDic");
            foreach (KeyValuePair<string, int> kv in dic1)
            {
                Debug.Log("Key："+kv.Key);
                Debug.Log("Value："+kv.Value);
            }
            
            Dictionary<object,object>  dic2 = LuaManager.Instance().GetLuaEnv().Global.Get<Dictionary<object, object>>("testDic2");
            foreach (object key in dic2.Keys)
            {
                Debug.Log("Key:"+key);
                Debug.Log("Value:"+dic2[key]);
            }
        }
        
        #region Test4
        public enum MyEnum
        {
            Idle,
            Attack,
            Run
        }
        #endregion
    }
}