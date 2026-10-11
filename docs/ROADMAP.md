# Roadmap (vertical slices, not a promise of release dates)

## PR 1 — Core extraction foundation
- [x] Weight/value/stamina/strength domain model
- [x] Bank, fail, per-item and bulk selling
- [x] Luck/profit/automation basic upgrade effects
- [x] Headless C# rules tests + CI
- [x] Graybox Unity 2D scene and mouse dragging
- [x] Prototype save/load and deterministic loot identities
- [ ] Unity editor compile and manual Windows/macOS playtest (requires editor)

## PR 2 — Physics and session correctness
- [ ] Collision resistance and force-based dragging; prevent lightweight pushes exploiting heavy objects
- [ ] Camera/layout input testing at different resolutions; trackpad and accessibility
- [ ] Scene/integration tests in Unity Test Framework
- [ ] Balance stamina, weight, velocity and risk with measured playtests
- [ ] Validate crash recovery and save migration

## PR 3 — Real artwork & HUD
- [ ] Recreate approved **flat hand-drawn 2D** drawer/objects/UI
- [ ] Replace OnGUI with UGUI or UI Toolkit
- [ ] Detailed item tooltips, extraction animation and tactile audio
- [ ] Distinct sale, catalog, failure and upgrade screens

## PR 4 — Upgrade tree and automation
- [ ] Linked nodes and prerequisites for all five upgrade branches
- [ ] Multiple machines, independent charges and weight capacity
- [ ] Simulations/tests for balance and optimal strategies

## PR 5 — Depth, mystery and release pipeline
- [ ] Chapters, layered drawer transitions and scripted finale
- [ ] Persistent collection journal for all sellable objects
- [ ] Windows/macOS builds, platform smoke tests and distribution
- [ ] Accessibility, onboarding and release QA
