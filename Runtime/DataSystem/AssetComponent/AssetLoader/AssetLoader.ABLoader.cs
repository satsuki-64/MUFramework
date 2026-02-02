using System.Collections;
using System.Collections.Generic;
using System.IO;
using MUFramework.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace MUFramework.DataSystem
{
    public sealed partial class AssetLoader : SingletonBase<AssetLoader>
    {
        private sealed class ABLoader
        {
            /// <summary>
            /// 当前加载得到的主包
            /// </summary>
            private AssetBundle mainAB = null;
            
            // 依赖包获取用的配置文件
            private AssetBundleManifest manifest = null;

            // AB包不能够重复加载 重复加载会报错
            // 用字典来存储 加载过的AB包 
            private Dictionary<string, AssetBundle> abDic = new Dictionary<string, AssetBundle>();
            private List<string> loadingABNames = new List<string>();

            /// <summary>
            /// 这个AB包存放路径 方便修改
            /// </summary>
            private string PathUrl
            {
                get
                {
                    return Application.streamingAssetsPath;
                }
            }

            /// <summary>
            /// 主包名 方便修改
            /// </summary>
            private string MainABName
            {
                get
                {
        #if UNITY_IOS
                    return "IOS";
        #elif UNITY_ANDROID
                    return "Android";
        #else
                    return "PC";
        #endif
                }
            }

            public void LoadAB(string abName)
            {
                // 防止重复加载
                if (abDic.ContainsKey(abName)) return;

                // 防止加载正在加载中的 AB 包
                if (loadingABNames.Contains(abName)) return;
                
                SingletonMonoMgr.Instance.StartCoroutine(ReallyLoadAB(abName));
            }

            /// <summary>
            /// 加载 AB 包
            /// </summary>
            /// <param name="abName"></param>
            private IEnumerator ReallyLoadAB(string abName)
            {
                AssetBundle ab = null;
                loadingABNames.Add(abName);
                
                // 1. 加载主 AB 包以及 manifest
                if (mainAB == null)
                {
                    AssetBundleCreateRequest mainABRequest =
                        AssetBundle.LoadFromFileAsync(Path.Combine(PathUrl, MainABName));
                    yield return mainABRequest;

                    if (mainABRequest.assetBundle == null)
                    {
                        Debug.LogWarning("主包加载失败！");
                        yield break;
                    }
                    
                    mainAB = mainABRequest.assetBundle;
                    manifest = mainAB.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                }

                // 2. 加载依赖包
                string[] strs = manifest.GetAllDependencies(abName);
                for (int i = 0; i < strs.Length; i++)
                {
                    //判断包是否加载过，如果没加载过则加载
                    if (!abDic.ContainsKey(strs[i]))
                    {
                        loadingABNames.Add(strs[i]);
                        yield return SingletonMonoMgr.Instance.StartCoroutine(ReallyLoadAB(strs[i]));
                        loadingABNames.Remove(strs[i]);
                    }
                }
                
                // 3. 最后加载目标 AB 包
                if (!abDic.ContainsKey(abName))
                {
                    AssetBundleCreateRequest abRequest = AssetBundle.LoadFromFileAsync(Path.Combine(PathUrl, abName));
                    yield return abRequest;

                    if (abRequest.assetBundle == null)
                    {
                        Debug.LogWarning("AB包加载失败: " + abName);
                        yield break;
                    }

                    ab = abRequest.assetBundle;
                    abDic.Add(abName, ab);
                    loadingABNames.Remove(abName);
                }
            }

            // 同步加载 不指定类型
            public Object LoadRes(string abName, string resName)
            {
                // 加载AB包
                LoadAB(abName);
                
                // 为了外面方便 在加载资源时 判断一下 资源是不是 GameObject
                // 如果是 直接实例化了 再返回给外部
                Object obj = abDic[abName].LoadAsset(resName);
                if (obj is GameObject)
                    return GameObject.Instantiate(obj);
                else
                    return obj;
            }

            // 同步加载 根据泛型指定类型
            public T LoadRes<T>(string abName, string resName) where T:Object
            {
                //加载AB包
                LoadAB(abName);
                
                //为了外面方便 在加载资源时 判断一下 资源是不是GameObject
                //如果是 直接实例化了 再返回给外部
                T obj = abDic[abName].LoadAsset<T>(resName);
                
                if (obj is GameObject)
                    return GameObject.Instantiate(obj);
                else
                    return obj;
            }
            

            //根据Type异步加载资源
            public void LoadResAsync(string abName, string resName, System.Type type, UnityAction<Object> callBack)
            {
                SingletonMonoMgr.Instance.StartCoroutine(ReallyLoadResAsync(abName, resName, type, callBack));
            }
            
            private IEnumerator ReallyLoadResAsync(string abName, string resName, System.Type type, UnityAction<Object> callBack)
            {
                // 如果对应的 AB 包未加载，先加载对应的 AB 包
                if (!abDic.ContainsKey(abName))
                {
                    yield return SingletonMonoMgr.Instance.StartCoroutine(ReallyLoadAB(abName));
                }
                
                // 在加载资源时 判断一下 资源是不是GameObject
                // 如果是 直接实例化了 再返回给外部
                AssetBundleRequest abr = abDic[abName].LoadAssetAsync(resName, type);
                yield return abr;
                //异步加载结束后 通过委托 传递给外部 外部来使用
                if (abr.asset is GameObject)
                    callBack(GameObject.Instantiate(abr.asset));
                else
                    callBack(abr.asset);
            }
            
            public IEnumerator ReallyLoadResAsync<T>(string abName, string resName, UnityAction<T> callBack)
                where T : Object
            {
                // 如果对应的 AB 包未加载，先加载对应的 AB 包
                if (!abDic.ContainsKey(abName))
                {
                    yield return SingletonMonoMgr.Instance.StartCoroutine(ReallyLoadAB(abName));
                }

                AssetBundleRequest abr = abDic[abName].LoadAssetAsync<T>(resName);
                yield return abr;
                
                // 在加载资源时 判断一下 资源是不是 GameObject，如果是 直接实例化了 再返回给外部
                if (abr.asset != null)
                {
                    // 异步加载结束后 通过委托 传递给外部 外部来使用
                    if (abr.asset is GameObject)
                        callBack(GameObject.Instantiate(abr.asset) as T);
                    else
                        callBack(abr.asset as T);
                }
                else
                {
                    Debug.LogWarning($"AB {abName} of {resName} Res fail to load！");
                }
            }

            //单个包卸载
            public void UnLoad(string abName)
            {
                if( abDic.ContainsKey(abName) )
                {
                    abDic[abName].Unload(false);
                    abDic.Remove(abName);
                }
            }

            //所有包的卸载
            public void UnloadAll()
            {
                AssetBundle.UnloadAllAssetBundles(false);
                abDic.Clear();
                mainAB = null;
                manifest = null;
            }
        }
    }
}