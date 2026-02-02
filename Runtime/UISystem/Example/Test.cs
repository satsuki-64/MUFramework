using MUFramework.CoreSystem;
using MUFramework.DataSystem;
using MUFramework.UISystem.Example;
using MUFramework.Utilities;
using UnityEngine;

namespace MUFramework.UISystem.Example
{
    public class Test : MonoBehaviour
    {
        public GameObject prefab;

        public TestView TestView;
        
        private void Start()
        {
            AssetLoader.Instance.Init();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SceneDefault scene = new SceneDefault("TestScene");
                SceneComponent.Instance.LoadSceneAsync(scene, () =>
                {
                    Debug.Log("加载场景结束！");
                });
            }
        }
    }
}