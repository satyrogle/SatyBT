using UnityEngine;

namespace SatyBT.Samples
{
    /// <summary>
    /// A stealth guard whose behaviour tree patrols waypoints, investigates
    /// noises, and chases a visible player — using observer aborts to switch
    /// between them reactively and a cooldown to avoid re-investigating
    /// immediately.
    ///
    /// Priority (top of the root selector wins):
    ///   1. Chase        — ObserverDecorator(LowerPriority) on "playerVisible"
    ///   2. Investigate  — ObserverDecorator(LowerPriority) on "noiseHeard",
    ///                      gated by a CooldownDecorator
    ///   3. Patrol       — default
    ///
    /// The <see cref="GuardCoordinator"/> may inject an even-higher-priority
    /// "alert" subtree at runtime.
    ///
    /// Sensing runs in Update with an early execution order so the blackboard
    /// is fresh before the <see cref="BehaviourTreeRunner"/> ticks the tree.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class StealthGuard : MonoBehaviour
    {
        public const string PlayerVisibleKey = "playerVisible";
        public const string NoiseHeardKey = "noiseHeard";
        public const string LastKnownPosKey = "lastKnownPlayerPos";
        public const string AlertedKey = "isAlerted";

        [Header("References")]
        [SerializeField] private Transform _player;
        [SerializeField] private Transform[] _waypoints;

        [Header("Senses")]
        [SerializeField] private float _viewRange = 8f;
        [SerializeField] private float _viewAngle = 90f;
        [SerializeField] private float _hearingRange = 5f;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 3.5f;
        [SerializeField] private float _arriveRange = 0.4f;
        [SerializeField] private float _catchRange = 1.2f;

        [Header("Behaviour")]
        [SerializeField] private float _lookAroundSeconds = 2.5f;
        [SerializeField] private float _reinvestigateCooldown = 4f;

        private BehaviourTree _tree;
        private Blackboard _bb;
        private int _waypointIndex;

        private bool _lastVisible;
        private bool _lastNoise;
        private Vector3 _alertTarget;

        /// <summary>The guard's tree. Used by the coordinator to inject into it.</summary>
        public BehaviourTree Tree => _tree;

        /// <summary>Root selector, the injection target for alert behaviour.</summary>
        public CompositeNode RootComposite => _tree?.Root as CompositeNode;

        public Blackboard Blackboard => _bb;

        public bool SeesPlayer => _bb != null && _bb.Get<bool>(PlayerVisibleKey);

        public Vector3 LastKnownPlayerPosition =>
            _bb != null ? _bb.Get<Vector3>(LastKnownPosKey) : transform.position;

        /// <summary>Configure the guard before it builds its tree (used by the demo).</summary>
        public void Configure(Transform player, Transform[] waypoints)
        {
            _player = player;
            _waypoints = waypoints;
        }

        private void Start()
        {
            _tree = new BehaviourTree(BuildRoot());
            _bb = _tree.Blackboard;
            _bb.Set(LastKnownPosKey, transform.position);

            var runner = gameObject.AddComponent<BehaviourTreeRunner>();
            runner.Initialise(_tree);
        }

        private void Update()
        {
            if (_bb == null)
                return;

            Sense();
        }

        // ── Sensing ─────────────────────────────────────────────────

        private void Sense()
        {
            bool visible = CanSeePlayer();
            if (visible)
                _bb.Set(LastKnownPosKey, _player.position);

            // Write only on change: keeps the observer callback edge-triggered
            // and avoids per-frame boxing.
            if (visible != _lastVisible)
            {
                _bb.Set(PlayerVisibleKey, visible);
                _lastVisible = visible;
            }

            bool noise = !visible && _player != null &&
                         Vector3.Distance(transform.position, _player.position) <= _hearingRange;
            if (noise)
                _bb.Set(LastKnownPosKey, _player.position);

            if (noise != _lastNoise)
            {
                _bb.Set(NoiseHeardKey, noise);
                _lastNoise = noise;
            }
        }

        private bool CanSeePlayer()
        {
            if (_player == null)
                return false;

            Vector3 toPlayer = _player.position - transform.position;
            if (toPlayer.sqrMagnitude > _viewRange * _viewRange)
                return false;

            float angle = Vector3.Angle(transform.forward, toPlayer);
            return angle <= _viewAngle * 0.5f;
        }

        // ── Tree construction ───────────────────────────────────────

        private NodeBase BuildRoot()
        {
            return new Selector(
                new ObserverDecorator(
                    BuildChase(),
                    PlayerVisibleKey, () => _bb.Get<bool>(PlayerVisibleKey),
                    ObserverDecorator.AbortMode.LowerPriority),

                new ObserverDecorator(
                    new CooldownDecorator(BuildInvestigate(), _reinvestigateCooldown, onSuccessOnly: false),
                    NoiseHeardKey, () => _bb.Get<bool>(NoiseHeardKey),
                    ObserverDecorator.AbortMode.LowerPriority),

                BuildPatrol());
        }

        private NodeBase BuildChase()
        {
            return new ActionNode(() =>
            {
                Vector3 target = _bb.Get<Vector3>(LastKnownPosKey);
                MoveTowards(target);

                if (_bb.Get<bool>(PlayerVisibleKey) &&
                    Vector3.Distance(transform.position, target) <= _catchRange)
                {
                    Debug.Log($"{name}: caught the player!");
                    return BTStatus.Success;
                }
                return BTStatus.Running;
            });
        }

        private NodeBase BuildInvestigate()
        {
            return new Sequence(
                new ActionNode(() =>
                {
                    Vector3 target = _bb.Get<Vector3>(LastKnownPosKey);
                    MoveTowards(target);
                    return Vector3.Distance(transform.position, target) <= _arriveRange
                        ? BTStatus.Success
                        : BTStatus.Running;
                }),
                new WaitNode(_lookAroundSeconds),
                new ActionNode(() =>
                {
                    // Give up: clear the noise so patrol resumes next tick.
                    _bb.Set(NoiseHeardKey, false);
                    _lastNoise = false;
                    return BTStatus.Success;
                }));
        }

        private NodeBase BuildPatrol()
        {
            return new Sequence(
                new ActionNode(() =>
                {
                    if (_waypoints == null || _waypoints.Length == 0)
                        return BTStatus.Success;

                    Vector3 target = _waypoints[_waypointIndex].position;
                    MoveTowards(target);
                    return Vector3.Distance(transform.position, target) <= _arriveRange
                        ? BTStatus.Success
                        : BTStatus.Running;
                }),
                new ActionNode(() =>
                {
                    if (_waypoints != null && _waypoints.Length > 0)
                        _waypointIndex = (_waypointIndex + 1) % _waypoints.Length;
                    return BTStatus.Success;
                }));
        }

        // ── Movement (used by the tree and by injected alert behaviour) ──

        public void SetAlertTarget(Vector3 target) => _alertTarget = target;

        public void MoveTowardsAlertTarget() => MoveTowards(_alertTarget);

        private void MoveTowards(Vector3 target)
        {
            target.y = transform.position.y;
            transform.position = Vector3.MoveTowards(
                transform.position, target, _moveSpeed * Time.deltaTime);

            Vector3 dir = target - transform.position;
            if (dir.sqrMagnitude > 0.0001f)
                transform.forward = Vector3.Slerp(transform.forward, dir.normalized, 0.2f);
        }
    }
}
