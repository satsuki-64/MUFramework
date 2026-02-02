using System.Collections;
using MUFramework.Utilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace MUFramework.CoreSystem
{
    public class SceneComponent : SingletonBase<SceneComponent>
    {
        private SceneBase currentScene;
        
        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="scene">场景名</param>
        /// <param name="function">加载完成后执行的回调</param>
        public void LoadSceneAsync(SceneBase scene, UnityAction function)
        {
            if (scene == null || string.IsNullOrEmpty(scene.SceneName))
            {
                Debug.LogWarning($"加载场景失败！请检查场景名称是否正确：{scene.SceneName}");
                return;
            }
            
            if (currentScene != null)
            {
                currentScene.Finish();
            }
            
            SingletonMonoMgr.Instance.StartCoroutine(ReallyLoadSceneAsync(scene, function));
        }
        
        private IEnumerator ReallyLoadSceneAsync(SceneBase scene, UnityAction function)
        {
            // 1. 加载场景
            // asyncOperation.progress 表示当前加载的进度
            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(scene.SceneName);
            
            // 如果当前正在加载
            while (asyncOperation.isDone == false)
            {
                // 更新进度
                scene.Progress(asyncOperation.progress);

                yield return null;
            }

            yield return asyncOperation;
            
            // 2. 加载本地资源
            yield return scene.Load();
            
            // 3. 场景初始化
            scene.Begin();
            
            currentScene = scene;
            
            // 4. 加载完成之后，再去执行回调的方法
            function?.Invoke();
        }
    }
}