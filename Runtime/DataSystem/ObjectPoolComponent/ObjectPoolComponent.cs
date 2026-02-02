using System.Collections.Generic;
using MUFramework.Utilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace MUFramework.DataSystem
{
    public class ObjectPoolComponent : SingletonBase<ObjectPoolComponent>
    {
        /// <summary>
        /// 存储不同类型的资源列表的总字典
        /// </summary>
        public Dictionary<string, GameObjectPool> PoolDic = new Dictionary<string, GameObjectPool>();

        /// <summary>
        /// 当前对象池挂载在场景中的对象上
        /// </summary>
        private GameObject rootObject;

        /// <summary>
        /// 创建对象池
        /// </summary>
        /// <param name="prefab">对象预制体</param>
        /// <param name="poolName">对象池名称</param>
        /// <param name="maxCount">池最大容量</param>
        /// <param name="initialCount">初始池容量</param>
        /// <param name="pattern">增长模式</param>
        /// <param name="isAutoRecycle">是否自动回收</param>
        /// <param name="recycleTime">自动回收时间</param>
        /// <returns></returns>
        public GameObjectPool CreateGameObjectPool(GameObject prefab, string poolName, int maxCount,
            int initialCount = 0, GrowthPattern pattern = GrowthPattern.LinearGrowth, bool isAutoRecycle = false,float recycleTime = 10f)
        {
            if (rootObject == null)
            {
                rootObject = new GameObject("ObjectPoolRoot");
            }

            if (PoolDic.ContainsKey(poolName))
            {
                return PoolDic[poolName];
            }
            else
            {
                GameObjectPool gameObjectPool = new GameObjectPool(maxCount, rootObject, prefab, poolName, initialCount,
                    pattern, isAutoRecycle,recycleTime);
                PoolDic.Add(poolName, gameObjectPool);
                return gameObjectPool;
            }
        }

        /// <summary>
        /// 从池中获取对象
        /// </summary>
        /// <param name="poolName">池名称</param>
        /// <returns>对象实例</returns>
        public GameObject GetGameObjectFromPool(string poolName)
        {
            // 如果存在指定资源类型的缓存列表，并且数量不为 0
            if (PoolDic.ContainsKey(poolName))
            {
                return PoolDic[poolName].Get();
            }
            else
            {
                Debug.LogWarning($"{poolName}对象池不存在");
                return null;
            }
        }
        
        /// <summary>
        /// 返还资源到缓存池
        /// </summary>
        /// <param name="name"></param>
        /// <param name="go"></param>
        public void ReleaseGameObject(string poolName, GameObject go)
        {
            if (PoolDic.ContainsKey(poolName))
            {
                PoolDic[poolName].Release(go);
            }
            else
            {
                Debug.LogWarning($"{poolName}对象池不存在");
            }
        }

        /// <summary>
        /// 清空缓存池的方法，主要用在切换场景时
        /// </summary>
        public void Clear()
        {
            PoolDic.Clear();
            rootObject = null;
        }
    }
}