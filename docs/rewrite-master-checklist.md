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
- [x] logging and observability checklist created
- [x] Stage 0 execution started
- [x] manual smoke test passed for `New Game`, `Continue`, world restore, fog restore, and basic hero movement
- [x] script layout cleanup applied without breaking the local build
- [ ] visibility and fog of war design documented and staged
- [x] Stage 0 completed
- [x] Stage 1 completed
- [x] Stage 2 completed
- [x] Stage 3 completed
- [ ] observability hardening pass before Stage 4
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

- [x] introduce `HeroMotor`
- [x] introduce `HeroGroundSensor`
- [x] introduce `HeroWallSensor`
- [x] shrink or replace `HeroController`
- [x] keep `HeroState` locomotion-only

### Behavior

- [x] hero does not start airborne without cause
- [x] hero stands on solid cells consistently
- [x] hero falls only when support is absent
- [x] hero does not move through solid cells on X

### Logging

- [x] locomotion logs are limited to useful gameplay events
- [x] movement blocking is explained by deterministic sensor results
- [x] grounded changes are easy to read in a short log session
- [-] monitor `StateChanged` and `MoveBlocked` volume in long stress sessions; current counts are not a blocker while events remain deterministic

## Stage 2 - Mining

### Structure

- [x] introduce `HeroMining`
- [x] target selection is driven by the same joystick that drives movement
- [x] mining target selection depends on contact or near-contact rules, not on free button directions
- [x] mining reads from runtime instead of scene helpers
- [x] hit progress is owned by runtime or hero mining logic, not by spawned tile views
- [x] crack rendering is view-only and follows runtime hit progress
- [x] hero mining animation is driven by mining intent and hit loop, not by separate scene hacks
- [x] mining writes through one world mutation API

### Behavior

- [x] only mineable cells can be dug
- [x] hero starts mining when joystick direction points into a valid nearby block
- [x] side mining works when the hero presses into a neighboring wall block
- [x] downward mining works when the hero is close enough to the floor block below
- [x] upward mining works when the hero presses toward a nearby ceiling block
- [x] mining does not fire while the hero is simply walking with no valid target
- [x] default pickaxe breaks a dirt block in 4 hits
- [x] crack stages update consistently during the hit loop
- [x] partial mining damage persists when the hero leaves the block
- [x] partial mining damage survives save and load
- [x] tool power can change required hits without changing mining flow
- [x] blocked digs report why they were rejected
- [x] runtime and view stay in sync after a dig
- [x] movement behavior remains unchanged while mining is added

### Logging

- [x] `DigStarted` exists and is meaningful
- [x] `DigHit` exists and shows target, hit index, and crack stage
- [x] `DigBlocked` exists and explains the block reason
- [x] `DigCompleted` exists and reflects a real world mutation

## Stage 3 - Ladder

### Structure

- [x] introduce `HeroLadder`
- [x] ladder entry uses sensors and runtime queries
- [x] ladder mode is explicit in hero runtime

### Behavior

- [x] vertical ladder movement is deterministic
- [x] side exit is deterministic
- [x] top exit is deterministic
- [x] bottom exit is deterministic
- [x] ladder logic does not break ground logic

### Logging

- [x] `LadderEntered`
- [x] `LadderExited`
- [-] `LadderBlocked` is deferred for now; recent verified sessions did not need a gameplay ladder-block event, while placement rejections are already covered by `PlaceLadderBlocked`

### Stage 3 Close-Out

- [x] latest verified session logs show deterministic ladder climb, top exit, bottom exit, and side interaction with no `Unity/Error` or `Unity/Warning`
- [x] upward mining now coexists correctly with ladder usage when the hero is inside the ladder column and when standing on the top of the ladder
- [x] recent sessions show matched `LadderEntered` and `LadderExited` counts with no re-entry spam loop
- [-] monitor occasional `DigBlocked reason=outsideMiningArea` when the hero steps into open cells beyond the cave edge after digging; current behavior is not blocking Stage 3

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

## Visibility, Fog, And Minimap Track

### Structure

- [ ] separate permanent discovery from temporary live visibility
- [ ] move fog rules out of `ChunkManager` into a dedicated visibility service or runtime subsystem
- [ ] define how lantern level maps to visibility radius

### Behavior

- [ ] visibility works in mined `Empty` cells, `Tunnel`, and future `Ladder` cells
- [ ] discovered cells persist for minimap usage
- [ ] current live visibility follows the hero without depending on `TileID.Tunnel`
- [ ] reveal updates after movement, digging, and future object placement

### Save And Rendering

- [ ] save data keeps permanent discovery separately from transient lighting state
- [ ] `HiddenArea` acts as view only and does not own discovery logic
- [ ] minimap reads discovered cells instead of current temporary light radius
- [ ] tunnel background rendering is separated from logical open-space tiles
- [ ] full tunnel and half-tunnel background variants are driven by local vertical gap rules

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

