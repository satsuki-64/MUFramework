using System;
using MUFramework.CoreSystem;
using MUFramework.UISystem.MVVMFramework;
using UnityEngine;
using UnityEngine.UI;

namespace MUFramework.UISystem.Example
{
    public class TestView : ViewBase<TestViewModel>
    {
        public Text testText;
        public Button testButton;

        /// <summary>
        /// View 需要在 OnInitialize 为属性进行绑定
        /// </summary>
        protected override void OnInitialize()
        {
            base.OnInitialize();
            Binder.Add<string>("Text", TestTextChanged);
            Binder.Add<bool>("ButtonBool", TestButtonStateChanged);
        }

        /// <summary>
        /// 如果是 Unity GameObject 上的回调，需要在 Start 中注册
        /// </summary>
        private void Start()
        {
            testButton.onClick.AddListener(BindingContext.ButtonOnClick);
        }

        public void TestTextChanged(string oldText, string newText)
        {
            testText.text = newText;
        }

        public void TestButtonStateChanged(bool oldButtonState, bool newButtonState)
        {
            testButton.interactable = newButtonState;
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                EventComponent.Instance.EventTrigger("TestCallBack");
            }
        }
    }
}