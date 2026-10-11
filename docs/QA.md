# Prototype QA checklist

## Automated (GitHub Actions, no Unity license needed)

Run `dotnet run --project Tests/BottomDrawer.Core.Tests.csproj --configuration Release`.
The suite exercises immutable base price→weight mapping, heavier items taking more stamina/s moving slower, fixed-rate drain independent of Strength, state-machine transitions, forfeiture, safe banking, sale idempotency, deterministic slots, chapter-locked anomalies and restore semantics.

## Unity Editor manual smoke (Windows and macOS)

1. Open the repository in Unity **6000.0.45f1**. Wait for compilation to complete with **zero Console errors**. If Unity shows legacy Input disabled, set **Active Input Handling** to **Both** or **Input Manager (Old)**.
2. Run **The Bottom Drawer → Create Or Open Prototype Scene**; press Play.
3. Click **Start Session**; observe ten colored item shapes in the drawer.
4. Drag small (light) and large (heavy) objects to the **green, bottom-center extraction tray**. Confirm that movement speed and stamina/second differ with weight, and that letting go stops drain.
5. Extract two objects, click **Bank & End Run**; sell one then the other. Wallet must increase **exactly once** per item.
6. Buy **Strength I** and **Stamina I**. Start a new run: capacity must increase by 20, while at the same weight Strength must raise movement speed without reducing stamina drain/second.
7. Bank and sell enough items to buy Luck, Profit and Automation. Verify new slots use Luck, sale multiplier applies only to receipts, and Space auto-collect works with limited charges and max weight.
8. In a run, collect something, then hold a heavy item until stamina hits zero. The run must fail with **zero bankable loot**, and previously banked coins/upgrades must remain.
9. In a run, bank some items and close/reopen Unity. Confirm banked loot remains sellable. Quit **mid-run**; after restarting it must be marked failed.
10. Reopen several times without new extraction: item identities should persist; no fresh rarity rolls just from restarting the game.
11. Test at 1280×720 and 1920×1080; test click targeting, dragging and unobstructed UI. Run a standalone Windows build and macOS Universal build when platforms/build support are installed.

## Known prototype limitations

- **Not a release gate yet:** Unity Editor compilation and standalone builds are not automated in CI (Unity editor/license/runners required).
- No final artwork, sounds, tutorial, chapter transitions or animated depth.
- No formal frame-level physics tests. Directly held Rigidbody2D objects use simplified MovePosition; physically accurate push/impulse cost is a separate workstream.
- No production-grade cross-version save migrations. Saves are local experimental data.
- Current prototype uses polling via Unity legacy Input API and immediate-mode OnGUI; both should be replaced/polished later.
- No code signing/notarization or macOS Gatekeeper end-user test.
