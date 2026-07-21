# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-07-21

Initial public release.

### Added

- **Core** — `BTStatus` enum; `NodeBase` abstract base with an enter/exit
  lifecycle, cascading `Reset`/`Abort`, an activation lifecycle, and a
  `LastStatus` / `HasTicked` debug hook; `BehaviourTree` container; and a
  `BehaviourTreeRunner` MonoBehaviour. `Tick(float deltaTime)` threads frame
  time through the tree so time-based nodes need no `UnityEngine.Time`
  dependency in the core.
- **Composites** — `Sequence`, `Selector`, and `Parallel` (configurable
  `SuccessPolicy` / `FailurePolicy`, cross-tick completion, abort-on-resolve).
  All support params-array constructors, runtime `Insert`/`Remove`, and a
  pending-interrupt mechanism used by observer aborts.
- **Decorators** — `Inverter`, `Repeater`, `Succeeder`, `CooldownDecorator`
  (deltaTime-driven gate), and `ObserverDecorator` with `None` / `Self` /
  `LowerPriority` / `Both` abort modes.
- **Leaves** — `Condition` and `ActionNode` for prototyping; subclass
  `NodeBase` for production nodes.
- **Blackboard** — typed key-value store with `Get`, `TryGet`, a
  development-build type-mismatch warning, and per-key reactive subscriptions.
- **Node injection** — `NodeInjector` (`Inject`, `Remove`, `IsActive`,
  `ActiveCount`, `ActiveInjections`) and `InjectionHandle` with tick-count
  expiry and `OnInjected` / `OnRemoved` events. Injected subtrees receive the
  blackboard and are activated on injection and deactivated on removal.
- **Editor** — Tree Debugger window (Window > SatyBT > Tree Debugger) showing a
  live, colour-coded node hierarchy, injected-node markers, and a
  blackboard / injections side panel.
- **Tests** — Unity Test Framework suite covering composite semantics,
  decorators, parallel policies, observer aborts, blackboard, injection
  lifecycle, Reset/Abort cascade, and a zero-allocation tick test.
- **CI** — GitHub Actions workflow (GameCI) running edit-mode and play-mode
  tests against Unity 2021.3 LTS.
- **Samples** — BasicNPC (patrol-chase-attack), DynamicInjection
  (DifficultyDirector), and StealthGuard (observer aborts, cooldown-gated
  investigate, and coordinator-driven injection).
