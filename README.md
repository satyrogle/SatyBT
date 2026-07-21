# SatyBT

[![CI](https://github.com/satyrogle/SatyBT/actions/workflows/ci.yml/badge.svg)](https://github.com/satyrogle/SatyBT/actions/workflows/ci.yml)

A code-first, zero-GC behaviour tree framework for Unity, with reactive
observer aborts and runtime node injection.

> **[Demo GIF placeholder — record from the StealthGuard sample.]**
> Attach `StealthGuardDemo` to an empty GameObject and press Play: guards
> patrol, investigate noises, and chase on sight, and a coordinator injects a
> group-wide alert when the player is spotted.

> The CI badge stays unknown/failing until a `UNITY_LICENSE` secret is added to
> the repository — see [`.github/workflows/ci.yml`](.github/workflows/ci.yml)
> and the [GameCI activation guide](https://game.ci/docs/github/activation).

## Why SatyBT

- **Code-first.** Trees are plain C#, so they live in version control, diff
  cleanly, and refactor under the compiler — no editor graph to keep in sync.
- **Zero-GC tick.** No allocations occur on the tick path. This matters for
  mobile and for scenes running many agents. See the
  [zero-GC FAQ](#is-it-really-zero-gc) and `Tests/Runtime/AllocationTests.cs`.
- **Reactive.** `ObserverDecorator` watches a blackboard key and aborts a
  running subtree — or pre-empts a lower-priority one — the moment state
  changes, instead of polling every tick.
- **Runtime injection.** External systems can insert and remove nodes in a live
  tree, with optional automatic expiry after a number of ticks.

## Features

- **Composites:** `Sequence`, `Selector`, `Parallel` (RequireOne / RequireAll
  success and failure policies).
- **Decorators:** `Inverter`, `Repeater`, `Succeeder`, `CooldownDecorator`,
  `ObserverDecorator` (None / Self / LowerPriority / Both aborts).
- **Leaves:** `Condition`, `ActionNode` for prototyping — or subclass
  `NodeBase` for production nodes.
- **Blackboard:** typed store with `Get` / `TryGet` and per-key reactive
  subscriptions.
- **Injection:** `NodeInjector` with tick-count expiry and injection events.
- **Editor Tree Debugger:** a live, colour-coded view of a running tree.
- `deltaTime` is threaded through `Tick`, so time-based nodes need no
  `UnityEngine.Time` dependency in the core.

## Installation

Unity Package Manager → **Add package from git URL**:

```
https://github.com/satyrogle/SatyBT.git
```

Or clone into your project's `Packages/` folder. Requires **Unity 2021.3 or
later**.

_OpenUPM distribution is planned (pending)._

## Quick start

Build a tree with nested constructors and drive it with the runner:

```csharp
using SatyBT;
using UnityEngine;

public class Npc : MonoBehaviour
{
    private void Start()
    {
        var root = new Selector(
            new Sequence(
                new Condition(() => CanSeeEnemy()),
                new ActionNode(() => { Attack(); return BTStatus.Success; })),
            new ActionNode(() => { Patrol(); return BTStatus.Running; }));

        var tree = new BehaviourTree(root);

        // Runner ticks every frame in Update():
        gameObject.AddComponent<BehaviourTreeRunner>().Initialise(tree);

        // …or tick it yourself at any cadence:
        // tree.Tick(Time.deltaTime);
    }
}
```

The blackboard is created with the tree:

```csharp
tree.Blackboard.Set("health", 100f);
float hp = tree.Blackboard.Get<float>("health");

if (tree.Blackboard.TryGet("target", out Transform target))
    Chase(target);

tree.Blackboard.Subscribe("health", key => Debug.Log($"{key} changed"));
```

## Observer aborts

An `ObserverDecorator` reacts to a blackboard key. In `LowerPriority` mode it
pre-empts a running lower-priority sibling the moment its condition becomes
true — the standard "chase interrupts patrol on sight" pattern:

```csharp
var root = new Selector(
    new ObserverDecorator(
        chaseSubtree,
        key: "playerVisible",
        condition: () => bb.Get<bool>("playerVisible"),
        abortMode: ObserverDecorator.AbortMode.LowerPriority),
    patrolSubtree);
```

In `Self` mode it aborts its own subtree when the condition stops holding.
Subscriptions attach when the node joins a live tree and detach on removal, so
an observer keeps watching even while a lower-priority branch runs — with no
per-tick allocation.

## Runtime injection

Insert a subtree into a live tree, optionally auto-expiring after N ticks:

```csharp
var handle = tree.Injector.Inject(
    id: "enrage",
    node: enrageSubtree,
    target: rootSelector,
    position: 0,          // highest priority
    durationTicks: 300);  // auto-removed after 300 ticks

handle.OnRemoved += _ => Debug.Log("Enrage expired");

// or remove manually:
tree.Injector.Remove("enrage");
```

The injected subtree receives the tree's blackboard automatically, and any
observers inside it subscribe on injection and unsubscribe on removal.

## Editor tree debugger

Open **Window > SatyBT > Tree Debugger** in Play mode, pick a
`BehaviourTreeRunner`, and watch the tree evaluate: nodes are colour-coded by
last status (Running yellow, Success green, Failure grey, never-ticked dim),
injected nodes show their id and remaining ticks, and a side panel lists live
blackboard entries and active injections.

> **[Debugger screenshot placeholder.]**

## Architecture

Every node extends `NodeBase` and implements `Tick(float deltaTime)`, returning
`BTStatus.Success`, `Failure`, or `Running`. Composites hold ordered children
(`Sequence` = AND, `Selector` = priority OR, `Parallel` = all-at-once);
decorators wrap a single child; leaves are terminal. A `Blackboard` is shared
across the tree, and a `NodeInjector` handles runtime modification. The node
injection pattern was generalised from the StateInjector / "Red Tape Engine" in
[Desk 42](https://github.com/satyrogle/Desk-42).

## Samples

Import via Package Manager → SatyBT → Samples.

| Sample | Shows |
| --- | --- |
| **Basic NPC** | Patrol → chase → attack with blackboard-driven detection. |
| **Dynamic Injection** | A `DifficultyDirector` injecting an enrage phase into a live tree with automatic expiry. |
| **Stealth Guard** | The full feature set: observer `LowerPriority` aborts, a cooldown-gated investigate branch, a `WaitNode`, and a `GuardCoordinator` injecting a group-wide alert. |

## FAQ

### Why code-first?

No visual graph to author or keep in sync with code. Trees are C#, so they are
version-controlled, reviewable in a diff, and refactored with the compiler's
help. Prototype with `Condition` / `ActionNode` lambdas; promote hot or reused
logic to named `NodeBase` subclasses.

### Is it really zero-GC?

The tick path allocates nothing: composites pre-size their buffers, the injector
reuses its scan structures, and observer subscriptions happen off the tick path.
`Tests/Runtime/AllocationTests.cs` ticks a tree containing every node type — plus
a live injection and an active observer subscription — under
`Is.Not.AllocatingGCMemory()`. Note that _writing_ a value-type blackboard key
boxes it, so prefer reading state and writing only when it changes (the sample
guards do exactly this).

### When should I subclass NodeBase?

For anything shipping in production. `Condition` and `ActionNode` are convenient
for prototyping but hide intent behind delegates; a named subclass reads better
and can hold per-run state via `OnEnter` / `OnReset`.

## Licence

MIT. See [LICENSE](LICENSE).
