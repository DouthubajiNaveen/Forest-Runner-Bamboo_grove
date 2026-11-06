using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestRunner
{
    /// <summary>Implemented by pooled components that need to reset when taken from / returned to the pool.</summary>
    public interface IPoolable
    {
        void OnPoolGet();
        void OnPoolRelease();
    }

    /// <summary>Marker placed on every pooled instance so it can find its way back to its pool.</summary>
    public class PooledObject : MonoBehaviour
    {
        public string key;
        public IPoolable poolable;
        public bool inPool;
    }

    /// <summary>
    /// Generic keyed object pool. Obstacles, coins, power-ups, decor pieces and track sections
    /// are all pooled so that no allocations / instantiations happen during a run.
    /// </summary>
    public class ObjectPooler : MonoBehaviour
    {
        public static ObjectPooler Instance { get; private set; }

        readonly Dictionary<string, Stack<GameObject>> pools = new Dictionary<string, Stack<GameObject>>();
        readonly Dictionary<string, Func<GameObject>> factories = new Dictionary<string, Func<GameObject>>();

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Register(string key, Func<GameObject> factory)
        {
            factories[key] = factory;
            if (!pools.ContainsKey(key)) pools[key] = new Stack<GameObject>();
        }

        public bool IsRegistered(string key)
        {
            return factories.ContainsKey(key);
        }

        GameObject Create(string key)
        {
            GameObject go = factories[key]();
            go.name = key;
            PooledObject po = go.GetComponent<PooledObject>();
            if (po == null) po = go.AddComponent<PooledObject>();
            po.key = key;
            po.poolable = go.GetComponent<IPoolable>();
            po.inPool = false;
            return go;
        }

        public void Prewarm(string key, int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject go = Create(key);
                Release(go);
            }
        }

        public GameObject Get(string key, Transform parent)
        {
            Stack<GameObject> stack = pools[key];
            GameObject go = null;
            while (stack.Count > 0 && go == null) go = stack.Pop();
            if (go == null) go = Create(key);

            PooledObject po = go.GetComponent<PooledObject>();
            po.inPool = false;
            go.transform.SetParent(parent, false);
            go.SetActive(true);
            if (po.poolable != null) po.poolable.OnPoolGet();
            return go;
        }

        public void Release(GameObject go)
        {
            if (go == null) return;
            PooledObject po = go.GetComponent<PooledObject>();
            if (po == null || po.inPool) return;
            po.inPool = true;
            if (po.poolable != null) po.poolable.OnPoolRelease();
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            pools[po.key].Push(go);
        }
    }
}
