# Asset Restructure Plan

## Purpose

This document is the manual cleanup and folder-restructure plan for `Assets` and `Packages`.

It is designed for:

1. removing legacy or third-party leftovers safely
2. grouping first-party project assets into a clean structure
3. separating third-party content from project code and content

## Current Top-Level Audit

Current top-level `Assets` layout:

1. `Assets/Animations`
2. `Assets/LitJSON`
3. `Assets/Materials`
4. `Assets/MobileDependencyResolver`
5. `Assets/OtherAssets`
6. `Assets/Plugins`
7. `Assets/Prefabs`
8. `Assets/Resources`
9. `Assets/Scenes`
10. `Assets/Scripts`
11. `Assets/Sprites`
12. `Assets/TextMesh Pro`

Current top-level loose files:

1. `Assets/GameController.cs`
2. `Assets/MapPlan.md`
3. `Assets/terrain_layout.txt`

## What We Know Right Now

Safe facts from the audit:

1. `Assets/OtherAssets` currently contains only the old test `SaveLoadSystem`
2. `Assets/LitJSON` is only used by the legacy save stack that is already marked for removal
3. `Assets/MobileDependencyResolver` is editor-only Google dependency tooling and we found no project-side usage
4. `Assets/Plugins/Demigiant/DOTween` is still needed by `FadeScreenPanel` and `HudElement`
5. `Assets/TextMesh Pro` is still needed by menu UI
6. `Assets/Resources/Crack.prefab` is still needed by the old crack path because `TileBehaviour` loads it with `Resources.Load("Crack")`
7. `Assets/Resources/BillingMode.json`, `Assets/Resources/MapData.txt`, and `Assets/Resources/MapCollisions.txt` currently have no direct code references
8. `Packages/manifest.json` includes many optional Unity packages that do not show direct project usage in code

## Target Layout

Recommended target layout:

1. `Assets/_Project`
2. `Assets/ThirdParty`

Recommended first-party layout under `Assets/_Project`:

1. `Assets/_Project/Art/Animations`
2. `Assets/_Project/Art/Materials`
3. `Assets/_Project/Art/Sprites`
4. `Assets/_Project/Content/Scenes`
5. `Assets/_Project/Content/Prefabs`
6. `Assets/_Project/Content/Resources`
7. `Assets/_Project/Data/Configs`
8. `Assets/_Project/Data/Maps`
9. `Assets/_Project/Docs`
10. `Assets/_Project/Scripts`

Recommended third-party layout:

1. `Assets/ThirdParty/DOTween`
2. `Assets/ThirdParty/TextMeshPro`
3. `Assets/ThirdParty/MobileDependencyResolver`
4. `Assets/ThirdParty/LitJson`

## Delete Now

These are the safest manual deletes right now.

Delete now:

1. `Assets/OtherAssets`
2. `Assets/GameController.cs`
3. `Assets/GameController.cs.meta`
4. `Assets/_Recovery`

Delete now only together with the legacy save stack from the script inventory:

1. `Assets/Scripts/Other/SaveLoadSystem`
2. `Assets/LitJSON`

Reason:

1. once the legacy save stack is removed, `LitJSON` becomes dead weight
2. `Assets/OtherAssets` is only the old test save helper
3. `Assets/_Recovery` is an editor recovery artifact, not project content

## Delete After One Quick Reopen

These are strong removal candidates, but I would remove them in small batches and reopen Unity after each batch.

Asset-side candidates:

1. `Assets/MobileDependencyResolver`
2. `Assets/Resources/BillingMode.json`
3. `Assets/Resources/MapData.txt`
4. `Assets/Resources/MapCollisions.txt`
5. `Assets/Scripts/SceneSystem/Configs/TestScene.asset`

Why they are in this bucket:

1. `MobileDependencyResolver` looks unused, but package ecosystems can be sticky
2. `BillingMode.json` is likely a leftover from purchasing or ads setup
3. `MapData.txt` and `MapCollisions.txt` have no direct code usage in the current runtime
4. `TestScene.asset` has no confirmed production reference

