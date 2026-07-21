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

            var root = new Selector(
                BuildAttackBranch(),
                BuildChaseBranch(),
                BuildPatrolBranch());

            var tree = new BehaviourTree(root);

            // Seed some initial blackboard values
            tree.Blackboard.Set("hasTarget", false);
            tree.Blackboard.Set("targetDistance", float.MaxValue);

            _runner.Initialise(tree);
        }

        private NodeBase BuildAttackBranch()
        {
            return new Sequence(
                new Condition(() =>
                    _runner.Tree.Blackboard.Get<float>("targetDistance") <= _attackRange),
                new ActionNode(() =>
                {
                    Debug.Log("Attacking!");
                    return BTStatus.Success;
                }));
        }

        private NodeBase BuildChaseBranch()
        {
            return new Sequence(
                new Condition(() =>
                    _runner.Tree.Blackboard.Get<bool>("hasTarget")),
                new Condition(() =>
                    _runner.Tree.Blackboard.Get<float>("targetDistance") <= _chaseRange),
                new ActionNode(() =>
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
