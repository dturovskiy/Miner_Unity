# Runtime Ownership

## Ownership Rule

There must be exactly one authoritative gameplay runtime for world cells.

Recommended ownership model:

1. `GameSaveData` is the root persistence model for the whole game
2. `WorldData` remains the low-level mutable world storage model
3. `WorldRuntime` becomes the only gameplay mutation and query authority over `WorldData`
4. everything else either reads from `WorldRuntime` or reacts to its events

## Current System Assessment

| System | Current role | Current problem | Target role | Action |
| --- | --- | --- | --- | --- |
| `GameSaveData` | not implemented yet | there is no single root save model for world, hero, and progression | root persistence model | introduce |
| `WorldData` | terrain cell storage | currently shares authority with other mutable state | low-level cell storage under runtime ownership | keep |
| `WorldGridService` | second cell grid for gameplay queries | diverges from terrain runtime and uses conflicting coordinates | thin facade or removed entirely | refactor or remove |
| `TerrainToGridBootstrap` | copies terrain data into gameplay grid | exists because world state is duplicated | bootstrap runtime readiness only, or remove | remove or replace |
| `ChunkManager` | loads tiles, mutates terrain, updates view, saves | mixes view responsibilities with gameplay mutation authority | view adapter that reacts to world runtime changes | refactor |
| current fog logic in `ChunkManager` | reveals `HiddenArea` around the hero | mixes permanent discovery, current visibility, and rendering in one place | move to a dedicated visibility service and leave `ChunkManager` as a view adapter | refactor |
| `StoneGravityService` | stone movement and landing logic | mutates world state outside one shared gameplay authority | world runtime subsystem or runtime-owned helper | refactor |
| `HeroController` | movement orchestration and hero behavior entry point | too broad as a long-term feature host | shrink into `HeroMotor` or be replaced by smaller components | refactor |
| `HeroCollision` | physics sensing plus world cell reads | mixes trusted physics checks with untrusted grid reads | split into `HeroGroundSensor` and `HeroWallSensor` | refactor |
| `HeroState` | locomotion state holder | small and useful already | locomotion mode only | keep |
| `MiningController` | legacy mining path | not the desired runtime architecture | frozen reference only | freeze |
| `TileBehaviour` | spawned tile behavior and crack path | current hit flow is not the target starting point | visual behavior only | freeze for now |
| `SavingService` | save logic plus scene lifecycle behavior | mixes persistence and scene flow | persistence service only | refactor later |
| `PlayButton` | menu entry logic | currently triggers save metadata side effects | UI-only scene entry | refactor later |
| `SaveLoadSystem` | old test helper | present in production scenes and tied to stale callback wiring | debug-only or removed | remove from production |
| `DiagManager` and relays | structured runtime logging | useful but noisy, with duplication and threading risk | observation layer only | refactor |
| menu button animation setup | UI transitions | generates warning spam in runtime logs | clean supported transition config | cleanup |
| `GameController` | standalone manager script | not part of active scene runtime | quarantine until needed | review later |

## Target Ownership Map

`GameSaveData` owns:

1. world save payload
2. hero save payload
3. progression save payload

It is the persistence root, not the gameplay API.

`WorldRuntime` owns:

1. all gameplay cell queries
2. all gameplay cell mutations
3. dig validation
4. ladder validation
5. stone support and scheduled stone state
6. world readiness state
7. persistence timing policy

`ChunkManager` owns:

1. visible chunk materialization
2. spawned tile object lifecycle
3. visual responses to runtime world changes
4. optional view-only stone effects
5. until refactor, temporary background spawning rules for open-space visuals

`VisibilityRuntime` or equivalent service should own:

1. permanent discovered-cell state for minimap and persistence
2. current live visibility around the hero
3. reveal rules for `Empty`, `Tunnel`, and future `Ladder` cells
4. visibility radius derived from lantern or light progression
5. visibility refresh after hero movement, digging, and placed-object changes

Background tunnel rendering should eventually be driven by:

1. world shape and neighboring solid cells
2. view-only rules for full tunnel, upper half tunnel, and lower half tunnel variants
3. a separate rendering concern, not the logical definition of passable underground space

`HeroMotor` owns:

1. motion application
2. facing
3. move intent consumption

`HeroGroundSensor` owns:

1. grounded checks
2. support checks
3. trusted foot contact data

`HeroWallSensor` owns:

1. horizontal block checks
2. side collision information

`HeroState` owns:

1. locomotion mode only

`HeroMining` owns:

1. dig input interpretation
2. target cell selection
3. dig requests to runtime

`HeroLadder` owns:

1. ladder mode entry
2. ladder movement rules
3. ladder exit rules

`SavingService` owns after Stage 4:

1. serialization and deserialization
2. save slot policy
3. save file handling

It should not own scene navigation or gameplay bootstrap decisions.

`Diagnostics` own:

1. observation
2. event formatting
3. runtime visibility

They should not own gameplay state or scene flow.

## Current Validated Mutation Path

The runtime path we are actively standardizing is:

1. generation or save restore fills `WorldData`
2. `WorldRuntime` becomes the only gameplay query and mutation boundary over that `WorldData`
3. gameplay dig and place requests flow through `ChunkManager` or `WorldGridService` facade into `WorldRuntime`
4. stone movement is scheduled by `ChunkManager`, executed through `WorldRuntime`, and applied by the runtime-owned `StoneGravityService`
5. `ChunkManager` updates spawned views after the runtime mutation succeeds
6. `GamePersistenceService` serializes the current `GameSaveData` snapshot from runtime state

Current practical notes:

1. `WorldGridService` is no longer a second mutable gameplay grid; it is a compatibility facade over `WorldRuntime`
2. `ChunkManager` still coordinates view updates and save timing, but it no longer owns the world model
3. the legacy raw files remain as migration input for now, not as the primary runtime truth
4. shared world-to-cell conversion now lives in `WorldCellCoordinates`, so hero-facing and terrain-facing cell math use one code path
5. current fog reveal still lives inside `ChunkManager`, so visibility is not fully separated yet

## Keep, Freeze, Remove, Introduce

Keep now:

1. `WorldData`
2. `HeroState`
3. `ChunkManager` view responsibilities
4. diagnostics infrastructure as a concept

Freeze now:

1. `MiningController`
2. crack and hit mining path
3. direct gameplay growth inside `ChunkManager`
4. menu-side gameplay callbacks

Remove from production scenes:

1. `SaveLoadSystem`
2. stale button callbacks to missing methods
3. unsupported animation-transition button setups

Introduce next:

1. `GameSaveData`
2. `WorldRuntime`
3. `HeroMotor`
4. `HeroGroundSensor`
5. `HeroWallSensor`
6. `HeroMining`
7. `HeroLadder`
8. a dedicated `SceneLoader` boundary if scene navigation remains necessary
9. a dedicated visibility or fog-of-war runtime service before final minimap and lantern integration

## Working Policy

Until the rewrite is complete:

1. `WorldData` is the only mutable world state in memory
2. no gameplay system may mutate both a grid copy and terrain data directly
3. no scene object may become a hidden gameplay authority
4. no feature work should bypass the world runtime boundary
5. if a system is frozen, it may be read for reference but not expanded
6. `Tunnel` should not remain an overloaded concept for both gameplay passability and background art once the visibility and mining work matures
