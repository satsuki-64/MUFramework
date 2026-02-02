using UnityEngine;
using UnityEngine.Events;

namespace MUFramework.Utilities
{
    public class SingletonMonoMgr : SingletonMonoAuto<SingletonMonoMgr>
    {
        private event UnityAction _updateEvent;
        private event UnityAction _fixedUpdateEvent;
        
        private void Update()
        {
            _updateEvent?.Invoke();
        }

        private void FixedUpdate()
        {
            _fixedUpdateEvent?.Invoke();
        }

        /// <summary>
        /// 插入 Update 中执行
        /// </summary>
        /// <param name="listener"></param>
        public void AddUpdateListener(UnityAction listener)
        {
            if (listener!=null)
            {
                _updateEvent += listener;   
            }
        }

        public void RemoveUpdateListener(UnityAction listener)
        {
            if (listener!=null)
            {
                _updateEvent -= listener;   
            }
        }

        public void ClearUpdateListener()
        {
            _updateEvent = null;
        }

        /// <summary>
        /// 插入 FixedUpdate 中执行
        /// </summary>
        /// <param name="listener"></param>
        public void AddFixedUpdateListener(UnityAction listener)
        {
            if (listener != null)
            {
                _fixedUpdateEvent += listener;
            }
        }

        public void RemoveFixedUpdateListener(UnityAction listener)
        {
            if (listener != null)
            {
                _fixedUpdateEvent -= listener;
            }
        }

        public void ClearFixedUpdateListener()
        {
            _fixedUpdateEvent = null;
        }

        public void ClearAllListerners()
        {
            ClearUpdateListener();
            ClearFixedUpdateListener();
        }
    }
}