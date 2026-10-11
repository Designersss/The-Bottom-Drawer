# The Bottom Drawer — design baseline v0.6

## Identity

Free native **Windows / macOS** indie game. Offline solo game, no accounts, online services, ads or paid features. Cozy and mysterious rather than horror. True top-down **hand-drawn 2D** (not 3D viewed from above), warm wood, parchment, brass and gold. Player remains seated; no walking character. The approved concept art is a *style reference*, not shipped art.

## Main loop

1. Open a densely packed antique wooden drawer and start an extraction session.
2. Click and continuously drag objects toward the front-center collection tray.
3. Objects have real gameplay **weight**, collision and inertia. Heavier items move slower and drain stamina faster **per second held**.
4. Choose whether to risk another extraction or **bank the entire session** safely.
5. At stamina zero, fail and lose all **unbanked** session findings. Permanent wallet, upgrades, journal and progress survive.
6. Sell **any** banked item individually or sell all; spend coins on branching upgrades.
7. New item layers gradually unlock the mystery of the impossibly deep drawer.

## Non-negotiable game balance

- Every more valuable intrinsic item must be strictly heavier than every cheaper intrinsic item.
- Baseline `weightKg = 0.25 + baseValue * 0.015` (subject to tuning, not independent per-item values).
- Higher weight lowers drag speed and raises stamina consumption/second.
- Strength increases speed at fixed weight; it **never directly lowers stamina per second**.
- Stamina improves capacity; Luck adds +2 percentage points to Rare loot probability per level; Profit increases banked sales prices; Automation unlocks machines and charges.
- Loot is generated as objects, not rolled on extraction. Existing items cannot be rerolled by repeatedly starting sessions.
- Rare loot is not guaranteed to be feasible to extract for every upgrade build: risk/strategy is deliberate.
- Paid-price promotions via Profit do not increase weight: 'intrinsic/base value' determines mass.

## Upgrade branches

| Branch | Initial prototype rule | Full game intent |
|---|---|---|
| Strength | +18% speed per level | Additional heavy-object handling |
| Stamina | +20 capacity per level | Recovery / efficiency specializations |
| Luck | +2pp Rare for new spawns | Anomalous chance and treasure discovery |
| Profit | +5% sale price per level | Appraisal, bulk trading |
| Automation | +1 charged auto extraction per level, with weight limit | Magnets, grabbers, machines with independent capacity |

Initial prototype's five flat levels are **not** the final node-and-link upgrade tree.

## Planned story chapters

1. Forgotten Things: ordinary drawer, a hint that the bottom is too deep.
2. Lost Memories: letters and photos show impossible memories.
3. Impossible Objects: gated anomalous rarity and supernatural artifacts.
4. The Bottom: a small copy of the room/table/drawer and the lost key.

Exact thresholds, the key's narrative and the ending are not implemented.

## Prototype scope and gaps

**In first PR:** data-driven loot definitions, unambiguous price/mass invariant, fixed-timestep stamina, mouse drag, safe bank/failure/sale, five upgrade effects, deterministic slot-generation, simple JSON persistence, editable scene bootstrapping, headless tests and CI.

**Not in first PR:** production sprites/fonts/audio, connected skill tree, polish and accessibility, balancing playtests, multiple depth layers, collectible journal, chapter scripting, production physics protections, final save migrations, Unity automated editor testing, signed builds.

**Review concern for later tests:** grabbing a light object and pushing a heavy object must never cause a free heavy extraction. Prototype grants extraction only for the dragged object; realistic impulse/force costs require deeper physics iteration.
