using System;

namespace MUFramework.UISystem.MVVMFramework
{
    /// <summary>
    /// 统一的视图接口，要求具体的 View 必须绑定上下文 ViewModel
    /// Reveal 和 Hide 方法表示 View 显示或者隐藏显示的方法
    /// </summary>
    /// <typeparam name="T">当前绑定的 ViewModel 的具体类型</typeparam>
    public interface IView<T> where T : ViewModelBase
    {
        T BindingContext { get; set; }
        void Reveal();
        void Hide();
    }
}