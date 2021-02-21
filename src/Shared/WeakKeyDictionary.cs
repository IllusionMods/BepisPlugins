using BepInEx;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BepisPlugins
{
    internal class WeakKeyDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> where TValue : class
    {
        private readonly Dictionary<HashedWeakReference, TValue> _items;

        public WeakKeyDictionary() => _items = new Dictionary<HashedWeakReference, TValue>();

        public int Count => _items.Count;

        private volatile bool reaperRunRequested = false;

        public void Set(TKey key, TValue value)
        {
            _items[new HashedWeakReference(key)] = value;
            if (!reaperRunRequested)
            {
                lock (this)
                {
                    if (!reaperRunRequested)
                    {
                        reaperRunRequested = true;
                        ThreadingHelper.Instance.StartCoroutine(PurgeKeys());
                    }
                }
            }
        }

        IEnumerator PurgeKeys()
        {
            yield return new UnityEngine.WaitForSeconds(5);
            reaperRunRequested = false;

            // Naive O(n) key prune
            var deadKeys = _items.Keys.Where(reference => !reference.IsAlive).ToList();
            foreach (var reference in deadKeys)
            {
                _items.Remove(reference);
            }
            
        }

        public TValue Get(TKey key)
        {
            if (_items.TryGetValue(new HashedWeakReference(key), out TValue value))
            {
                return value;
            }

            return null;
        }

        public bool Contains(TKey key) => _items.TryGetValue(new HashedWeakReference(key), out TValue _);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _items
                .Where(x => x.Key.IsAlive)
                .Select(x => new KeyValuePair<TKey, TValue>((TKey)x.Key.Target, x.Value))
                .GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Clear() => _items.Clear();
    }
}
