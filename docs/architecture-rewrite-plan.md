# Architecture Rewrite Plan

## Purpose

This document is the canonical roadmap for the gameplay rewrite.

We use it to answer four questions:

1. what problems are already verified
2. what architecture we are moving toward
3. what stage we are currently in
4. what is allowed or blocked during implementation

## Companion Docs

Use these documents together:

1. [Rewrite Master Checklist](./rewrite-master-checklist.md)
2. [Runtime Ownership](./runtime-ownership.md)
3. [Scene Cleanup Checklist](./scene-cleanup-checklist.md)
4. [План очистки та реструктуризації](./cleanup-restructure-plan.md)

## Current Status Snapshot

Completed and verified:

- [x] architecture audit completed
- [x] gameplay and world runtime review completed
- [x] diagnostics review completed
- [x] scene, UI, save, and legacy wiring review completed
- [x] initial rewrite roadmap created

In progress:

- [x] Stage 0 cleanup implementation started
- [x] initial scene wiring cleanup applied
- [x] initial diagnostics cleanup applied
- [x] single world authority decision locked
- [x] initial root save/runtime foundation implemented
- [x] runtime mutation path documented and partially migrated
- [x] manual smoke test passed for menu boot, world restore, fog restore, and hero movement
- [x] legacy `SaveLoadSystem` objects removed from production scenes
- [x] gameplay cell conversion centralized into one shared coordinate contract
- [x] script folders and data assets reorganized into cleaner runtime-oriented groups

Not started yet:

- [ ] root save model implementation finished
- [ ] single world authority implementation finished
- [ ] Stage 1 ground core refactor
- [ ] Stage 2 mining rewrite
- [ ] Stage 3 ladder rewrite
- [ ] Stage 4 save and UI reintegration

## Verified Baseline

These items are treated as verified facts, not guesses:

1. `WorldGridService` and terrain runtime do not use the same coordinate model in `MainScene`.
2. The project currently maintains more than one mutable world representation.
3. Terrain mutation does not always update every runtime representation consistently.
4. `MainScene` and `MenuScene` still contain legacy or test wiring that should not be part of clean gameplay runtime.
5. UI animation warnings currently add noise to diagnostics.
6. `Ground Core` looks close to usable physically, but its logical cell layer is not yet trustworthy.
7. Diagnostics confirm both the coordinate drift and the amount of non-gameplay warning noise during a normal session.

## Latest Smoke Test

Latest verified manual pass:

1. `New Game` from menu works
2. `MainScene` boots without runtime failure
3. hero movement still works after the runtime/save migration
4. hero position restores on `Continue`
5. world state and fog state restore correctly

Known non-blocking note:

1. hero move speed currently feels too high, but this is tuning, not an architecture blocker

## Independent Verification Status

Three separate reviews re-checked the rewrite direction:

1. gameplay and world runtime review
2. diagnostics and log reliability review
3. scene wiring, UI, save, and legacy/test review

All three reviews agreed on the same rewrite baseline:

1. the coordinate mismatch is real
2. world state ownership is currently split
3. the movement core is healthier than the world-query layer around it
4. diagnostics are already helpful, but too noisy
5. scene cleanup must happen before gameplay expansion

## Rewrite Goal

The product goal is:

1. stable ground movement
2. isolated mining logic
3. isolated ladder logic

The engineering goal is:

1. one runtime source of truth for the world
2. small feature-focused hero components
3. one root save model for the game
4. clean separation between domain logic, view logic, scene wiring, persistence, and diagnostics

## Non-Negotiable Decisions

These rules stay in effect during the rewrite:

1. there must be one authoritative runtime model for world cells
2. all gameplay cell queries must use the same coordinate contract
3. gameplay mutation must go through one world-facing API
4. `WorldData` remains the mutable world state, and `WorldRuntime` is the only gameplay API over it
5. `GameSaveData` is the root save model for world, hero, and progression state
6. scene buttons and menu helpers must not drive gameplay decisions
7. view systems must react to domain state, not own it
8. hero features must be separated by responsibility
9. diagnostics must stay useful enough to explain gameplay transitions

## Target Runtime Shape

### World Layer

Introduce or reshape a single world-facing runtime service.

Suggested name:

`WorldRuntime`

Suggested responsibility set:

1. bootstrap world state from generation or save
2. expose cell queries
3. validate digging
4. validate ladder interaction
5. own stone support logic and scheduled stone state
6. publish world change events
7. own persistence timing policy

Locked decision:

1. `WorldData` remains the low-level mutable world state
2. `WorldRuntime` becomes the only gameplay mutation and query boundary over `WorldData`
3. `GameSaveData` becomes the root persistence model for world, hero, and progression

`WorldGridService` should either:

1. become a thin facade over the runtime, or
2. be removed once the runtime owns all gameplay queries

### Hero Layer

Hero logic should be split by feature:

1. `HeroMotor`
2. `HeroGroundSensor`
3. `HeroWallSensor`
4. `HeroState`
5. `HeroMining`
6. `HeroLadder`

Target ownership:

1. `HeroMotor` handles velocity, facing, and motion application
2. `HeroGroundSensor` answers grounded and support questions
3. `HeroWallSensor` answers horizontal blocking questions
4. `HeroState` stores locomotion mode only
5. `HeroMining` selects targets and requests dig actions only
6. `HeroLadder` owns ladder mode entry, movement, and exit only

### View Layer

View-facing systems stay view-facing:

1. `ChunkManager` materializes visible cells and view updates
2. crack visuals stay visual only
3. stone warning effects stay visual only
4. UI stays presentation only

`ChunkManager` should stop being a second gameplay mutation authority.

### Diagnostics Layer

