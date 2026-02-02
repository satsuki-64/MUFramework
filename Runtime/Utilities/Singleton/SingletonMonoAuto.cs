using UnityEngine;

namespace MUFramework.Utilities
{
    /// <summary>
    /// 访问即可完成 Mono 的单例实例化，也可以将其挂载在场景中，由 Unity 自动完成实例化
    /// </summary>
    public class SingletonMonoAuto<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;
        
        public static T Instance
        {
            get
            {
                // 如果实例不存在，尝试在场景中查找
                if (instance == null)
                {
                    instance = FindFirstObjectByType<T>(); // 使用Unity最新的查找API

                    // 如果场景中也没找到，则自动创建一个新的GameObject和组件
                    if (instance == null)
                    {
                        GameObject singletonObject = new GameObject();
                        singletonObject.name = typeof(T).Name;
                        instance = singletonObject.AddComponent<T>();
                        DontDestroyOnLoad(singletonObject); // 设置为跨场景不销毁
                    }
                }
                
                return instance;
            }
        }

        protected virtual void Awake()
        {
            // 在 Awake 中初始化单例实例
            InitializeSingleton();
        }

        private void InitializeSingleton()
        {
            if (instance == null)
            {
                // 如果instance为空，将当前实例赋值给它
                instance = this as T;
                
                // 如果当前对象不是根对象，可以将其设置为根对象
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }
                
                DontDestroyOnLoad(gameObject); // 确保跨场景不销毁
            }
            else if (instance != this)
            {
                // 如果instance已存在且不是当前实例，说明存在重复，销毁当前游戏对象
                Debug.LogWarning($"额外的单例实例'{typeof(T)}' 被找到.将其删除.");
                DestroyImmediate(gameObject);
            }
            else
            {
                // 当 instance 不为空，并且等于当前实例时，不执行操作
            }
        }

        protected virtual void OnDestroy()
        {
            // 只有当被销毁的实例是真正的单例实例时，才将instance置为null
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}