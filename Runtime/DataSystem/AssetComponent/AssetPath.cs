using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace MUFramework.DataSystem
{
    /// <summary>
    /// 路径类型枚举
    /// </summary>
    public enum PathType
    {
        ReadOnly,      // 只读路径（StreamingAssets）
        ReadWrite,     // 可读写路径（PersistentData）
        Temporary,     // 临时缓存路径
        ProjectRoot    // 项目根路径（仅编辑器）
    }
    
    public static class AssetPath
    {
        /// <summary>
        /// 获取指定类型的路径
        /// </summary>
        public static string GetPath(PathType type, string subPath = "")
        {
            // 添加平台兼容性检查
            if (type == PathType.ProjectRoot && !Application.isEditor)
            {
                Debug.LogWarning("ProjectRoot 路径主要在编辑器模式下使用，运行时将重定向到 PersistentDataPath");
            }

            // 获得当前模式下的根目录
            string basePath = GetBasePath(type);
            
            // 目录拼接
            string fullPath = Path.Combine(basePath, subPath);

            // 如果当前不是只读模式，则检查当前路径中的文件夹是否存在，如果不存在、则为其创建
            if (type == PathType.ReadWrite || type == PathType.Temporary || type == PathType.ProjectRoot)
            {
                string directory = Path.GetDirectoryName(fullPath);
                
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }

            return fullPath;   
        }
        
        /// <summary>
        /// 获取根目录
        /// </summary>
        /// <param name="type">目标路径类型</param>
        /// <returns></returns>
        private static string GetBasePath(PathType type)
        {
            return type switch
            {
                PathType.ReadOnly => Application.streamingAssetsPath,
                PathType.ReadWrite => Application.persistentDataPath,
                PathType.Temporary => Application.temporaryCachePath,
                PathType.ProjectRoot => GetProjectRootPath(),
                _ => Application.persistentDataPath
            };
        }
        
        private static string GetProjectRootPath()
        {
#if UNITY_EDITOR
            return Directory.GetParent(Application.dataPath).FullName;
#else
            return Application.persistentDataPath;
#endif
        }
        
        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        public static void ExistsAsync(string relativePath, PathType type, Action<bool> callback)
        {
            string fullPath = GetPath(type, relativePath);
            
            if (type == PathType.ReadOnly && Application.platform == RuntimePlatform.Android)
            {
                string uriPath = fullPath.StartsWith("jar:") ? fullPath : "jar:" + fullPath;
                var request = UnityWebRequest.Head(uriPath);
                var operation = request.SendWebRequest();
                operation.completed += (asyncOp) =>
                {
                    bool exists = request.responseCode == 200;
                    request.Dispose();
                    callback?.Invoke(exists);
                };
            }
            else
            {
                bool exists = File.Exists(fullPath);
                callback?.Invoke(exists);
            }
        }
    }
}