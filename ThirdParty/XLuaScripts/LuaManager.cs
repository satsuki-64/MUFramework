using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace XLua
{
    /// <summary>
    /// XLua 管理器
    /// </summary>
    public class LuaManager
    {
        private static LuaManager instance;

        public static LuaManager Instance()
        {
            if (instance == null)
            {
                instance = new LuaManager();
            }
            
            return instance;
        }

        private LuaEnv luaEnv;

        public LuaEnv GetLuaEnv()
        {
            if (luaEnv == null)
            {
                luaEnv = new LuaEnv();
                Init();
            }

            return luaEnv;    
        }

        private List<FileInfo> LuaFileList = new List<FileInfo>();
        public List<string> XLuaPathList = new List<string>();
        
        private void Init()
        {
            var pathConfig = Resources.Load<XLuaPathConfig>("XLuaPathConfig");
            if (pathConfig != null)
            {
                XLuaPathList.Clear();
                XLuaPathList.AddRange(pathConfig.luaPaths);
                Debug.Log("使用本地的XLuaPathConfig作为XLua加载器");
            }
            else
            {
                Debug.Log("未找到XLua配置文件！");
                // 使用默认路径（备用）
                XLuaPathList.Clear();
                XLuaPathList.Add(Path.Combine(Application.dataPath, "MUFramework/ThirdParty/XLuaScripts/Lua"));
                XLuaPathList.Add(Path.Combine(Application.dataPath, "MUFramework/Runtime/Utilities/Example"));
            }
            
            GetLuaEnv().AddLoader(MyCustomLoader);
            GetLuaEnv().DoString("require('Main')");
            Debug.Log("Xlua 初始化完成！");
        }
        
        public bool DoLuaFile(string luaFileName)
        {
            if (luaFileName != null) 
            {
                GetLuaEnv().DoString($"require('{luaFileName}')");
                return true;
            }
            
            return false;
        }
        
        private byte[] MyCustomLoader(ref string filePath)
        {
            foreach (string pathRootLua in XLuaPathList)
            {
                // 1. 从目标 Lua 文件路径中，读取所有的 Lua 文件
                GetAllLuaFiles(pathRootLua);

                // 2. 遍历每一个文件，检查其是否为目标代码名
                foreach (FileInfo file in LuaFileList)
                {
                    // Debug.Log("当前File的Name：" + file.Name + "; " + "当前filePath：" + filePath + "; ");
                    
                    if (file.Name == (filePath + ".lua"))
                    {
                        // Debug.Log($"完整FilePath:{file.FullName}，是否存在：{File.Exists(file.FullName)}");
                        
                        if (File.Exists(file.FullName))
                        {
                            // Debug.Log($"找到Lua文件，文件为：{file.Name}");
                            
                            byte[] fileBytes = File.ReadAllBytes(file.FullName);
                            // 检查并移除 UTF-8 BOM (EF BB BF)
                            if (fileBytes.Length >= 3 && fileBytes[0] == 0xEF && fileBytes[1] == 0xBB && fileBytes[2] == 0xBF)
                            {
                                byte[] trimmedBytes = new byte[fileBytes.Length - 3];
                                Buffer.BlockCopy(fileBytes, 3, trimmedBytes, 0, trimmedBytes.Length);
                                return trimmedBytes;
                            }
                        
                            return fileBytes;
                        }
                        else
                        {
                            Debug.LogError($"MyCustomLoader failed: {filePath}.lua not found");
                            return null;
                        }
                    }
                }
            }
            
            Debug.LogWarning("MyCustomLoader failed");
            return null;
        }

        public List<FileInfo> GetAllLuaFiles(string pathRootLua)
        {
            LuaFileList.Clear();
            
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(pathRootLua);

                // 检查目录是否存在，如果不存在，则创建
                if (!directoryInfo.Exists)
                {
                    Debug.LogWarning($"Directory {pathRootLua} not found");
                    Directory.CreateDirectory(pathRootLua);
                    return null;
                }

                // 获得当前 pathRootLua 下面的所有文件，并且递归的获取子文件夹下的所有lua文件
                FileInfo[] fileInfos = directoryInfo.GetFiles("*.lua", SearchOption.AllDirectories);
                LuaFileList.AddRange(fileInfos);
            }
            catch (Exception e)
            {
                Debug.LogError($"获取Lua文件时发生错误：{e.Message}");
            }

            return LuaFileList;
        }
    }
}