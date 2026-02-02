using System;
using System.Collections.Generic;
using System.Reflection;

namespace MUFramework.UISystem.MVVMFramework
{
    public class PropertyBinder<T> where T:ViewModelBase
    {
        private delegate void BindHandler(T viewmodel);
        private delegate void UnBindHandler(T viewmodel);

        /// <summary>
        /// _binders 存储的委托由 Add 方法添加
        /// _binders 存储的委托的执行由 Bind 方法执行
        /// </summary>
        private readonly List<BindHandler> _binders = new List<BindHandler>();
        private readonly List<UnBindHandler> _unbinders = new List<UnBindHandler>();
        
        /// <summary>
        /// 将变量和指定的回调进行绑定
        /// </summary>
        /// <param name="name">具体 ViewModel 中的属性名</param>
        /// <param name="valueChangedHandler">属性待绑定的回调</param>
        /// <typeparam name="TProperty">待绑定变量的类型</typeparam>
        public void Add<TProperty>(string name,BindableProperty<TProperty>.ValueChangedHandler valueChangedHandler )
        {
            // fieldInfo 是一个 FieldInfo 类型，存储了一个指定了泛型类型的 BindableProperty<T> 类型
            // 因为 BindableProperty 存储在具体 ViewModel 当中，因此利用反射，在具体 ViewModel 中获取指定变量名的变量
            var fieldInfo = typeof(T).GetField(name, BindingFlags.Instance | BindingFlags.Public);
            
            // 如果当前 T 对应的 ViewModel 当中没有指定的属性
            if (fieldInfo == null)
            {
                throw new Exception(string.Format("Unable to find bindableproperty field '{0}.{1}'", typeof(T).Name, name));
            }

            // 在 _binders 链表当中添加一个委托
            // viewmodel 是一个形式参数, 它声明了这个Lambda表达式需要接收一个类型为 T（即 ViewModelBase）的参数
            // 当 Binder 方法当中执行委托时，为其传入具体的 ViewModel 参数
            _binders.Add(viewmodel =>
            {
                GetBindablePropertyFromViewModel<TProperty>(name, viewmodel, fieldInfo).OnValueChanged += valueChangedHandler;
            });

            _unbinders.Add(viewModel =>
            {
                GetBindablePropertyFromViewModel<TProperty>(name, viewModel, fieldInfo).OnValueChanged -= valueChangedHandler;
            });
        }

        /// <summary>
        /// 找到指定变量变量的 BindableProperty 属性，进而可以获得其 OnValueChanged 变量
        /// </summary>
        /// <param name="name">变量名</param>
        /// <param name="viewModel">具体 ViewModel 类型的实例</param>
        /// <param name="fieldInfo">存储了指定了 BindableProperty 泛型类型的FieldInfo</param>
        /// <typeparam name="TProperty"></typeparam>
        /// <returns>变量对应的BindableProperty</returns>
        private BindableProperty<TProperty> GetBindablePropertyFromViewModel<TProperty>(string name, T viewModel,FieldInfo fieldInfo)
        {
            // 从 viewModelName 对象实例中，获取由 fieldInfo 所代表的那个字段的当前值
            // 使用当前传入的具体 ViewModel 实例，获取用于绑定具体 ViewModel 的 BindableProperty 属性
            var value = fieldInfo.GetValue(viewModel);
            
            BindableProperty<TProperty> bindableProperty = value as BindableProperty<TProperty>;
            if (bindableProperty == null)
            {
                throw new Exception(string.Format("Illegal bindableproperty field '{0}.{1}' ", typeof(T).Name, name));
            }

            return bindableProperty;
        }

        /// <summary>
        /// 此方法由 ViewBase 在 OnBindingContextChanged 中执行，当 View 执行 OnInitialize 时添加 OnBindingContextChanged 回调
        /// </summary>
        /// <param name="viewmodel"></param>
        public void Bind(T viewmodel)
        {
            if (viewmodel != null)
            {
                for (int i = 0; i < _binders.Count; i++)
                {
                    _binders[i](viewmodel);
                }
            }
        }

        public void Unbind(T viewmodel)
        {
            if (viewmodel!=null)
            {
                for (int i = 0; i < _unbinders.Count; i++)
                {
                    _unbinders[i](viewmodel);
                }
            }
        }

    }
}