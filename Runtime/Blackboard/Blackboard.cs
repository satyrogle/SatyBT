using System;
using System.Collections.Generic;

namespace SatyBT
{
    /// <summary>
    /// Shared key-value data store for a behaviour tree. Nodes read and
    /// write state through the blackboard. External systems can subscribe
    /// to key changes via <see cref="Subscribe"/>.
    ///
    /// Reads (<see cref="Get{T}"/> / <see cref="TryGet{T}"/>) do not allocate:
    /// unboxing a value type out of the backing store copies, it does not
    /// allocate. Writing a value type via <see cref="Set{T}"/> boxes it into
    /// the object-typed store, so hot tick paths should prefer reading state
    /// and writing only when it changes. Subscriptions are stored per-key in
    /// a pre-allocated list and dispatched with an index loop, not foreach.
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

        /// <summary>
        /// Get a value. Returns default(T) if the key is missing, or if the
        /// stored value is not assignable to T. In the editor and development
        /// builds a type mismatch (key present but wrong type) logs a warning;
        /// the release tick path is unaffected.
        /// </summary>
        public T Get<T>(string key)
        {
            if (_data.TryGetValue(key, out object value))
            {
                if (value is T typed)
                    return typed;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Debug.LogWarning(
                    $"[SatyBT.Blackboard] Key '{key}' holds " +
                    $"{(value == null ? "null" : value.GetType().Name)} but " +
                    $"{typeof(T).Name} was requested. Returning default({typeof(T).Name}).");
#endif
            }
            return default;
        }

        /// <summary>
        /// Try to get a value. Returns true and sets <paramref name="value"/>
        /// only when the key exists and its stored value is assignable to T.
        /// Never logs; use this when a missing or mistyped key is expected.
        /// </summary>
        public bool TryGet<T>(string key, out T value)
        {
            if (_data.TryGetValue(key, out object stored) && stored is T typed)
            {
                value = typed;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>Check whether a key exists.</summary>
        public bool Has(string key) => _data.ContainsKey(key);

        /// <summary>
        /// Read-only view of the stored entries, for tooling such as the
        /// editor debugger. Do not enumerate this on the tick path.
        /// </summary>
        public IReadOnlyDictionary<string, object> Entries => _data;

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
