using System.Collections.Generic;

namespace SatyBT
{
    /// <summary>
    /// Allows external systems to insert or remove nodes from a running
    /// behaviour tree at runtime. Injected nodes can auto-expire after
    /// a specified number of ticks.
    ///
    /// This was generalised from the Red Tape Engine / StateInjector
    /// pattern in Desk 42, where external game systems modify an agent's
    /// decision tree mid-execution (e.g. a difficulty director adding
    /// new behaviours to an NPC).
    /// </summary>
    public sealed class NodeInjector
    {
        private readonly BehaviourTree _tree;
        private readonly Dictionary<string, InjectionHandle> _active = new(8);
        private readonly List<string> _expiredKeys = new(4); // reused each frame

        internal NodeInjector(BehaviourTree tree)
        {
            _tree = tree;
        }

        /// <summary>
        /// Inject a node into a target composite at the given position.
        /// </summary>
        /// <param name="id">Unique identifier. Duplicate IDs are rejected.</param>
        /// <param name="node">The node (or subtree root) to inject.</param>
        /// <param name="target">The composite node to inject into.</param>
        /// <param name="position">Index in the composite's child list.</param>
        /// <param name="durationTicks">
        /// Number of ticks before auto-removal. 0 or negative = permanent
        /// (must be removed manually).
        /// </param>
        /// <returns>The handle, or null if the ID is already active.</returns>
        public InjectionHandle Inject(string id, NodeBase node, CompositeNode target,
            int position, int durationTicks = 0)
        {
            if (_active.ContainsKey(id))
                return null;

            int expiresAt = durationTicks > 0
                ? _tree.TickCount + durationTicks
                : -1;

            var handle = new InjectionHandle(id, node, target, expiresAt);

            // Propagate the tree's blackboard to the injected node
            _tree.PropagateBlackboard(node);

            target.Insert(position, node);
            _active[id] = handle;

            handle.RaiseInjected();
            return handle;
        }

        /// <summary>Manually remove an injection by ID.</summary>
        /// <returns>True if found and removed.</returns>
        public bool Remove(string id)
        {
            if (!_active.TryGetValue(id, out var handle))
                return false;

            handle.Target.Remove(handle.Node);
            handle.Node.Reset();
            _active.Remove(id);

            handle.RaiseRemoved();
            return true;
        }

        /// <summary>Check whether an injection with this ID is active.</summary>
        public bool IsActive(string id) => _active.ContainsKey(id);

        /// <summary>Number of currently active injections.</summary>
        public int ActiveCount => _active.Count;

        /// <summary>
        /// Called by the tree each tick to expire timed injections.
        /// Uses a pre-allocated list to avoid GC.
        /// </summary>
        internal void ProcessExpirations(int currentTick)
        {
            _expiredKeys.Clear();

            foreach (var kvp in _active)
            {
                if (kvp.Value.ExpiresAtTick > 0 && currentTick >= kvp.Value.ExpiresAtTick)
                    _expiredKeys.Add(kvp.Key);
            }

            for (int i = 0; i < _expiredKeys.Count; i++)
                Remove(_expiredKeys[i]);
        }
    }
}
