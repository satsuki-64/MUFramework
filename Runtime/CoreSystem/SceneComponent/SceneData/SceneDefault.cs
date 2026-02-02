using MUFramework.DataSystem;
using MUFramework.UISystem;
using MUFramework.UISystem.MVVMFramework;
using UnityEngine;
using UnityEngine.UI;
using Slider = UnityEngine.UIElements.Slider;

namespace MUFramework.CoreSystem
{
    public class SceneDefault : SceneBase
    {
        public SceneDefault(string sceneName) : base(sceneName)
        {
            // AddUIInfo(new UIInfo("TestUI_1", AssetLoadType.AB, "PC/AB", true));
            // AddUIInfo(new UIInfo("TestUI_2", AssetLoadType.AB, "PC/AB", false));
            AddUIInfo(new UIInfo("Test1", AssetLoadType.Resources, "UI/Test1", true));
            AddUIInfo(new UIInfo("Test2", AssetLoadType.Resources, "UI/Test2", true));
        }
        
        public void Load()
        {
            Debug.Log($"加载{SceneName}本地资源中");
        }

        public void Begin()
        {
            Debug.Log($"{SceneName}场景初始化中");
        }

        public void Progress(float progress)
        {
            // 限制进度在0-1之间
            progress = Mathf.Clamp01(progress);
        }

        public void Finish()
        {
            Debug.Log($"{SceneName}场景卸载");
        }
    }
}