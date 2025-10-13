using System.Collections.Generic;
using UnityEngine;

namespace SpatialUI
{
    public class FloatingTextPool : MonoBehaviour
    {
        public FloatingText prefab;
        public int prewarm = 12;
        private readonly Queue<FloatingText> _pool = new Queue<FloatingText>();

        public void Init()
        {
            for (int i = 0; i < prewarm; i++)
                _pool.Enqueue(Create());
        }

        FloatingText Create()
        {
            var go = Instantiate(prefab.gameObject, transform);
            go.SetActive(false);
            return go.GetComponent<FloatingText>();
        }

        public FloatingText Get()
        {
            if (_pool.Count == 0) _pool.Enqueue(Create());
            var ft = _pool.Dequeue();
            ft.gameObject.SetActive(true);
            return ft;
        }

        public void Release(FloatingText ft)
        {
            ft.gameObject.SetActive(false);
            _pool.Enqueue(ft);
        }
    }
}
