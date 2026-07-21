using UnityEngine;

namespace SatyBT.Samples
{
    /// <summary>
    /// Example: a director system that injects new behaviours into
    /// an NPC's running tree based on external game state. Demonstrates
    /// the NodeInjector pattern.
    ///
    /// Attach to a manager GameObject. Assign the target NPC's
    /// <see cref="BehaviourTreeRunner"/> in the inspector.
    /// </summary>
    public class DifficultyDirector : MonoBehaviour
    {
        [SerializeField] private BehaviourTreeRunner _targetNPC;
        [SerializeField] private int _injectionDurationTicks = 300;

        private bool _hasInjected;

        private void Update()
        {
            if (_targetNPC == null || _targetNPC.Tree == null) return;

            // Example trigger: inject an "enrage" behaviour after 500 ticks
            if (!_hasInjected && _targetNPC.Tree.TickCount >= 500)
            {
                InjectEnrageBehaviour();
                _hasInjected = true;
            }
        }

        private void InjectEnrageBehaviour()
        {
            var tree = _targetNPC.Tree;

            // Build a small subtree to inject
            var enrageSequence = new Sequence(
                new ActionNode(() =>
                {
                    tree.Blackboard.Set("isEnraged", true);
                    Debug.Log("[Director] NPC enraged!");
                    return BTStatus.Success;
                }),
                new ActionNode(() =>
                {
                    Debug.Log("[Director] NPC attacking with fury!");
                    return BTStatus.Running;
                }));

            // Find the root composite and inject at position 0 (highest priority)
            if (tree.Root is CompositeNode rootComposite)
            {
                var handle = tree.Injector.Inject(
                    id: "enrage_phase",
                    node: enrageSequence,
                    target: rootComposite,
                    position: 0,
                    durationTicks: _injectionDurationTicks
                );

                if (handle != null)
                {
                    handle.OnRemoved += h =>
                    {
                        tree.Blackboard.Set("isEnraged", false);
                        Debug.Log("[Director] Enrage expired.");
                        _hasInjected = false;
                    };
                }
            }
        }
    }
}