Diagnostics stay in the project, but with stricter scope:

1. boot and runtime readiness
2. hero movement and grounding
3. hero mining
4. hero ladder
5. world mutations
6. stone lifecycle

Known scene and UI warning spam should not bury gameplay events.

## What We Keep, Freeze, Remove, And Introduce

Keep:

1. `WorldData` as the likely low-level world storage base
2. `HeroState` as a small locomotion state holder
3. `ChunkManager` view responsibilities
4. diagnostics as a concept

Freeze:

1. `MiningController`
2. hit-based tile cracking flow
3. ad hoc gameplay rules added directly inside `ChunkManager`
4. menu-driven callbacks that affect gameplay runtime

Remove from production scenes:

1. test-only `SaveLoadSystem`
2. stale persistent callbacks pointing to missing methods
3. unsupported UI animation transition setups that generate warning spam

Introduce:

1. `WorldRuntime`
2. `GameSaveData`
3. `HeroMotor`
4. `HeroGroundSensor`
5. `HeroWallSensor`
6. `HeroMining`
7. `HeroLadder`
8. a cleaner boundary between scene loading and save loading

## Roadmap

## Stage 0 - Cleanup And Safety Rails

Status:

`In progress`

Goals:

1. remove runtime ambiguity
2. remove scene noise
3. make diagnostics trustworthy
4. lock the runtime ownership model

Work packages:

1. fix the gameplay coordinate system so terrain runtime and logical cell queries use the same grid
2. choose the single world authority
3. isolate or remove legacy and test objects still wired in scenes
4. remove or fix noisy UI animation trigger warnings
5. separate menu navigation concerns from save concerns
6. introduce the root save model for world, hero, and progression
7. document runtime ownership
8. reduce diagnostics noise and duplication

Exit criteria:

1. no coordinate mismatch between hero cell logic and terrain cell logic
2. no scene objects call missing methods
3. diagnostics session starts cleanly without UI warning spam
4. a single world authority is chosen and documented
5. production scenes no longer depend on test-only objects

Current blockers before Stage 1:

1. menu navigation is still coupled to save reset in `PlayButton`
2. diagnostics still produce too many `Heartbeat` events to be considered clean
3. hero logs still contain readiness-era duplication that makes early boot traces harder to read

## Stage 1 - Ground Core

Status:

`Blocked by Stage 0`

Goals:

1. stable horizontal movement
2. stable grounded detection
3. stable falling detection

Work packages:

1. separate motor logic from sensing logic
2. make locomotion state transitions depend only on trusted sensors
3. keep logs focused on input, grounding, blocking, and locomotion state

Exit criteria:

1. hero does not start airborne without cause
2. hero stands on solid cells consistently
3. hero falls only when support is absent
4. hero does not move through solid cells on X

## Stage 2 - Mining

Status:

`Blocked by Stage 1`

Goals:

1. add mining without changing ground core behavior
2. make digging go through world runtime only

Work packages:

1. add `HeroMining`
2. select target cell from current hero cell and dominant input axis
3. validate diggability via world runtime
4. apply dig through a single world mutation API
5. log `DigStarted`, `DigBlocked`, `DigCompleted`

Exit criteria:

1. only mineable cells can be dug
2. cell destruction updates runtime and view consistently
3. ground movement behavior remains unchanged

## Stage 3 - Ladder

Status:

`Blocked by Stage 1`

Goals:

1. clean ladder entry and exit
2. separate ladder mode from motor and mining mode

Work packages:

1. add `HeroLadder`
2. detect ladder availability through runtime and ladder sensors
3. define vertical movement rules
4. define side exit and top or bottom exit rules
5. log `LadderEntered`, `LadderExited`, `LadderBlocked`

Exit criteria:

1. ladder mode is explicit
2. ladder transitions are deterministic
3. ladder logic does not break ground logic

## Stage 4 - Save, UI, And Secondary Systems

Status:

`Blocked by Stages 0 through 3`

Goals:

1. reconnect save and load only after gameplay runtime is stable
2. reconnect UI without polluting gameplay architecture

Work packages:

1. decide whether current save and load code is retained, rewritten, or partially removed
2. move save policy to runtime boundaries instead of UI callbacks
3. reconnect menu and scene flow on top of stable gameplay runtime

Exit criteria:

1. save and load no longer decide gameplay state transitions
2. menu flow does not produce gameplay warning noise
3. scene navigation and persistence have separate responsibilities

## Implementation Rules

During the rewrite:

1. do not add new gameplay behavior to `MiningController`
2. do not add new gameplay rules to `ChunkManager`
3. do not add new scene callbacks that directly mutate gameplay state
4. do not patch over coordinate drift with local offsets
5. do not add mining or ladder before Stage 0 exit criteria are met
6. do not trust world-query results until runtime readiness is explicit

## Definition Of Done By Layer

World runtime is done for a stage when:

1. there is one mutation path
2. cell queries are deterministic
3. runtime readiness is explicit

Hero runtime is done for a stage when:

1. each feature owns one responsibility
2. state transitions can be explained by logs and sensors
3. unrelated features do not couple through one large controller

Scene cleanup is done for a stage when:

1. production scenes reference only supported runtime objects
2. there are no missing-method callbacks
3. boot logs are readable

## Recommended Working Order

1. finish Stage 0 scene and diagnostics cleanup
2. choose and implement world authority
3. refactor ground core on top of that runtime
4. add mining on top of stable ground and world runtime
5. add ladder on top of stable ground and world runtime
6. reconnect save and UI last

## Immediate Next Step

Start Stage 0 with the scene cleanup and world ownership checklist, then lock the single-world-runtime decision before writing new gameplay features.

