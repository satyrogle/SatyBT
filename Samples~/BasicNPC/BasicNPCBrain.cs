using UnityEngine;

namespace SatyBT.Samples
{
    /// <summary>
    /// Example: a basic NPC with patrol → chase → attack behaviour.
    /// Attach to a GameObject and call Play to start the tree.
    /// </summary>
    public class BasicNPCBrain : MonoBehaviour
    {
        [SerializeField] private float _chaseRange = 10f;
        [SerializeField] private float _attackRange = 2f;

        private BehaviourTreeRunner _runner;

        private void Start()
        {
            _runner = gameObject.AddComponent<BehaviourTreeRunner>();

            var root = new Selector()
                .AddChild(BuildAttackBranch())
                .AddChild(BuildChaseBranch())
                .AddChild(BuildPatrolBranch());

            var tree = new BehaviourTree((NodeBase)root);

            // Seed some initial blackboard values
            tree.Blackboard.Set("hasTarget", false);
            tree.Blackboard.Set("targetDistance", float.MaxValue);

            _runner.Initialise(tree);
        }

        private NodeBase BuildAttackBranch()
        {
            return (NodeBase)new Sequence()
                .AddChild(new Condition(() =>
                    _runner.Tree.Blackboard.Get<float>("targetDistance") <= _attackRange))
                .AddChild(new ActionNode(() =>
                {
                    Debug.Log("Attacking!");
                    return BTStatus.Success;
                }));
        }

        private NodeBase BuildChaseBranch()
        {
            return (NodeBase)new Sequence()
                .AddChild(new Condition(() =>
                    _runner.Tree.Blackboard.Get<bool>("hasTarget")))
                .AddChild(new Condition(() =>
                    _runner.Tree.Blackboard.Get<float>("targetDistance") <= _chaseRange))
                .AddChild(new ActionNode(() =>
                {
                    Debug.Log("Chasing target...");
                    return BTStatus.Running;
                }));
        }

        private NodeBase BuildPatrolBranch()
        {
            return new ActionNode(() =>
            {
                Debug.Log("Patrolling...");
                return BTStatus.Running;
            });
        }
    }
}
