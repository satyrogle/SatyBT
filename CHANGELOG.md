# Changelog

## [1.0.0] - 2026-07-20

Initial public release.

### Core
- `BTStatus` enum (Success, Failure, Running)
- `NodeBase` abstract base with enter/exit lifecycle and zero-GC tick path
- `BehaviourTree` container with root node, blackboard, and injector
- `BehaviourTreeRunner` MonoBehaviour for per-frame ticking

### Composites
- `Sequence` (AND gate)
- `Selector` (OR gate)
- Runtime `Insert()` and `Remove()` on all composites

### Decorators
- `Inverter`
- `Repeater` (fixed count or infinite)
- `Succeeder`

### Leaves
- `Condition` (bool predicate wrapper)
- `ActionNode` (BTStatus delegate wrapper)

### Blackboard
- Typed key-value store
- Reactive subscriptions per key

### Node injection
- `NodeInjector` with `Inject()`, `Remove()`, `IsActive()`, `ActiveCount`
- `InjectionHandle` with tick-count expiry, `OnInjected` / `OnRemoved` events
- Automatic blackboard propagation to injected nodes

### Samples
- BasicNPC (patrol-chase-attack)
- DynamicInjection (DifficultyDirector)
