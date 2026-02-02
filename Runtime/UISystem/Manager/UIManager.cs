using System.Collections;
using System.Collections.Generic;
using MUFramework.DataSystem;
using MUFramework.UISystem.Example;
using MUFramework.UISystem.MVVMFramework;
using MUFramework.Utilities;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MUFramework.UISystem
{
    public class UIManager : SingletonBase<UIManager>
    {
        public const int REFSCREENHIGHT = 1080;
        public const int REFSCREEWIDTG = 1920;
        
        /// <summary>
        /// 当前场景中的 Canvas
        /// </summary>
        private Canvas CurrentCanvas;

        /// <summary>
        /// 第一次进入场景后，首先设置当前场景的 CurrentCanvas
        /// </summary>
        public Canvas SetupCanvas()
        {
            // 0. 将老的 Canvas 移除
            if (CurrentCanvas  != null)
            {
                CurrentCanvas = null;
            }
            
            // 1. 将老的 Canvas 下的物体设置到新 Canvas 下
            Canvas[] canvasArray = GameObject.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            if (canvasArray != null && canvasArray.Length > 0) 
            {
                Log.Warning($"当前 {SceneManager.GetActiveScene().name} 场景当中存在Canvas，请检查此场景物体，将Canvas移除！",LogModule.UI);
                foreach (Canvas canvas in canvasArray)
                {
                    Transform[] transforms = canvas.transform.GetComponentsInChildren<Transform>(true);
                    foreach (Transform transform in transforms)
                    {
                        if (transform.parent == canvas.transform)
                        {
                            transform.SetParent(CurrentCanvas.transform);
                        }
                    }
                    
                    GameObject.Destroy(canvas);
                }
            }

            // 2. 创建新的 Canvas
            GameObject newCanvas = new GameObject("RootCanvas");
            Canvas canvasComponent = newCanvas.AddComponent<Canvas>();
            CurrentCanvas = canvasComponent;
            CurrentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler canvasScaler = newCanvas.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(REFSCREEWIDTG, REFSCREENHIGHT);
            newCanvas.AddComponent<GraphicRaycaster>();
            canvasComponent.enabled = true;
            
            return CurrentCanvas;
        }

        /// <summary>
        /// 根据资源类型，从本地加载 UI 资源，并将其挂载在当前场景的 Canvas 下
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public IEnumerator LoadUIAsync(UIInfo info, UnityAction<ViewComponent> callBack)
        {
            bool loadCompleted = false;

            if (info != null && CurrentCanvas != null)
            {
                AssetLoader.Instance.LoadResAsync<GameObject>(info.Name,info.Path,info.LoadType,
                    (GameObject obj) =>
                    {
                        Log.Debug($"当前加载路径为：{info.Path}，加载{info.Name}成功", LogModule.UI);
                        obj.SetActive(false);
                        // 注：SetParent 默认worldPositionStays为true，因此Unity会尝试计算，使得UI元素在世界坐标系下保持当前的位置、旋转和缩放
                        // 当 worldPositionStays参数设置为 false 时，UI元素（其RectTransform）会直接使用它在预制体中保存的本地值
                        obj.transform.SetParent(CurrentCanvas.transform,false);
                        ViewComponent viewComponent = obj.GetComponent<ViewComponent>();
                        loadCompleted = true;
                        callBack?.Invoke(viewComponent);
                    });

                yield return new WaitUntil(() => loadCompleted);
            }
            else
            {
                Log.Warning("当前场景中没有找到Canvas!", LogModule.UI);
            }
        }
    }
}