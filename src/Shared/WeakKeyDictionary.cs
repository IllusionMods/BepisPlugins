using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BepisPlugins
{
    internal class WeakKeyDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> where TValue : class
    {
        private readonly Dictionary<int, List<KeyValuePair<WeakReference, TValue>>> _items;

        public WeakKeyDictionary() => _items = new Dictionary<int, List<KeyValuePair<WeakReference, TValue>>> ();

        public int Count => DoCount();

        private int DoCount()
        {
            int count = 0;
            foreach (List<KeyValuePair<WeakReference, TValue>> valueList in _items.Values)
            {
                count += valueList.Count;
            }
            return count;
        }

        public void Set(TKey key, TValue value)
        {
            int newReferenceHash = RuntimeHelpers.GetHashCode(key);
            lock (_items)
            {
                if (_items.TryGetValue(newReferenceHash, out List<KeyValuePair<WeakReference, TValue>> references))
                {
                    bool found = false;
                    for (int i = 0; i < references.Count; i++)
                    {
                        KeyValuePair<WeakReference, TValue> reference = references[i];
                        if (ReferenceEquals(reference.Value, value))
                        {
                            references[i] = new KeyValuePair<WeakReference, TValue>(new WeakReference(key), value);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        references.Add(new KeyValuePair<WeakReference, TValue>(new WeakReference(key), value));
                    }
                }
                else
                {
                    List<KeyValuePair<WeakReference, TValue>> newReferences = new List<KeyValuePair<WeakReference, TValue>>();
                    newReferences.Add(new KeyValuePair<WeakReference, TValue>(new WeakReference(key), value));
                    _items[newReferenceHash] = newReferences;
                }
                PurgeDeadKeys();
            }
        }

        private void PurgeDeadKeys()
        {
            List<int> deadLists = new List<int>();
            foreach (int key in _items.Keys)
            {
                List<KeyValuePair<WeakReference, TValue>> deadKeys = _items[key].Where(kvp => !kvp.Key.IsAlive).ToList();
                foreach (KeyValuePair<WeakReference, TValue> deadKey in deadKeys)
                {
                    _items[key].Remove(deadKey);
                }
                if (_items[key].Count == 0)
                {
                    deadLists.Add(key);
                }
            }
            foreach (int deadKey in deadLists)
            {
                _items.Remove(deadKey);
            }
        }

        public TValue Get(TKey key)
        {
            int lookupRef = RuntimeHelpers.GetHashCode(key);
            if (_items.TryGetValue(lookupRef, out List<KeyValuePair<WeakReference, TValue>> valueList))
            {
                TValue value = null;
                foreach (KeyValuePair<WeakReference, TValue> kvp in valueList)
                {
                    if (kvp.Key.IsAlive && ReferenceEquals(kvp.Key.Target, key))
                    {
                        value = kvp.Value;
                    }
                }
                if (value != null)
                {
                    return value;
                }
            }
            return null;
        }

        public bool Contains(TKey key) => _items.TryGetValue(RuntimeHelpers.GetHashCode(key), out List<KeyValuePair<WeakReference, TValue>> list);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            List<KeyValuePair<TKey, TValue>> joinSet = new List<KeyValuePair<TKey, TValue>>();

            foreach (int key in _items.Keys)
            {
                foreach (KeyValuePair<WeakReference, TValue> kvp in _items[key])
                {
                    if (kvp.Key.IsAlive)
                        joinSet.Add(new KeyValuePair<TKey, TValue>((TKey)kvp.Key.Target, kvp.Value));
                }
            }
            return joinSet.GetEnumerator();

        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Clear() => _items.Clear();
    }
}
