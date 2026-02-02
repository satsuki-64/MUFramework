using MUFramework.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace MUFramework.DataSystem 
{
    public enum AssetLoadType
    {
        AB,
        Resources,
        AssetDatabase
    }

    public sealed partial class AssetLoader : SingletonBase<AssetLoader>
    {
        private ABLoader abLoader;
        private ResLoader resLoader;
        
        public void Init()
        {
            abLoader = new ABLoader();
            resLoader = new ResLoader();
        }

        public void Unload()
        {
            abLoader?.UnloadAll();
            abLoader = null;
            resLoader?.UnloadAll();
            resLoader = null;
        }

        public void LoadResAsync<T>(AssetInfo assetInfo, UnityAction<T> callBack) where T : Object
        {
            if (assetInfo != null)
            {
                LoadResAsync<T>(assetInfo.Name, assetInfo.Path, assetInfo.LoadType, callBack);
            }
            else
            {
                Debug.LogWarning("assetInfo is null!Please init the AssetLoader!");
            }
        }

        public void LoadResAsync<T>(string resName, string resPath, AssetLoadType loadType, UnityAction<T> callBack) where T : Object
        {
            switch (loadType)
            {
                case AssetLoadType.AB:
                    LoadResFromAssetBundle<T>(resName, resPath, callBack);
                    break;
                case AssetLoadType.Resources:
                    LoadResFromResources<T>(resPath, callBack);
                    break;
                case AssetLoadType.AssetDatabase:
                    #if UNITY_EDITOR
                        Object tempObj = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(resPath);
                    
                        // 在加载资源时 判断一下 资源是不是 GameObject，如果是 直接实例化了 再返回给外部
                        if (tempObj != null)
                        {
                            // 异步加载结束后 通过委托 传递给外部 外部来使用
                            if (typeof(T) is GameObject)
                                callBack(GameObject.Instantiate(tempObj) as T);
                            else
                                callBack(tempObj as T);
                        }
                    #else
                        Debug.LogWarning("不可在非编辑器模式下使用AssetDatabase加载！请改用其他接口");
                    #endif
                    break;
            }
        }

        /// <summary>
        /// 异步的从 AB 包中加载资源并缓存
        /// </summary>
        /// <param name="abName">AB包名称</param>
        /// <param name="resName">资源名称</param>
        /// <param name="callBack">加载完成后，执行的回调</param>
        /// <typeparam name="T">要加载的资源的类型</typeparam>
        public void LoadResFromAssetBundle<T>(string abName, string resName, UnityAction<T> callBack) where T : Object
        {
            if (abLoader!=null)
            {
                SingletonMonoMgr.Instance.StartCoroutine(abLoader.ReallyLoadResAsync<T>(abName, resName, callBack));
            }
            else
            {
                Debug.LogWarning("abLoader is null!Please init the AssetLoader!");
            }
        }

        /// <summary>
        /// 异步的从 Resources 中加载资源并缓存
        /// </summary>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        /// <typeparam name="T"></typeparam>
        public void LoadResFromResources<T>(string name, UnityAction<T> callback) where T : Object
        {
            if (resLoader != null)
            {
                SingletonMonoMgr.Instance.StartCoroutine(resLoader.ReallyLoadSceneAsync(name, callback));
            }
            else
            {
                Debug.LogWarning("resLoader is null!Please init the AssetLoader!");
            }
        }
    }
}