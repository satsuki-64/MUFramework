using System;
using UnityEngine;

namespace MUFramework.DataSystem
{
    public class AutoRecycle : MonoBehaviour
    {
        [HideInInspector]
        public string PoolName;
        [HideInInspector]
        public float MaxTime = 10f;
        private float time;
        
        private void Update()
        {
            time += Time.deltaTime;
            if (time > MaxTime)
            {
                ObjectPoolComponent.Instance.ReleaseGameObject(PoolName,this.gameObject);
            }
        }

        public void Reset()
        {
            this.time = 0f;
        }
    }
}