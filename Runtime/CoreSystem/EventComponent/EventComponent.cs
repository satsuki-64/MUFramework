using System.Collections.Generic;
using MUFramework.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace MUFramework.CoreSystem
{
    public class EventComponent : SingletonBase<EventComponent>
    {
        /// <summary>
        /// Key 对应事件的名字
        /// Value 对应监听这个事件的对应的委托函数们
        /// </summary>
        private Dictionary<string,IEventInfo> eventDictionary = new Dictionary<string,IEventInfo>();

        /// <summary> 
        /// 添加事件监听
        /// </summary>
        /// <param name="name">事件的名字</param>
        /// <param name="action">准备用来处理事件的委托函数</param>
        public void AddEventListener<T>(string name, UnityAction<T> action)
        {
            if (string.IsNullOrEmpty(name) || action == null)
            {
                Debug.LogWarning("事件名或事件不能为空.");
                return;
            }

            if (eventDictionary.ContainsKey(name))
            {
                (eventDictionary[name] as EventInfo<T>).action += action;
                eventDictionary[name].EventCount++;
            }
            else
            {
                eventDictionary.Add(name, new EventInfo<T>(action));
            }
        }
        
        /// <summary>
        /// 非泛型版本，添加事件监听
        /// </summary>
        /// <param name="name"></param>
        /// <param name="action"></param>
        public void AddEventListener(string name,UnityAction action)
        {
            if (string.IsNullOrEmpty(name) || action == null)
            {
                Debug.LogWarning("事件名或事件不能为空.");
                return;
            }
            
            if (eventDictionary.ContainsKey(name))
            {
                (eventDictionary[name] as EventInfo).action += action;
                eventDictionary[name].EventCount++;
            }
            else
            {
                eventDictionary.Add(name, new EventInfo(action));
            }
        }

        /// <summary>
        /// 移除对应的事件监听
        /// </summary>
        /// <param name="name">事件的名字</param>
        /// <param name="action">移除的事件</param>
        public void RemoveEventListener<T>(string name,UnityAction<T> action)
        {
            if (string.IsNullOrEmpty(name) || action == null)
            {
                Debug.LogWarning("事件名或事件不能为空.");
                return;
            }
            
            if (eventDictionary.ContainsKey(name))
            {
                (eventDictionary[name] as EventInfo<T>).action -= action;
                eventDictionary[name].EventCount--;

                if (eventDictionary[name].EventCount == 0) 
                {
                    eventDictionary.Remove(name);
                }
            }
        }
        
        /// <summary>
        /// 非泛型版本，移除事件监听
        /// </summary>
        /// <param name="name"></param>
        /// <param name="action"></param>
        public void RemoveEventListener(string name,UnityAction action)
        {
            if (string.IsNullOrEmpty(name) || action == null)
            {
                Debug.LogWarning("事件名或事件不能为空.");
                return;
            }

            if (eventDictionary.ContainsKey(name))
            {
                (eventDictionary[name] as EventInfo).action -= action;
                eventDictionary[name].EventCount--;
                
                if (eventDictionary[name].EventCount == 0) 
                {
                    eventDictionary.Remove(name);
                }
            }
        }

        /// <summary>
        /// 泛型版本执行
        /// </summary>
        /// <param name="name">要执行的事件别名</param>
        /// <param name="info"></param>
        /// <typeparam name="T">指定触发事件的名称</typeparam>
        /// <returns>执行的回调数量</returns>
        public int EventTrigger<T>(string name,T info)
        {
            if (eventDictionary.ContainsKey(name))
            {
                (eventDictionary[name] as EventInfo<T>).action?.Invoke(info);
                return eventDictionary[name].EventCount;
            }
            else
            {
                Debug.LogWarning($"EventComponent中不存在{name}事件");
                return 0; 
            }
        }
        
        /// <summary>
        /// 非泛型版本执行
        /// </summary>
        /// <param name="name">要执行的事件别名</param>
        /// <returns>执行的回调数量</returns>
        public int EventTrigger(string name)
        {
            if (eventDictionary.ContainsKey(name))
            {
                (eventDictionary[name] as EventInfo).action?.Invoke();
                return eventDictionary[name].EventCount;
            }
            else
            {
                Debug.LogWarning($"EventComponent中不存在{name}事件");
                return 0;                
            }
        }

        /// <summary>
        /// 清空事件中心，主要用于场景切换时
        /// </summary>
        public void Clear()
        {
            eventDictionary.Clear();
        }
    }
}