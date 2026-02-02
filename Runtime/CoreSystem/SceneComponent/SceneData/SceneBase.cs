using System.Collections;
using System.Collections.Generic;
using MUFramework.UISystem;
using MUFramework.UISystem.Example;
using MUFramework.UISystem.MVVMFramework;
using MUFramework.Utilities;
using UnityEngine;

namespace MUFramework.CoreSystem
{
    public abstract class SceneBase
    {
        public string SceneName { get; }

        private Dictionary<UIInfo, ViewComponent> UIViews = new Dictionary<UIInfo, ViewComponent>();

        public SceneBase(string sceneName)
        {
            SceneName = sceneName;
        }

        private SceneBase() { }

        /// <summary>
        /// 加载本地资源
        /// </summary>
        public IEnumerator Load()
        {
            // 加载 UI 资源
            if (UIViews != null && UIViews.Count > 0)
            {
                Log.Debug("设置Canvas", LogModule.UI);
                UIManager.Instance.SetupCanvas();
                
                int totalCount = UIViews.Keys.Count;
                int completedCount = 0;

                foreach (UIInfo uiInfo in UIViews.Keys)
                {
                    // 根据 UIInfos 里面的信息从本地加载并实例化 ViewBase 及其物体，然后将其加入 UIViews 字典
                    SingletonMonoMgr.Instance.StartCoroutine
                    (
                        UIManager.Instance.LoadUIAsync(uiInfo, (viewComponent) =>
                        {
                            completedCount++;
                            if (viewComponent != null)
                            {
                                UIViews[uiInfo] = viewComponent;
                                Log.Debug($"当前成功找到{UIViews[uiInfo].gameObject.name}物体上面的ViewComponent!", LogModule.UI);
                            }
                            else
                            {
                                Log.Warning($"当前{uiInfo.Name}物体的ViewComponent未能成功获取！", LogModule.UI);
                            }
                            
                        })
                    );
                }

                // 等待所有的资源加载完成
                yield return new WaitUntil(() => completedCount >= totalCount);

                Log.Debug("所有UI资源加载完成！", LogModule.UI);
            }
        }

        /// <summary>
        /// 场景初始化
        /// </summary>
        public void Begin()
        {
            Log.Debug("开始场景初始化！", LogModule.UI);
            if (UIViews != null && UIViews.Count > 0) 
            {
                foreach (var View in UIViews)
                {
                    if (View.Key.IsActiveAfterLoaded)
                    {
                        View.Value.DoReveal();
                    }
                }
            }
        }

        /// <summary>
        /// 进度条
        /// </summary>
        public void Progress(float progress)
        {
            
        }

        /// <summary>
        /// 卸载场景前调用
        /// </summary>
        public void Finish()
        {
            Log.Debug("执行Finish", LogModule.UI);
            if (UIViews != null && UIViews.Count > 0)
            {
                foreach (var View in UIViews)
                {
                    if (View.Key.IsActiveAfterLoaded)
                    {
                        View.Value.DoHide();
                    }
                }

                UIViews.Clear();
            }
        }

        /// <summary>
        /// Scene 将自己场景所需的 UI 通过此方法加入到 Scene 当中
        /// </summary>
        /// <param name="info"></param>
        protected void AddUIInfo(UIInfo info)
        {
            if (info != null && UIViews != null) 
            {
                UIViews.Add(info, null);
            }
        }
    }
}