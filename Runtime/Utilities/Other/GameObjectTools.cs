using UnityEngine;

namespace MUFramework.Utilities
{
    public static class GameObjectTools
    {
        /// <summary>
        /// 从目标对象中根据名字找到子对象
        /// </summary>
        /// <param name="parentObject">父物体</param>
        /// <param name="childName">待寻找的子物体名称</param>
        /// <returns></returns>
        public static GameObject FindObjectInChild(GameObject parentObject, string childName) 
        {
            Transform[] transforms = parentObject.GetComponentsInChildren<Transform>();

            foreach (Transform tra in transforms)
            {
                if (tra.gameObject.name == childName) 
                {
                    return tra.gameObject;
                }
            }

            Debug.LogWarning($"没有在{parentObject.name}中找到{childName}物体！");
            return null;
        }

        /// <summary>
        /// 获得当前场景中的 Canvas
        /// </summary>
        /// <returns>Canvas Object</returns>
        public static GameObject FindCanvas() 
        {
            GameObject canvas = GameObject.FindObjectOfType<Canvas>().gameObject;
            
            if (canvas == null)  
            {
                Debug.LogError("当前场景当中没有Canvas,请添加！");
            }

            return canvas;
        }

        /// <summary>
        /// 从目标Panel的子物体中，根据子物体的名称获得对应组件 
        /// </summary>
        /// <typeparam name="T">对应组件</typeparam>
        /// <param name="panel">目标Panel</param>
        /// <param name="ComponentName">子物体名称</param>
        /// <returns></returns>
        public static T GetComponentInChild<T>(GameObject parentObject, string childName) where T : Component
        {
            Transform[] transforms = parentObject.GetComponentsInChildren<Transform>();

            foreach (Transform tra in transforms)
            {
                if (tra.gameObject.name == childName)
                {
                    T component;
                    if (tra.gameObject.TryGetComponent(out component))
                    {
                        return component;
                    }
                    
                    Debug.LogWarning($"没有在{parentObject.name}中找到{typeof(T)}组件！");
                    return null;
                }
            }

            Debug.LogWarning($"没有在{parentObject.name}中找到{childName}物体！");
            return null;
        }
    }
}