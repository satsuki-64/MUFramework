using UnityEngine.Events;

namespace MUFramework.CoreSystem
{
    /// <summary>
    /// 用于兼容一个字典同时存储非泛型和泛型版本的事件
    /// </summary>
    public interface IEventInfo
    {
        public int EventCount { get; set; }
    }
    
    public class EventInfo<T> : IEventInfo
    {
        public UnityAction<T> action;
        public int EventCount { get; set;}

        public EventInfo(UnityAction<T> action)
        {
            this.action += action;
        }
    }
    
    public class EventInfo : IEventInfo
    {
        public UnityAction action;
        public int EventCount { get; set;}

        public EventInfo(UnityAction action)
        {
            this.action += action;
        }
    }
}