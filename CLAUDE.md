# CLAUDE.md — Crumble: Idle Relics

Guidance for Claude Code when working in this repository.

## Project Summary

**Crumble: Idle Relics** — a vertical mobile idle/incremental game (iOS/Android, portrait 1080x1920, one-handed play). The player taps ancient tablets to break them, earns Antique Coins, buys tools (click damage) and assistants (passive DPS), then Prestiges for Knowledge Points (KP) spent in a 4-branch prerequisite Research Tree. Endgame: Hard Prestige ("Cosmic Archive") wipes everything including the tree for Time Crystals (infinite multipliers). 16-bit pixel art, heavy juice (screen shake, particles). No forced ads.

The full Game Design Document lives in the project owner's head/chat history; the approved architecture plan is the source of truth for build order.

- **Engine:** Unity 6000.5.3f1, 2D URP template
- **UI:** UGUI (Canvas) — NOT UI Toolkit
- **Input:** New Input System (multi-touch taps, swipes)
- **JSON:** Newtonsoft (`com.unity.nuget.newtonsoft-json`) — NOT JsonUtility (save data contains Dictionaries)

## Hard Rules (never violate)

1. **All economy math is `BigDouble`** (BreakInfinity.cs, in `Assets/_Game/Plugins/BreakInfinity/`). Currency, tablet HP, damage, DPS, costs, multipliers — never `float`, `int`, or bare `double` for these values.
2. **Formulas live only in `GameMath`** (static class). Never inline cost/HP/KP formulas elsewhere:
   - Upgrade cost: `Cost = BaseCost × GrowthFactor^Level` (~1.07 tools, ~1.15 assistants); bulk-buy uses the geometric-series sum.
   - Tablet HP: `HP = BaseHP × DifficultyFactor^Stage`.
   - Prestige KP: cube-root formula (anti-inflation).
3. **Logic never touches UI.** Managers raise events on the static `GameEvents` bus (`OnCoinsChanged`, `OnTabletDamaged`, `OnTabletShattered`, `OnPrestige`, …); UI components only subscribe (and unsubscribe in `OnDisable`/`OnDestroy`). UI may call manager public methods (e.g. `UpgradeManager.TryBuy`), but managers never reference UI.
4. **Content is ScriptableObjects; saves store state only.** Tablet materials, tools, assistants, research nodes, artifacts are SO assets balanced in the Inspector. The save file stores levels/HP/timestamps keyed by stable string IDs — never definitions. This keeps saves valid across game updates.
5. **Saves are versioned, atomic Newtonsoft JSON.** `SaveData` has a `version` int + migration hook. Write to temp file then swap; keep a `.bak` fallback. `BigDouble` serializes as a **string** (custom `JsonConverter`) to avoid precision loss. `last_login_timestamp` (UTC) drives offline progress.
6. **Managers are Singletons** on a persistent bootstrap object (`DontDestroyOnLoad`), one class per file.
7. **Mobile performance:** object-pool anything spawned per tap (floating damage numbers, particles). No per-frame allocations in the tap/tick path.

## Folder Layout

All game content under `Assets/_Game/` (keeps it separate from template assets):

```
Assets/_Game/
  Scripts/Core/      GameManager, GameEvents, SaveManager, SaveData
  Scripts/Math/      GameMath, NumberFormatter
  Scripts/Data/      ScriptableObject definitions (TabletMaterialSO, ToolSO, AssistantSO, ResearchNodeSO, ...)
  Scripts/Gameplay/  TabletManager, CurrencyManager, UpgradeManager, PrestigeManager, ResearchManager, ...
  Scripts/UI/        HUD, panels, tree view (subscribe-only)
  Plugins/BreakInfinity/
  Data/              SO asset instances (Tablets/, Tools/, Assistants/, Research/)
  Scenes/            Main.unity
  Tests/EditMode/    NUnit tests (GameMath, save round-trip)
```

## Build Roadmap (current step marked)

1. ✅ **Foundation** — folders, BreakInfinity, Newtonsoft, GameEvents, GameMath + NumberFormatter (+41 tests), SO definitions, GameManager/SaveManager skeleton, Main.unity with _Bootstrap
2. ✅ Core loop — TabletManager (5 crack visual states), tap damage, CurrencyManager, minimal portrait HUD, placeholder art generator, milestone "boss" tablets (final stage per material: 2× HP, 3× reward, tint + "(Hard)" label), 64 tests
3. ✅ Economy — 12 tools + 12 assistants (SO assets + icons), UpgradeManager, DPS tick, tabbed buy panel with x1/x10/MAX
4. ✅ Prestige + KP — PrestigeManager, live cube-root KP preview button, confirm dialog, run wipe keeping KP (GameLoaded rebind flow), 67 tests
5. ✅ Research Tree — 60 ResearchNodeSO (4 branches × 15 stages), ResearchManager (prereq graph, KP purchases, effect aggregation into damage/DPS/coins/costs), 4-tab panel with lock/silhouette rules, 70 tests
6. ✅ Offline progress — OfflineProgressManager (DPS × capped time, trickle + amortized shatters), welcome-back popup with COLLECT / COLLECT x2 (rewarded ad via AdManager stub), OfflineEfficiency + OfflineCapHours research live, 74 tests
7. ✅ Juice — FeverManager (25-tap combo → 5× clicks for 10s+research, decay), fever bar UI, CameraShake (shatter/milestone/fever), tinted shatter+dust particles (manual Emit, pooled), tap punch-scale, Haptics wrapper
8. ➡️ Side systems — Expedition Tent, Museum & Artifacts, night cycle + sandstorm events
9. Endgame — Cosmic Archive Hard Prestige, Time Crystals, Cosmic Altar
10. Ship pass — safe-area, pooling audit, rewarded ads/IAP hooks

Update the ➡️ marker when a step is completed.

## Verification

- Run EditMode tests (Unity Test Runner) after any change to `GameMath`, `NumberFormatter`, or save code.
- Dev tools: `Crumble → Dev → Reset Save` wipes the save (live-resets when playing); `Crumble → Run EditMode Tests` writes results to `Temp/crumble_test_results.txt`.
- Use Unity MCP tools: enter Play Mode, exercise the changed loop (tap / buy / prestige), then check `Unity_GetConsoleLogs` for errors and capture the Game view for UI checks.
- A change isn't done until the project compiles with zero console errors and a save round-trips (save → reload → identical state).
