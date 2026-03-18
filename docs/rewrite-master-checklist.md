# Rewrite Master Checklist

## Legend

- [x] completed and verified
- [ ] not completed yet
- [-] intentionally frozen or deferred

## Global Tracking

- [x] architecture audit completed
- [x] gameplay and world runtime review completed
- [x] diagnostics review completed
- [x] scene, UI, save, and legacy wiring review completed
- [x] rewrite roadmap created
- [x] runtime ownership document created
- [x] scene cleanup checklist created
- [x] unified cleanup and restructure plan created
- [x] Stage 0 execution started
- [x] manual smoke test passed for `New Game`, `Continue`, world restore, fog restore, and basic hero movement
- [x] script layout cleanup applied without breaking the local build
- [x] Stage 0 completed
- [ ] Stage 1 completed
- [ ] Stage 2 completed
- [ ] Stage 3 completed
- [ ] Stage 4 completed

## Stage 0 - Cleanup And Safety Rails

### World Runtime And Coordinates

- [x] coordinate mismatch identified and verified
- [x] double mutable world state identified and verified
- [x] choose the single world authority
- [x] introduce the initial `GameSaveData` root save model
- [x] document the final world mutation path
- [x] align every gameplay `WorldToCell` contract to one coordinate system
- [x] make hero cell readers depend on explicit world readiness
- [x] ensure dig and stone mutations go through one runtime authority

### Scene Cleanup

- [x] legacy and test scene wiring identified
- [x] broken persistent callback identified
- [x] UI warning sources identified
- [x] remove `SaveLoadSystem` from production scenes or move it to debug-only usage
- [x] remove stale `RemoveDestroyedBlocksFromBinary` callback from `MainScene`
- [x] decide one supported button transition strategy
- [x] remove warning-producing button setups from clean runtime scenes
- [x] separate menu navigation concerns from save concerns

### Diagnostics Cleanup

- [x] diagnostics noise sources identified
- [x] duplicate context sources identified
- [x] route Unity log relay safely through a main-thread queue or equivalent buffer
- [x] remove duplicated gameplay keys from emitted events
- [x] reduce heartbeat noise to a useful level
- [x] add explicit runtime bootstrap events such as `WorldGridReady`
- [x] verify that a clean boot session is readable without UI warning spam

### Stage 0 Exit Gate

- [x] hero cell logic and terrain cell logic report the same cells
- [x] production scenes do not reference test-only runtime objects
- [x] scenes do not call missing methods
- [x] clean session logs are readable
- [x] one world authority is documented and implemented

## Stage 1 - Ground Core

### Structure

- [ ] introduce `HeroMotor`
- [ ] introduce `HeroGroundSensor`
- [ ] introduce `HeroWallSensor`
- [ ] shrink or replace `HeroController`
- [ ] keep `HeroState` locomotion-only

### Behavior

- [ ] hero does not start airborne without cause
- [ ] hero stands on solid cells consistently
- [ ] hero falls only when support is absent
- [ ] hero does not move through solid cells on X

### Logging

- [ ] locomotion logs are limited to useful gameplay events
- [ ] movement blocking is explained by deterministic sensor results
- [ ] grounded changes are easy to read in a short log session

## Stage 2 - Mining

### Structure

- [ ] introduce `HeroMining`
- [ ] target cell selection uses current hero cell and dominant input axis
- [ ] mining reads from runtime instead of scene helpers
- [ ] mining writes through one world mutation API

### Behavior

- [ ] only mineable cells can be dug
- [ ] blocked digs report why they were rejected
- [ ] runtime and view stay in sync after a dig
- [ ] movement behavior remains unchanged while mining is added

### Logging

- [ ] `DigStarted` exists and is meaningful
- [ ] `DigBlocked` exists and explains the block reason
- [ ] `DigCompleted` exists and reflects a real world mutation

## Stage 3 - Ladder

### Structure

- [ ] introduce `HeroLadder`
- [ ] ladder entry uses sensors and runtime queries
- [ ] ladder mode is explicit in hero runtime

### Behavior

- [ ] vertical ladder movement is deterministic
- [ ] side exit is deterministic
- [ ] top exit is deterministic
- [ ] bottom exit is deterministic
- [ ] ladder logic does not break ground logic

### Logging

- [ ] `LadderEntered`
- [ ] `LadderExited`
- [ ] `LadderBlocked`

## Stage 4 - Save, UI, And Secondary Systems

### Save And Scene Flow

- [ ] decide whether current save system is retained, rewritten, or partly removed
- [ ] separate scene navigation from save loading
- [ ] move persistence policy out of menu button side effects
- [ ] decide whether save writes are immediate, throttled, or checkpoint-based

### UI Reintegration

- [ ] reconnect menu flow on top of stable gameplay runtime
- [ ] verify UI does not introduce warning noise into gameplay sessions
- [ ] verify gameplay can boot without menu-side side effects

## Frozen During Rewrite

- [-] expanding `MiningController`
- [-] growing hit-based crack logic before instant dig is stable
- [-] adding new gameplay responsibilities to `ChunkManager`
- [-] patching problems with one-off scene hacks

## Ready-To-Start Focus

If we start implementation now, the first checklist items to attack are:

1. choose the single world authority
2. remove test-only and broken scene wiring
3. clean diagnostics and UI warning noise

