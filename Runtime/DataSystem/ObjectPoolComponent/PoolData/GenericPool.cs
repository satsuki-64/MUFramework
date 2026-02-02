using System.Collections.Generic;
using UnityEngine;

namespace MUFramework.DataSystem
{
    public enum GrowthPattern
    {
        FixedSize,
        LinearGrowth,
        ConstantGrowth
    }

    public class GenericPool<T> where T : class,new()
    {
        /// <summary>
        /// 对象池最大容量
        /// </summary>
        public readonly int MAXCOUNT;
        
        public int ConstantGrowthNumber
        {
            get
            {
                return constantGrowthNumber;
            }
            set
            {
                if (value > 0)
                {
                    constantGrowthNumber = value;
                }
                else
                {
                    constantGrowthNumber = 0;
                    Debug.LogWarning("ConstantGrowthNumber不应小于或等于0");
                }
            }
        }
        private int constantGrowthNumber = 1;
        
        protected Stack<T> Pool;
        protected GrowthPattern GrowthPattern;
        
        public GenericPool(int maxCount,int initialCount = 0,GrowthPattern pattern = GrowthPattern.LinearGrowth)
        {
            Pool = new Stack<T>();
            MAXCOUNT = maxCount;
            this.GrowthPattern = pattern; // 默认线性增长
            
            if (initialCount > maxCount)
            {
                Debug.LogWarning($"对象池的初始容量{initialCount}不应该大于最大容量{maxCount}");
            }
            
            for (int i = 0; i < initialCount && i < maxCount; i++) 
            {
                Push(new T());
            }
        }
        
        /// <summary>
        /// 将实例压入对象池中
        /// </summary>
        /// <param name="obj">待压入实例</param>
        /// <returns>是否压入成功</returns>
        public virtual bool Push(T obj)
        {
            if (Pool.Count < MAXCOUNT)
            {
                Pool.Push(obj);
                return true;
            }
            else
            {
                Debug.LogWarning($"{typeof(T).Name}对象池已达{MAXCOUNT}上限，请释放对象池中内存");
                return false;
            }
        }

        public virtual T Get()
        {
            if (Pool != null && Pool.Count > 0) 
            {
                T tempObj = Pool.Pop();
                return tempObj;
            }
            else
            {
                PoolGrowth();
                return Pool.Pop();
            }
        }

        protected virtual void PoolGrowth()
        {
            switch (GrowthPattern)
            {
                case GrowthPattern.LinearGrowth:
                    Push(new T());
                    break;
                
                case GrowthPattern.FixedSize:
                    
                    break;
                
                case GrowthPattern.ConstantGrowth:
                    for (int i = 0; i < ConstantGrowthNumber; i++) 
                    {
                        Push(new T());
                    }
                    break;
            }
        }

        public virtual bool Release(T obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (Pool.Count >= MAXCOUNT)
            {
                return false;
            }
            else
            {
                Push(obj);
                return true;
            }
        }
    }
}