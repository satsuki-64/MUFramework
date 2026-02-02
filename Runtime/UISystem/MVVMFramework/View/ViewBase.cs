using System;
using MUFramework.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace MUFramework.UISystem.MVVMFramework
{
    public enum ViewState
    {
        None,
        Revealed,
        Hidden,
    }

    [System.Serializable]
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class ViewBase<T> : MonoBehaviour, IView<T> where T : ViewModelBase, new()
    {
        /// <summary>
        /// 给 View 的具体子类使用，子类可以利用 ViewBase 中的 Binder 绑定器来为属性绑定回调方法
        /// </summary>
        protected readonly PropertyBinder<T> Binder = new PropertyBinder<T>();

        protected ViewState ViewState = ViewState.None;
        
        #region 将 View 和 ViewModel 使用 BindableProperty 进行绑定

        private bool _isInitialized;

        /// <summary>
        /// 为 View 绑定的 ViewModel 也使用 BindableProperty 包裹起来
        /// </summary>
        public readonly BindableProperty<T> ViewModelProperty = new BindableProperty<T>();

        /// <summary>
        /// 获取当前 View 对应的 View Model
        /// </summary>
        public T BindingContext
        {
            get
            {
                return ViewModelProperty.Value;
            }
            set
            {
                if (!_isInitialized)
                {
                    OnInitialize();
                    _isInitialized = true;
                    ViewState = ViewState.Hidden;
                }

                // 触发 OnValueChanged 事件
                // 如果更改当前 View 绑定的 ViewModel，则会导致 
                ViewModelProperty.Value = value;
            }
        }
        
        /// <summary>
        /// 当 View 绑定的上下文（ViewModel）发生改变时，调用 OnBindingContextChanged 将绑定的回调方法重新绑定与解绑
        /// 在 OnInitialize 当中 += 了 OnValuePropertyChanged
        /// </summary>
        public virtual void OnBindingContextChanged(T oldValue, T newValue)
        {
            Binder.Unbind(oldValue);
            Binder.Bind(newValue);
        }

        #endregion

        #region View 的若干可配置属性
        /// <summary>
        /// 是否在隐藏时删除
        /// </summary>
        public bool destroyOnHide;

        /// <summary>
        /// View 显示完成之后，执行的回调函数
        /// </summary>
        private event Action RevealedAction;

        public void AddReveal(Action action)
        {
            if (action != null)
            {
                RevealedAction += action;
            }
        }

        /// <summary>
        /// View 隐藏完成之后，执行的回调函数
        /// </summary>
        private event Action HiddenAction;

        public void AddHidden(Action action)
        {
            if (action != null)
            {
                HiddenAction += action;
            }
        }

        public bool immediate = true;

        #endregion
        
        #region View 的生命周期函数
        private void Awake()
        {
            // 当前 View 被加载后，为其实例化对应的 ViewModel
            BindingContext = new T();
            ViewComponent viewComponent = gameObject.AddComponent<ViewComponent>();
            viewComponent.AddReveal(Reveal);
            viewComponent.AddHide(Hide);
        }
        
        /// <summary>
        /// View 初始化，当执行 Awake 之后被执行
        /// </summary>
        protected virtual void OnInitialize()
        {
            ViewModelProperty.OnValueChanged += OnBindingContextChanged;
        }

        /// <summary>
        /// 对 View 进行显示
        /// 按照顺序，依次执行 OnAppear、OnReveal、OnRevealed
        /// </summary>
        /// <param name="immediate"> 是否立即显示 </param>
        /// <param name="action"> 在完全显示之后，在 OnRevealed 中执行的方法 </param>
        public void Reveal()
        {
            if (ViewState == ViewState.Hidden)
            {
                Log.Debug($"{this.gameObject.name}：Reveal!", LogModule.UI);
                OnAppear();
                OnReveal(immediate);
                OnRevealed();   
            }
        }

        /// <summary>
        /// 按照顺序，依次执行 OnHide、OnHidden、OnDisappear
        /// 对 View 进行隐藏
        /// </summary>
        /// <param name="immediate"></param>
        /// <param name="action"></param>
        public void Hide()
        {
            if (ViewState == ViewState.Revealed)
            {
                Log.Debug($"{this.gameObject.name}：Hide!", LogModule.UI);
                OnHide(immediate);
                OnHidden();
                OnDisappear();    
            }
        }

        /// <summary>
        /// 激活当前 UI 对象，并调用 VM 中的 OnStartReveal 方法
        /// </summary>
        public virtual void OnAppear()
        {
            gameObject.SetActive(true);
            BindingContext.OnStartReveal();
        }

        /// <summary>
        /// 显示当前 UI 对象
        /// </summary>
        /// <param name="immediate"></param>
        private void OnReveal(bool immediate)
        {
            if (immediate)
            {
                //立即显示
                transform.localScale = Vector3.one;
                GetComponent<CanvasGroup>().alpha = 1;
            }
            else
            {
                StartAnimatedReveal();
            }

            GetComponent<CanvasGroup>().blocksRaycasts = true;
        }

        /// <summary>
        /// 显示完成之后，并且执行某些回调方法
        /// </summary>
        public virtual void OnRevealed()
        {
            BindingContext.OnFinishReveal();

            // 回掉函数
            if (RevealedAction != null)
            {
                RevealedAction();
            }

            ViewState = ViewState.Revealed;
        }

        /// <summary>
        /// 将当前对象隐藏
        /// </summary>
        /// <param name="immediate">UI 是否立即隐藏，若为 false 则使用动画隐藏</param>
        private void OnHide(bool immediate)
        {
            BindingContext.OnStartHide();
            if (immediate)
            {
                //立即隐藏
                transform.localScale = Vector3.zero;
                GetComponent<CanvasGroup>().alpha = 0;
            }
            else
            {
                StartAnimatedHide();
            }

            GetComponent<CanvasGroup>().blocksRaycasts = false;
        }

        /// <summary>
        /// alpha 1->0时
        /// </summary>
        public virtual void OnHidden()
        {
            //回掉函数
            if (HiddenAction != null)
            {
                HiddenAction();
            }
        }

        /// <summary>
        /// 取消激活当前 UI 对象，并调用 VM 中的 OnFinishHide 方法
        /// </summary>
        public virtual void OnDisappear()
        {
            gameObject.SetActive(false);
            BindingContext.OnFinishHide();

            ViewState = ViewState.Hidden;

            if (destroyOnHide)
            {
                //销毁
                Destroy(this.gameObject);
            }
        }

        /// <summary>
        /// 当 GameObject 将被销毁时，将 BindingContext 销毁，并释放 OnValueChanged
        /// </summary>
        public virtual void OnDestroy()
        {
            if (BindingContext.IsRevealed)
            {
                Hide();
            }

            BindingContext.OnDestory();
            BindingContext = null;
            ViewModelProperty.OnValueChanged = null;
        }

        #endregion

        #region View 的显示、隐藏动画

        /// <summary>
        /// scale:1,alpha:1
        /// </summary>
        protected virtual void StartAnimatedReveal()
        {
            var canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// alpha:0,scale:0
        /// </summary>
        protected virtual void StartAnimatedHide()
        {
            var canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.interactable = false;
        }

        #endregion

    }
}