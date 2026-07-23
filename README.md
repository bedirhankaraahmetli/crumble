# ⛏️ Crumble: Idle Relics

**A pixel-art idle/incremental archaeology game for mobile — tap ancient tablets, break them open, and dig through the strata of time.**

Built with Unity for iOS/Android (portrait, one-handed play). Currently in active development.

<p align="center">
  <img src="Docs/screenshots/gameplay.png" width="320" alt="Gameplay: tablet excavation with the upgrade panel" />
</p>

## 🎮 The Game

You run an archaeological excavation camp. Tap the screen to chip away at ancient tablets — from humble **Dried Mud** all the way to **Void Rift** at the edge of the cosmos — and earn Antique Coins from every hit and every shattered slab.

- **Active play** — tap to deal damage; every tablet visibly cracks through 5 damage states before it shatters.
- **Idle play** — hire assistants (a Water Dripper, a Clockwork Automaton, eventually an Interdimensional Portal…) that keep mining while you're away.
- **Economy** — spend coins on 12 tools and 12 assistants with exponential cost curves; buy x1, x10, or MAX in one tap.
- **Milestone tablets** — every material ends in a tougher "boss" tablet with a fat bonus reward.
- **Prestige** *(in development)* — when tablet HP outgrows your damage, reset the run for **Knowledge Points** and spend them in a 4-branch prerequisite **Research Tree**.
- **Endgame** *(planned)* — the Cosmic Archive: a hard prestige that wipes everything, including the tree, for Time Crystals and unbounded multipliers.

No forced ads — ever. Monetization will be limited to optional rewarded ads and ethical IAPs.

## ✨ What's playable today

The core loop is fully playable in-editor: tap tablets through 20 stages of 4 Tier-1 materials, hire tools/assistants from a scrollable tabbed shop, watch passive DPS mine for you, and hit milestone bosses — with progress saved between sessions.

## 🏗️ Architecture

The codebase is built to scale to the full design before any of it existed:

| Principle | Implementation |
|---|---|
| **Big numbers everywhere** | All economy math (HP, damage, costs, currencies) runs on `BigDouble` ([BreakInfinity.cs](Assets/_Game/Plugins/BreakInfinity/)) — no overflow at 1e308, ready for endgame multipliers |
| **Formulas in one place** | Every balance formula (cost curves, HP scaling, cube-root prestige KP, geometric bulk-buy) lives in a static [`GameMath`](Assets/_Game/Scripts/Math/GameMath.cs) — covered by EditMode tests |
| **Logic never touches UI** | Managers raise events on a static [`GameEvents`](Assets/_Game/Scripts/Core/GameEvents.cs) bus; UI components only subscribe. Swap the whole HUD without touching a manager |
| **Content as data** | Tablets, tools, assistants, research nodes are ScriptableObjects balanced in the Inspector — the save file stores only state, keyed by stable IDs, so saves survive game updates |
| **Corruption-proof saves** | Versioned Newtonsoft JSON with atomic writes (temp-file swap + `.bak` fallback) and a migration hook; `BigDouble` serialized as strings to keep precision |
| **Mobile performance** | Object-pooled floating damage numbers, fixed-rate DPS ticks, zero allocations on the tap path |
| **Reproducible tooling** | One-click editor menus generate placeholder art, content assets, and scene wiring; a scripted test runner writes results to a file for CI |

## 🗂️ Project layout

```
Assets/_Game/
  Scripts/Core/      GameManager, GameEvents bus, SaveManager, SaveData
  Scripts/Math/      GameMath (all formulas), NumberFormatter (1.23K → 1.00aa → 1e3000)
  Scripts/Data/      ScriptableObject definitions (tablets, tools, assistants, research)
  Scripts/Gameplay/  TabletManager, CurrencyManager, UpgradeManager, tap input
  Scripts/UI/        HUD, upgrade panel, tablet view, pooled floating text (subscribe-only)
  Plugins/           BreakInfinity.cs (MIT)
  Data/              Balanced content assets
  Editor/            Content builders, test runner, dev tools
  Tests/EditMode/    66 NUnit tests (math, formatting, save round-trips)
```

## 🚀 Getting started

1. **Unity 6000.5.3f1** (2D URP) — open the project, let packages resolve.
2. Open `Assets/_Game/Scenes/Main.unity` and press **Play**.
3. Tap the tablet. Buy a Dusting Brush. You know what to do from there.

Handy editor menus:

- `Crumble → Run EditMode Tests` — full test suite, results in `Temp/crumble_test_results.txt`
- `Crumble → Dev → Reset Save` — wipe the save and restart fresh (works live in Play mode)
- `Crumble → Build Step 2/3 Content And Scene` — regenerate placeholder art, content assets, and scene wiring from scratch

## 🗺️ Roadmap

- [x] **Foundation** — BigDouble math core, event bus, versioned atomic save system, test suite
- [x] **Core loop** — tap damage, 5 crack states, stage progression, milestone bosses
- [x] **Economy** — 12 tools + 12 assistants, DPS tick, x1/x10/MAX buy panel
- [x] **Prestige** — cube-root Knowledge Points, live preview, confirm + reset flow
- [x] **Research Tree** — 4 branches × 15 stages, prerequisite graph, KP-powered permanent multipliers
- [x] **Offline progress** — welcome-back earnings with optional x2 rewarded-ad collect
- [x] **Juice** — Fever Mode, critical hits, positional damage popups, live DPS meter, screen shake, particles, haptics
- [x] **Side systems** — Expedition Tent missions, Museum & Artifacts with set bonuses, night cycle, sandstorm swipe events
- [ ] **Endgame** — Cosmic Archive, Time Crystals, Cosmic Altar *(next up)*
- [ ] **Ship** — safe areas, pooling audit, store integration

## 🧰 Built with

- [Unity](https://unity.com/) 6 (2D URP, UGUI, New Input System)
- [BreakInfinity.cs](https://github.com/Razenpok/BreakInfinity.cs) by Razenpok (MIT) — arbitrary-magnitude numbers
- [Newtonsoft Json.NET](https://www.newtonsoft.com/json) (via Unity package) — save serialization

*Current visuals are procedurally generated placeholders — final 16-bit pixel art is on the way.*
