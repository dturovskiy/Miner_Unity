# Script Inventory

## Purpose

This document separates script files into four practical groups:

1. safe delete candidates
2. keep, even if they are not referenced directly by scenes or prefabs
3. freeze for reference only
4. rewrite-in-place candidates

The goal is to keep the project tree clean without deleting runtime pieces that are still needed through code.

## Safe Delete Candidates

These files are legacy or orphaned in the current architecture.
They are not part of the new runtime path and are not referenced by production scenes.

Delete candidates:

1. `Assets/GameController.cs`
2. `Assets/Scripts/Other/ConfigChanger.cs`
3. `Assets/Scripts/Other/LevelConfig.cs`
4. `Assets/Scripts/ScreenController/ResolutionController.cs`
5. `Assets/Scripts/Other/SaveLoadSystem/SaveableBehavior.cs`
6. `Assets/Scripts/Other/SaveLoadSystem/SavingService.cs`
7. `Assets/Scripts/Other/SaveLoadSystem/TransformSaver.cs`
8. `Assets/OtherAssets/TEST/SaveLoadSystem.cs`

Why these are safe candidates:

1. `GameController.cs` only talks to the legacy save stack
2. `ConfigChanger.cs` only talks to `LevelConfig.cs`
3. `ResolutionController.cs` has no current scene or code integration
4. the `Other/SaveLoadSystem` stack is now replaced by `GamePersistenceService` plus `GameSaveData`
5. `Assets/OtherAssets/TEST/SaveLoadSystem.cs` is test-only and the remaining YAML hit comes from `Assets/_Recovery/0.unity`, not from production scenes

Optional non-script cleanup:

1. `Assets/_Recovery/0.unity`

If you do not need Unity's recovery artifact, it can be removed too.

## Keep Even Without Direct Scene Refs

These files may not appear as direct MonoBehaviour refs in scenes or prefabs, but they are required by code, inheritance, enums, serialization, or runtime composition.

Keep:

1. `Assets/Scripts/GameRuntime/GamePersistenceService.cs`
2. `Assets/Scripts/GameRuntime/GameSaveData.cs`
3. `Assets/Scripts/GameRuntime/WorldRuntime.cs`
4. `Assets/Scripts/GameRuntime/WorldCellCoordinates.cs`
5. `Assets/Scripts/Diagnostics/Runtime/Diag.cs`
6. `Assets/Scripts/Diagnostics/Runtime/DiagRecord.cs`
7. `Assets/Scripts/Diagnostics/Runtime/DiagSessionInfo.cs`
8. `Assets/Scripts/Diagnostics/Runtime/IDiagContextProvider.cs`
9. `Assets/Scripts/Diagnostics/Runtime/IDiagSnapshotProvider.cs`
10. `Assets/Scripts/SceneSystem/SceneData.cs`
11. `Assets/Scripts/UI/HudElement.cs`
12. `Assets/Scripts/Scenes/MainScene/World/WorldCellFlags.cs`
13. `Assets/Scripts/Scenes/MainScene/World/WorldCellRules.cs`
14. `Assets/Scripts/Scenes/MainScene/World/WorldCellType.cs`
15. `Assets/Scripts/Terrain/StoneGravityService.cs`
16. `Assets/Scripts/Terrain/TileID.cs`
17. `Assets/Scripts/Terrain/WorldData.cs`
18. `Assets/Scripts/Scenes/MainScene/TerrainGenerator/OreClass.cs`
19. `Assets/Scripts/Scenes/MainScene/TileBehaviour.cs`

Why they stay:

1. some are plain data types or enums
2. some are runtime-only helpers created from code
3. some are base classes or interfaces
4. some are used indirectly through prefabs, `Resources`, or `AddComponent`

## Freeze For Reference

These files are not the target architecture, but they still help as reference until the rewrite reaches that area.

Freeze:

1. `Assets/Scripts/Scenes/MainScene/MiningController.cs`
2. `Assets/Scripts/Scenes/MainScene/Crack.cs`
3. `Assets/Scripts/Scenes/MainScene/TileBehaviour.cs`

Notes:

1. `MiningController.cs` is not wired into production runtime now
2. `Crack.cs` and the hit-based `TileBehaviour` path still exist for the old crack flow
3. do not expand these files unless we explicitly decide to revive that path

## Rewrite-In-Place Candidates

These files already have names that can fit a clean future role, so rewriting them in place is reasonable.

Good rewrite-in-place candidates:

1. `Assets/Scripts/Scenes/MainScene/LadderBehaviour.cs`
2. `Assets/Scripts/Scenes/MainScene/World/WorldGridService.cs`
3. `Assets/Scripts/Scenes/MainScene/Hero/HeroController.cs`
4. `Assets/Scripts/Scenes/MainScene/Hero/HeroCollision.cs`

How to think about them:

1. `LadderBehaviour.cs` can remain the per-ladder data or anchor component
2. `WorldGridService.cs` is already being rewritten into a thin facade
3. `HeroController.cs` can be shrunk over time, but its final responsibility should still match its name
4. `HeroCollision.cs` can be narrowed until it becomes a clean sensor host

## Better To Rename Than Reuse Blindly

These files could be reused technically, but the names do not match the clean target role.

Prefer rename or replacement later:

1. `Assets/Scripts/Scenes/MainScene/MiningController.cs`

Reason:

1. if the final feature is truly hero-scoped mining, the clean name is still `HeroMining`
2. reusing `MiningController` without renaming would keep legacy meaning in the codebase

## Current Recommendation

If you want to clean directories now, the safest manual delete batch is:

1. `Assets/GameController.cs`
2. `Assets/Scripts/Other/ConfigChanger.cs`
3. `Assets/Scripts/Other/LevelConfig.cs`
4. `Assets/Scripts/ScreenController/ResolutionController.cs`
5. `Assets/Scripts/Other/SaveLoadSystem/SaveableBehavior.cs`
6. `Assets/Scripts/Other/SaveLoadSystem/SavingService.cs`
7. `Assets/Scripts/Other/SaveLoadSystem/TransformSaver.cs`
8. `Assets/OtherAssets/TEST/SaveLoadSystem.cs`

After that, keep `MiningController.cs` as frozen reference until we actually introduce the clean mining feature.
