using System.Collections.Generic;
using UnityEngine;

namespace SatyBT.Samples
{
    /// <summary>
    /// Watches a group of <see cref="StealthGuard"/>s and, when any of them
    /// spots the player, injects a high-priority "alert" subtree into every
    /// guard's tree so the whole group converges on the last known position.
    /// The injection auto-expires after a set number of ticks, after which the
    /// guards fall back to their normal behaviour. Demonstrates NodeInjector
    /// driving many trees from one external system.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class GuardCoordinator : MonoBehaviour
    {
        private const string AlertId = "alert";

        [SerializeField] private List<StealthGuard> _guards = new List<StealthGuard>();
        [SerializeField] private int _alertDurationTicks = 600;

        private int _activeAlerts;
        private bool _alerted;

        public void Register(StealthGuard guard)
        {
            if (guard != null && !_guards.Contains(guard))
                _guards.Add(guard);
        }

        private void Update()
        {
            if (_alerted)
                return;

            for (int i = 0; i < _guards.Count; i++)
            {
                StealthGuard guard = _guards[i];
                if (guard != null && guard.SeesPlayer)
                {
                    AlertAll(guard.LastKnownPlayerPosition);
                    return;
                }
            }
        }

        private void AlertAll(Vector3 position)
        {
            _alerted = true;
            _activeAlerts = 0;

            for (int i = 0; i < _guards.Count; i++)
            {
                StealthGuard guard = _guards[i];
                if (guard == null || guard.Tree == null || guard.RootComposite == null)
                    continue;

                var handle = guard.Tree.Injector.Inject(
                    AlertId,
                    BuildAlert(guard, position),
                    guard.RootComposite,
                    position: 0,
                    durationTicks: _alertDurationTicks);

                if (handle != null)
                {
                    _activeAlerts++;
                    StealthGuard captured = guard;
                    handle.OnRemoved += _ => OnAlertExpired(captured);
                }
            }

            Debug.Log($"[Coordinator] Player spotted — alerting {_activeAlerts} guard(s).");
        }

        private NodeBase BuildAlert(StealthGuard guard, Vector3 position)
        {
            return new Sequence(
                new ActionNode(() =>
                {
                    guard.Blackboard.Set(StealthGuard.AlertedKey, true);
                    guard.SetAlertTarget(position);
                    return BTStatus.Success;
                }),
                new ActionNode(() =>
                {
                    guard.MoveTowardsAlertTarget();
                    return BTStatus.Running;
                }));
        }

        private void OnAlertExpired(StealthGuard guard)
        {
            if (guard != null && guard.Blackboard != null)
                guard.Blackboard.Set(StealthGuard.AlertedKey, false);

            _activeAlerts--;
            if (_activeAlerts <= 0)
            {
                _activeAlerts = 0;
                _alerted = false; // ready to alert again on the next detection
                Debug.Log("[Coordinator] Alert expired — guards standing down.");
            }
        }
    }
}
