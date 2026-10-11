# The Bottom Drawer

A free, offline, cozy-mystery 2D extraction / incremental game for **Windows and macOS**.

> **Development status: first playable graybox prototype.** Art, scene layout, balancing, physics, story and platform builds are NOT release ready.

## Setup

1. Install **Unity 6 (6000.0.45f1)** with Windows and/or macOS build support.
2. Clone this repository and open the **repository root** as a Unity project.
3. Let Unity import C# scripts and resolve packages.
4. Choose **The Bottom Drawer → Create Or Open Prototype Scene** in the Unity menu.
5. Press **Play**. The scene spawns temporary 2D shapes in an antique-color palette.

The desktop prototype intentionally uses Unity's legacy immediate-mode `OnGUI` as temporary UI; the approved hand-drawn UI and sprite artwork have not been implemented yet.

## Prototype controls

- **Start Session** in left panel begins an extraction run.
- **Hold left mouse** over an item and drag to the **bottom center tray**.
- Stamina drains **continuously while dragging**. More valuable items have higher weight and drain more.
- **Bank & End Run** safely ends the run. Sell findings individually or use Sell All.
- Reaching zero stamina fails the run and **forfeits unbanked loot**; existing coins/upgrades remain.
- Between runs, open **Upgrades** and choose Strength, Stamina, Luck, Profit or Automation.
- With Automation purchased, press **Space** to activate its limited-capacity auto collector.
- The save file is placed under `Application.persistentDataPath/prototype-save.json`.

## Core gameplay invariants

- **Price → weight:** intrinsic weight is always strictly increasing with *base* item price: `0.25 + 0.015 × basePrice`.
- Profit upgrades improve sale receipts, **not** intrinsic item weight.
- Strength improves drag speed, **not stamina drain per second**.
- The real-time drain depends on weight and time held. Let go to stop the drain.
- Only explicitly extracted items count as loot; collisions alone do not grant free extraction.
- Banked loot may be sold one by one, or all at once, exactly once.
- Quitting/restarting during active extraction counts as a failure, preventing run resets from preserving unbanked loot.
- Already spawned items retain identity and rarity between runs/restarts; Luck affects only newly generated items.

## Headless gameplay tests

The game domain (under `Assets/Scripts/Core`) has **no UnityEngine dependency** and is compiled in CI by a small zero-NuGet test runner:

```sh
dotnet run --project Tests/BottomDrawer.Core.Tests.csproj --configuration Release
```

The test runner covers price/weight monotonicity, drag math, session state transitions, forfeiture, banking, selling, upgrade behavior, persistence restoration behavior and deterministic loot. GitHub Actions runs it on every PR and feature branch push.

**Not yet verified:** actual Unity Editor compilation, physical drag feel, macOS notarization, Windows/macOS builds. These require platform-specific Unity editor/build checks before release.

## Git workflow

All work is done on focused `feat/*`, `fix/*`, `chore/*` branches via pull requests. Protect `main` from unreviewed direct changes. Unit/logic checks are required before merging; Unity smoke tests and desktop builds are separate gates when the project gains dedicated build infrastructure.

See [docs/GDD.md](docs/GDD.md) for approved gameplay rules and [docs/ROADMAP.md](docs/ROADMAP.md) for the next milestones.
