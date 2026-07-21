using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SatyBT.Editor
{
    /// <summary>
    /// Play-mode window that visualises a live <see cref="BehaviourTree"/>:
    /// an indented node hierarchy colour-coded by each node's last status,
    /// with injected nodes marked, plus a side panel of blackboard entries and
    /// active injections. Open via Window &gt; SatyBT &gt; Tree Debugger.
    ///
    /// This is editor-only tooling (the assembly targets the Editor platform)
    /// and has no effect on runtime builds. It reads the tree through the
    /// public runtime API only; it never drives or mutates it.
    /// </summary>
    public sealed class TreeDebuggerWindow : EditorWindow
    {
        private BehaviourTreeRunner _runner;
        private Vector2 _treeScroll;
        private Vector2 _sideScroll;

        private readonly List<BehaviourTreeRunner> _runners = new List<BehaviourTreeRunner>();
        private readonly Dictionary<NodeBase, InjectionHandle> _injectedLookup =
            new Dictionary<NodeBase, InjectionHandle>();

        [MenuItem("Window/SatyBT/Tree Debugger")]
        public static void Open()
        {
            var window = GetWindow<TreeDebuggerWindow>();
            window.titleContent = new GUIContent("SatyBT Debugger");
            window.minSize = new Vector2(420f, 240f);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        // Repaint continuously while playing so the view tracks live ticks.
        private void OnEditorUpdate()
        {
            if (Application.isPlaying)
                Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play mode to inspect a live behaviour tree.", MessageType.Info);
                return;
            }

            if (_runner == null || _runner.Tree == null)
            {
                EditorGUILayout.HelpBox(
                    "No BehaviourTreeRunner selected, or its tree is not initialised yet.",
                    MessageType.Warning);
                return;
            }

            RebuildInjectedLookup(_runner.Tree);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.MinWidth(260f));
            EditorGUILayout.LabelField("Tree", EditorStyles.boldLabel);
            _treeScroll = EditorGUILayout.BeginScrollView(_treeScroll);
            DrawNode(_runner.Tree.Root, 0);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(240f));
            _sideScroll = EditorGUILayout.BeginScrollView(_sideScroll);
            DrawBlackboard(_runner.Tree.Blackboard);
            EditorGUILayout.Space();
            DrawInjections(_runner.Tree);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            RefreshRunners();
            string current = _runner != null ? RunnerLabel(_runner) : "(select runner)";
            if (EditorGUILayout.DropdownButton(new GUIContent(current), FocusType.Keyboard,
                    EditorStyles.toolbarDropDown, GUILayout.Width(220f)))
            {
                var menu = new GenericMenu();
                if (_runners.Count == 0)
                {
                    menu.AddDisabledItem(new GUIContent("No runners in scene"));
                }
                else
                {
                    for (int i = 0; i < _runners.Count; i++)
                    {
                        var r = _runners[i];
                        menu.AddItem(new GUIContent(RunnerLabel(r)), r == _runner, () => _runner = r);
                    }
                }
                menu.ShowAsContext();
            }

            GUILayout.FlexibleSpace();

            if (_runner != null && _runner.Tree != null)
            {
                GUILayout.Label(
                    $"Tick {_runner.Tree.TickCount}  •  Root {_runner.Tree.Root?.LastStatus}",
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void RefreshRunners()
        {
            _runners.Clear();
            if (!Application.isPlaying)
                return;

#if UNITY_2022_2_OR_NEWER
            var found = Object.FindObjectsByType<BehaviourTreeRunner>(FindObjectsSortMode.None);
#else
            var found = Object.FindObjectsOfType<BehaviourTreeRunner>();
#endif
            for (int i = 0; i < found.Length; i++)
                _runners.Add(found[i]);

            if ((_runner == null || !_runners.Contains(_runner)) && _runners.Count > 0)
                _runner = _runners[0];
        }

        private static string RunnerLabel(BehaviourTreeRunner r)
        {
            return r != null ? r.gameObject.name : "(null)";
        }

        private void RebuildInjectedLookup(BehaviourTree tree)
        {
            _injectedLookup.Clear();
            foreach (var handle in tree.Injector.ActiveInjections)
                _injectedLookup[handle.Node] = handle;
        }

        private void DrawNode(NodeBase node, int depth)
        {
            if (node == null)
                return;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(depth * 14f);

            Color previous = GUI.color;
            GUI.color = StatusColor(node);

            string label = node.GetType().Name;
            if (_injectedLookup.TryGetValue(node, out var handle))
            {
                int remaining = handle.ExpiresAtTick < 0 ? -1 : handle.ExpiresAtTick - _runner.Tree.TickCount;
                label += remaining >= 0
                    ? $"   [injected '{handle.Id}', {remaining} ticks]"
                    : $"   [injected '{handle.Id}']";
            }

            GUILayout.Label(label);
            GUI.color = previous;

            EditorGUILayout.EndHorizontal();

            if (node is CompositeNode composite)
            {
                for (int i = 0; i < composite.ChildCount; i++)
                    DrawNode(composite.GetChild(i), depth + 1);
            }
            else if (node is DecoratorNode decorator)
            {
                DrawNode(decorator.Child, depth + 1);
            }
        }

        private static Color StatusColor(NodeBase node)
        {
            if (!node.HasTicked)
                return new Color(0.5f, 0.5f, 0.5f, 1f); // never ticked: dim

            switch (node.LastStatus)
            {
                case BTStatus.Running: return new Color(1f, 0.85f, 0.2f, 1f); // yellow
                case BTStatus.Success: return new Color(0.4f, 0.9f, 0.4f, 1f); // green
                default: return new Color(0.72f, 0.72f, 0.72f, 1f);           // failure: grey
            }
        }

        private void DrawBlackboard(Blackboard blackboard)
        {
            EditorGUILayout.LabelField("Blackboard", EditorStyles.boldLabel);
            if (blackboard == null)
                return;

            foreach (var kvp in blackboard.Entries)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(kvp.Key, GUILayout.Width(120f));
                EditorGUILayout.LabelField(kvp.Value != null ? kvp.Value.ToString() : "null");
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawInjections(BehaviourTree tree)
        {
            EditorGUILayout.LabelField($"Active injections ({tree.Injector.ActiveCount})", EditorStyles.boldLabel);
            foreach (var handle in tree.Injector.ActiveInjections)
            {
                int remaining = handle.ExpiresAtTick < 0 ? -1 : handle.ExpiresAtTick - tree.TickCount;
                string line = remaining >= 0
                    ? $"{handle.Id} — {remaining} ticks left"
                    : $"{handle.Id} — permanent";
                EditorGUILayout.LabelField(line);
            }
        }
    }
}
