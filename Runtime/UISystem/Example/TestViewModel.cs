using MUFramework.CoreSystem;
using MUFramework.UISystem.MVVMFramework;
using MUFramework.Utilities;
using UnityEngine;

namespace MUFramework.UISystem.Example
{
    public class TestViewModel : ViewModelBase
    {
        public BindableProperty<string> Text = new BindableProperty<string>();
        public BindableProperty<bool> ButtonBool = new BindableProperty<bool>();
        
        public TestViewModel()
        {
            EventComponent.Instance.AddEventListener("TestCallBack",() =>
            {
                Text.Value += $"{Random.Range(0, 1000)}";
            });
        }

        public void ButtonOnClick()
        {
            Debug.Log("ButtonOnClick");
            Text.Value = $"{Random.Range(0, 1000)}+测试的文字！";
        }
    }
}