## Keep

Keep these third-party areas for now:

1. `Assets/Plugins/Demigiant/DOTween`
2. `Assets/TextMesh Pro`

Keep these first-party content areas, but move them later:

1. `Assets/Animations`
2. `Assets/Materials`
3. `Assets/Prefabs`
4. `Assets/Scenes`
5. `Assets/Scripts`
6. `Assets/Sprites`
7. `Assets/Resources`

## Package Removal Candidates

These `Packages/manifest.json` entries look removable from the current project usage profile.

Likely removable:

1. `com.unity.ads`
2. `com.unity.analytics`
3. `com.unity.ai.navigation`
4. `com.unity.collab-proxy`
5. `com.unity.multiplayer.center`
6. `com.unity.purchasing`
7. `com.unity.test-framework`
8. `com.unity.timeline`
9. `com.unity.visualscripting`
10. `com.unity.xr.legacyinputhelpers`
11. `com.unity.ide.vscode`

Keep:

1. `com.unity.2d.sprite`
2. `com.unity.2d.tilemap`
3. `com.unity.ugui`
4. `com.unity.ide.visualstudio` if you still want Unity integration there

Do not manually remove built-in `com.unity.modules.*` packages unless there is a very specific reason.

## Manual Package Cleanup Order

Recommended order:

1. remove `com.unity.visualscripting`
2. remove `com.unity.multiplayer.center`
3. remove `com.unity.collab-proxy`
4. remove `com.unity.xr.legacyinputhelpers`
5. remove `com.unity.timeline`
6. remove `com.unity.ai.navigation`
7. remove `com.unity.analytics`
8. remove `com.unity.ads`
9. remove `com.unity.purchasing`
10. remove `com.unity.test-framework`
11. remove `com.unity.ide.vscode` if you do not use it

After each removal batch:

1. reopen Unity
2. let it reimport
3. open `MenuScene`
4. open `MainScene`
5. confirm there are no compile errors

## Manual Move Map

When you start moving folders, this is the cleanest target map:

1. `Assets/Animations` -> `Assets/_Project/Art/Animations`
2. `Assets/Materials` -> `Assets/_Project/Art/Materials`
3. `Assets/Sprites` -> `Assets/_Project/Art/Sprites`
4. `Assets/Prefabs` -> `Assets/_Project/Content/Prefabs`
5. `Assets/Scenes` -> `Assets/_Project/Content/Scenes`
6. `Assets/Resources` -> `Assets/_Project/Content/Resources`
7. `Assets/Scripts` -> `Assets/_Project/Scripts`
8. `Assets/MapPlan.md` -> `Assets/_Project/Docs/MapPlan.md`
9. `Assets/terrain_layout.txt` -> `Assets/_Project/Data/Maps/terrain_layout.txt`
10. `Assets/Scripts/SceneSystem/Configs/*.asset` -> `Assets/_Project/Data/Configs/Scenes`
11. `Assets/Plugins/Demigiant/DOTween` -> `Assets/ThirdParty/DOTween`
12. `Assets/TextMesh Pro` -> `Assets/ThirdParty/TextMeshPro`

Conditional move:

1. `Assets/LitJSON` -> `Assets/ThirdParty/LitJson` only if you decide to keep it
2. `Assets/MobileDependencyResolver` -> `Assets/ThirdParty/MobileDependencyResolver` only if you decide not to delete it

## Important Notes

1. do large folder moves inside Unity so `.meta` files stay attached correctly
2. do manual deletes only after the script inventory delete list is applied
3. if you want to fork the imported joystick later, the clean home is `Assets/_Project/Scripts/Input` or `Assets/_Project/ThirdPartyForks/Joystick`
4. do not move `Resources/Crack.prefab` until the old crack path stops using `Resources.Load`
