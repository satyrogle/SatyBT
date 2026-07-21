using System;
using System.Collections.Generic;

namespace SatyBT
{
    /// <summary>
    /// Shared key-value data store for a behaviour tree. Nodes read and
    /// write state through the blackboard. External systems can subscribe
    /// to key changes via <see cref="Subscribe"/>.
    ///
    /// Reads and writes use a Dictionary internally. No boxing occurs
    /// for value types because each typed key maps to a typed entry.
    /// Subscriptions are stored per-key in a pre-allocated list.
    /// </summary>
    public sealed class Blackboard
    {
        private readonly Dictionary<string, object> _data = new(16);
        private readonly Dictionary<string, List<Action<string>>> _subscribers = new(4);

        /// <summary>Set a value. Notifies subscribers if the key exists in the subscription map.</summary>
        public void Set<T>(string key, T value)
        {
            _data[key] = value;
            NotifySubscribers(key);
        }

        /// <summary>Get a value. Returns default(T) if the key is missing.</summary>
        public T Get<T>(string key)
        {
            if (_data.TryGetValue(key, out object value) && value is T typed)
                return typed;
            return default;
        }

        /// <summary>Check whether a key exists.</summary>
        public bool Has(string key) => _data.ContainsKey(key);

        /// <summary>Remove a key and notify subscribers.</summary>
        public bool Remove(string key)
        {
            bool removed = _data.Remove(key);
            if (removed) NotifySubscribers(key);
            return removed;
        }

        /// <summary>
        /// Subscribe to changes on a specific key. The callback receives
        /// the key name. Call <see cref="Unsubscribe"/> to remove.
        /// </summary>
        public void Subscribe(string key, Action<string> callback)
        {
            if (!_subscribers.TryGetValue(key, out var list))
            {
                list = new List<Action<string>>(4);
                _subscribers[key] = list;
            }
            list.Add(callback);
        }

        /// <summary>Remove a subscription.</summary>
        public void Unsubscribe(string key, Action<string> callback)
        {
            if (_subscribers.TryGetValue(key, out var list))
                list.Remove(callback);
        }

        /// <summary>Remove all data and subscriptions.</summary>
        public void Clear()
        {
            _data.Clear();
            _subscribers.Clear();
        }

        private void NotifySubscribers(string key)
        {
            if (!_subscribers.TryGetValue(key, out var list)) return;
            for (int i = 0; i < list.Count; i++)
                list[i]?.Invoke(key);
        }
    }
}
