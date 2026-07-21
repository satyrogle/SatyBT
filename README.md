# SatyBT

[![CI](https://github.com/satyrogle/SatyBT/actions/workflows/ci.yml/badge.svg)](https://github.com/satyrogle/SatyBT/actions/workflows/ci.yml)

A code-first, zero-GC behaviour tree framework for Unity.

> The CI badge stays unknown/failing until a `UNITY_LICENSE` secret is added to
> the repository — see [`.github/workflows/ci.yml`](.github/workflows/ci.yml)
> and the [GameCI activation guide](https://game.ci/docs/github/activation).

## What this is

SatyBT provides a minimal behaviour tree implementation for Unity (C#) with two properties that most alternatives lack:

1. **Zero garbage collection during tick.** No allocations occur on the tick path. This matters for mobile targets and for projects running many agents simultaneously.
2. **Runtime node injection.** External systems can insert and remove nodes from a live tree without rebuilding it. Injected nodes can auto-expire after a specified number of ticks.

## Installation

Add via Unity Package Manager using the git URL:

```
https://github.com/satyrogle/SatyBT.git
```

Or clone the repository into your project's `Packages/` folder.

Requires Unity 2021.3 or later.

## Architecture

### Node types

Every node extends `NodeBase` and implements `Tick()`, returning `BTStatus.Success`, `BTStatus.Failure`, or `BTStatus.Running`.

**Composites** hold ordered child lists:

- `Sequence` — ticks children left-to-right. Fails on first child failure (AND gate).
- `Selector` — ticks children left-to-right. Succeeds on first child success (OR gate).

Both composites support `Insert()` and `Remove()` for runtime modification.

**Decorators** wrap a single child:

- `Inverter` — flips Success/Failure.
- `Repeater` — re-ticks the child N times or indefinitely.
- `Succeeder` — always returns Success after the child finishes.

**Leaves** are terminal nodes:

- `Condition` — wraps a `Func<bool>`. Returns Success if true, Failure if false.
- `ActionNode` — wraps a `Func<BTStatus>`. Returns whatever the delegate returns.

Both leaf types are intended for prototyping. Production code should subclass `NodeBase` directly.

### Blackboard

A shared key-value store attached to each tree. Nodes read and write state through it.

```csharp
tree.Blackboard.Set("health", 100f);
float hp = tree.Blackboard.Get<float>("health");
```

Supports reactive subscriptions:

```csharp
tree.Blackboard.Subscribe("health", key => Debug.Log($"{key} changed"));
```

### NodeInjector

The differentiating feature. External systems can inject nodes into a running tree:

```csharp
var handle = tree.Injector.Inject(
    id: "enrage",
    node: enrageSubtree,
    target: rootSelector,
    position: 0,
    durationTicks: 300
);

handle.OnRemoved += h => Debug.Log("Enrage expired");
```

The injected node receives the tree's blackboard automatically. After 300 ticks it's removed and the `OnRemoved` event fires. You can also remove manually:

```csharp
tree.Injector.Remove("enrage");
```

This pattern was generalised from the StateInjector system in [Desk 42](https://github.com/satyrogle/Desk-42), where external game systems (the "Red Tape Engine") modify NPC decision trees during gameplay.

### Running a tree

```csharp
var root = new Selector()
    .AddChild(attackBranch)
    .AddChild(chaseBranch)
    .AddChild(patrolBranch);

var tree = new BehaviourTree(root);

// Attach to a GameObject
var runner = gameObject.AddComponent<BehaviourTreeRunner>();
runner.Initialise(tree);
```

The `BehaviourTreeRunner` ticks the tree every frame in `Update()`.

## Samples

Import via Package Manager > SatyBT > Samples:

- **Basic NPC** — patrol-chase-attack tree with blackboard-driven target detection.
- **Dynamic Injection** — a DifficultyDirector that injects an enrage phase into an NPC's live tree after 500 ticks, with automatic expiry.

## Licence

MIT. See [LICENSE](LICENSE).
