using System;

namespace SatyBT
{
    /// <summary>
    /// Tracks a single injected node inside a composite. Created by
    /// <see cref="NodeInjector.Inject"/> and used to manage expiration
    /// and removal.
    /// </summary>
    public sealed class InjectionHandle
    {
        /// <summary>Unique identifier for this injection.</summary>
        public string Id { get; }

        /// <summary>The node that was injected.</summary>
        public NodeBase Node { get; }

        /// <summary>The composite the node was injected into.</summary>
        public CompositeNode Target { get; }

        /// <summary>
        /// The tree tick count at which this injection expires.
        /// -1 means no automatic expiration.
        /// </summary>
        public int ExpiresAtTick { get; }

        /// <summary>Fired when the injection is applied.</summary>
        public event Action<InjectionHandle> OnInjected;

        /// <summary>Fired when the injection is removed (manual or expiry).</summary>
        public event Action<InjectionHandle> OnRemoved;

        internal InjectionHandle(string id, NodeBase node, CompositeNode target, int expiresAtTick)
        {
            Id = id;
            Node = node;
            Target = target;
            ExpiresAtTick = expiresAtTick;
        }

        internal void RaiseInjected() => OnInjected?.Invoke(this);
        internal void RaiseRemoved() => OnRemoved?.Invoke(this);
    }
}
