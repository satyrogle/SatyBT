using System.Collections.Generic;
using UnityEngine;

namespace SatyBT.Samples
{
    /// <summary>
    /// Self-contained bootstrapper for the StealthGuard sample. Attach it to an
    /// empty GameObject and press Play — it builds a small scene from
    /// primitives (a floor, patrol waypoints, guards, and a WASD-movable
    /// player) and wires up a <see cref="GuardCoordinator"/>. No art assets or
    /// prebuilt scene required; save it as a scene to record the README GIF.
    /// </summary>
    public sealed class StealthGuardDemo : MonoBehaviour
    {
        [SerializeField] private int _guardCount = 3;
        [SerializeField] private float _playerSpeed = 5f;

        private Transform _player;

        private void Start()
        {
            CreateFloor();
            _player = CreatePlayer();

            Transform[] waypoints = CreateWaypoints();

            var coordinator = gameObject.AddComponent<GuardCoordinator>();

            for (int i = 0; i < _guardCount; i++)
            {
                StealthGuard guard = CreateGuard(i, waypoints);
                coordinator.Register(guard);
            }
        }

        private void Update()
        {
            if (_player == null)
                return;

            // Simple WASD control so you can walk the player into the guards'
            // view and hearing to trigger chase / investigate / alert.
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 move = new Vector3(h, 0f, v);
            if (move.sqrMagnitude > 1f)
                move.Normalize();
            _player.position += move * (_playerSpeed * Time.deltaTime);
        }

        private static void CreateFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(30f, 1f, 30f);
        }

        private Transform CreatePlayer()
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 1f, -10f);
            Colorize(player, new Color(0.2f, 0.5f, 1f));
            return player.transform;
        }

        private Transform[] CreateWaypoints()
        {
            Vector3[] positions =
            {
                new Vector3(-8f, 0f, -8f),
                new Vector3(8f, 0f, -8f),
                new Vector3(8f, 0f, 8f),
                new Vector3(-8f, 0f, 8f)
            };

            var waypoints = new List<Transform>(positions.Length);
            for (int i = 0; i < positions.Length; i++)
            {
                var wp = new GameObject($"Waypoint{i}").transform;
                wp.position = positions[i];
                waypoints.Add(wp);
            }
            return waypoints.ToArray();
        }

        private StealthGuard CreateGuard(int index, Transform[] waypoints)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"Guard{index}";
            go.transform.position = new Vector3(-6f + index * 6f, 1f, 0f);
            Colorize(go, new Color(0.9f, 0.3f, 0.25f));

            var guard = go.AddComponent<StealthGuard>();
            guard.Configure(_player, waypoints);
            return guard;
        }

        private static void Colorize(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = color;
        }
    }
}
