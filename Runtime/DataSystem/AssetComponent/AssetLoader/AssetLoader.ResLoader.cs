using System.Collections;
using System.Collections.Generic;
using MUFramework.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace MUFramework.DataSystem
{
    public sealed partial class AssetLoader : SingletonBase<AssetLoader>
    {
        private sealed class ResLoader
        {
            // 资源缓存字典
            private Dictionary<string, Object> resourceCache = new Dictionary<string, Object>();
            
            public IEnumerator ReallyLoadSceneAsync<T>(string name,UnityAction<T> callback) where T : Object
            {
                // 1. 检查缓存
                if (resourceCache.ContainsKey(name))
                {
                    T cachedRes = resourceCache[name] as T;
                    
                    if (cachedRes != null)
                    {
                        // 直接返回缓存结果，确保异步回调也是异步执行的（使用协程等待一帧）
                        yield return null;
                        callback?.Invoke(cachedRes);
                        yield break;
                    }
                    else
                    {
                        resourceCache.Remove(name);
                    }
                }
                
                // 2. 加载资源
                ResourceRequest request = Resources.LoadAsync<T>(name);
                yield return request;
                
                // 3. 检查加载结果
                if (request.asset == null)
                {
                    Debug.LogError($"异步加载资源失败。路径: {name}, 类型: {typeof(T)}");
                    callback?.Invoke(null);
                    yield break;
                }

                // 4. 缓存已加载的资源
                T loadedAsset = request.asset as T;
                if (loadedAsset != null)
                {
                    resourceCache[name] = loadedAsset;
                }
                
                // 5. 回调：返回资源本身
                if (typeof(T) == typeof(GameObject))
                {
                    callback?.Invoke(GameObject.Instantiate(loadedAsset)); 
                }
                else
                {
                    callback?.Invoke(loadedAsset);
                }
            }

            public void UnloadAll()
            {
                resourceCache?.Clear();
            }
        }
    }
}