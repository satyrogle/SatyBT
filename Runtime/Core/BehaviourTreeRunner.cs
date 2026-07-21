using UnityEngine;

namespace SatyBT
{
    /// <summary>
    /// MonoBehaviour that ticks a <see cref="BehaviourTree"/> each frame.
    /// Attach to any GameObject that needs AI decision-making.
    /// </summary>
    [DisallowMultipleComponent]
    public class BehaviourTreeRunner : MonoBehaviour
    {
        /// <summary>The tree this runner is driving.</summary>
        public BehaviourTree Tree { get; private set; }

        /// <summary>Status returned by the most recent tick.</summary>
        public BTStatus LastStatus { get; private set; }

        /// <summary>
        /// Initialise the runner with a constructed tree.
        /// Call this from your setup code (Awake, Start, or a factory).
        /// </summary>
        public void Initialise(BehaviourTree tree)
        {
            Tree = tree;
        }

        private void Update()
        {
            if (Tree == null) return;
            LastStatus = Tree.Tick();
        }
    }
}
