using System.Collections.Generic;
using MUFramework.Utilities;
using UnityEngine;

namespace MUFramework.DataSystem
{
    public sealed partial class AssetUpdate : SingletonMonoAuto<AssetUpdate>
    {
        public class ABInfo
        {
            public string name;
            public long size;
            public string md5;

            public ABInfo(string name, string size, string md5)
            {
                this.name = name;
                this.size = long.Parse(size);
                this.md5 = md5;
            }

            public void DebugABInfo()
            {
                Debug.Log("当前加载的AB包：" + name);
                Debug.Log("大小为：" + size + " " + "MD5码为：" + md5);
            }
        
            public static void ABContentSplit(string info, Dictionary<string, ABInfo> abInfoDict)
            {
                if (abInfoDict != null)
                {
                    // 按照换行符来进行拆分
                    string[] strs = info.Split('\n');
                    string[] oneABInfo = null;
            
                    foreach (string str in strs)
                    {
                        if (!string.IsNullOrEmpty(str))
                        {
                            oneABInfo = str.Split(' ');
                            ABInfo tempABInfo = new ABInfo(oneABInfo[0],oneABInfo[1],oneABInfo[2]);
                    
                            // 将AB包的名字作为键，存入字典当中
                            abInfoDict.Add(oneABInfo[0], tempABInfo);
                        }
                    }    
                }
                else
                {
                    Debug.LogWarning("ABContentSplit 中字典为空！");
                }
            }
        }
    }
}