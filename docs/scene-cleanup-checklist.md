# Scene Cleanup Checklist

## Purpose

Use this checklist before expanding gameplay features.

The goal of this document is to make `MainScene` and `MenuScene` safe, quiet, and predictable enough for the rewrite.

## MainScene

- [x] audit current scene objects and callbacks
- [x] confirm test-only `SaveLoadSystem` is present in scene wiring
- [x] confirm stale callback to `RemoveDestroyedBlocksFromBinary`
- [x] confirm warning-producing button animation setup exists
- [ ] remove `SaveLoadSystem` from production scene wiring
- [ ] remove stale `RemoveDestroyedBlocksFromBinary` callback
- [ ] confirm only supported gameplay runtime objects remain
- [ ] align gameplay grid settings with runtime coordinate contract
- [ ] run a clean `MainScene` boot log and verify warning noise is gone

## MenuScene

- [x] audit current scene objects and callbacks
- [x] confirm test-only `SaveLoadSystem` is present in scene wiring
- [x] confirm warning-producing button animation setup exists
- [x] confirm `PlayButton` triggers save metadata side effects during menu flow
- [ ] remove `SaveLoadSystem` from production scene wiring
- [ ] replace unsupported button transition setup with a supported one
- [ ] remove menu-open save metadata side effects
- [ ] run a clean `MenuScene` boot log and verify warning noise is gone

## Shared Scene Decisions

- [ ] choose the supported button transition model for runtime scenes
- [ ] ensure `Button.controller` matches the chosen scene button setup
- [ ] separate scene navigation responsibility from save responsibility
- [ ] ensure production scenes contain no test-only helper objects
- [ ] ensure no scene callback points to a missing method

## Completion Condition

- [ ] `MainScene` boots without scene-wiring warnings
- [ ] `MenuScene` boots without scene-wiring warnings
- [ ] a normal session log is readable without UI warning spam
- [ ] production scenes contain no legacy test helpers
