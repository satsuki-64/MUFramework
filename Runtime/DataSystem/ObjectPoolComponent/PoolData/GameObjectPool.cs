using UnityEngine;

namespace MUFramework.DataSystem
{
    public sealed class GameObjectPool : GenericPool<GameObject>
    {
        private string poolName;
        private GameObject root;
        private GameObject prefab;
        
        private bool isAutoRecycle;
        private float autoRecycleTime;
        
        public GameObjectPool(int maxCount,GameObject root,GameObject prefab,string poolName,int initialCount = 0,
            GrowthPattern pattern = GrowthPattern.LinearGrowth,bool isAutoRecycle = false,float recycleTime = 10f)
            : base(maxCount, 0, pattern)
        {
            this.root = root;
            this.prefab = prefab;
            this.poolName = poolName;
            this.isAutoRecycle = isAutoRecycle;
            this.autoRecycleTime = recycleTime;
            
            for (int i = 0; i < initialCount && i < maxCount; i++)
            {
                GameObject go = GameObject.Instantiate(prefab);
                go.SetActive(false);
                go.transform.SetParent(this.root.transform);
                
                Pool.Push(go);

                if (isAutoRecycle)
                {
                    AutoRecycle temp = go.AddComponent<AutoRecycle>();
                    temp.PoolName = poolName;
                    temp.MaxTime = recycleTime;
                }
            }
        }

        public override GameObject Get()
        {
            if (Pool.Count < 0)
            {
                PoolGrowth();
            }

            GameObject obj = base.Get();
            obj.SetActive(true);
            obj.GetComponent<AutoRecycle>().Reset();
            
            if (obj.transform.parent != root.transform)
            {
                obj.transform.SetParent(root.transform);
            }

            return obj;
        }
        
        public override bool Push(GameObject obj)
        {
            obj.transform.SetParent(root.transform);
            
            if (base.Push(obj))
            {
                return true;
            }
            else
            {
                GameObject.Destroy(obj);
                return false;
            }
        }

        public override bool Release(GameObject obj)
        {
            if (base.Release(obj))
            {
                obj.SetActive(false);
                return true;
            }
            else
            {
                if (obj != null)
                {
                    GameObject.Destroy(obj);    
                }
                
                return false;
            }
        }
        
        protected override void PoolGrowth()
        {
            switch (GrowthPattern)
            {
                case GrowthPattern.LinearGrowth:
                    GameObject go1 = GameObject.Instantiate(prefab);
                    Push(go1);
                    if (isAutoRecycle)
                    {
                        AutoRecycle temp = go1.AddComponent<AutoRecycle>();
                        temp.PoolName = poolName;
                        temp.MaxTime = autoRecycleTime;
                    }
                    break;
                
                case GrowthPattern.FixedSize:
                    
                    break;
                
                case GrowthPattern.ConstantGrowth:
                    for (int i = 0; i < ConstantGrowthNumber; i++) 
                    {
                        GameObject go2 = GameObject.Instantiate(prefab);
                        Push(go2);
                        if (isAutoRecycle)
                        {
                            AutoRecycle temp = go2.AddComponent<AutoRecycle>();
                            temp.PoolName = poolName;
                            temp.MaxTime = autoRecycleTime;
                        }
                    }
                    break;
            }
        }
    }
}