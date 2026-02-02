using System;

namespace MUFramework.Utilities
{
    public class SingletonBase<T> where T : new()
    {
        private static T instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new T();
                }
                
                return instance;
            }
        }
    }
}