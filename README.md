# Dig Deep — UMD GDC Fall 2025 Game Jam

A first-person voxel mining game built in Unity for the University of Maryland Game Developers Club Fall 2025 Game Jam. Dig through procedurally generated layered terrain, collect ores, sell your haul, and upgrade your pickaxe to reach greater depths.

**Play it on itch.io:** https://brokens.itch.io/dig-deep

---

## Table of Contents

- [Gameplay Overview](#gameplay-overview)
- [Controls](#controls)
- [Core Gameplay Loop](#core-gameplay-loop)
- [World Generation](#world-generation)
- [Ore Types](#ore-types)
- [Subsurface Layers](#subsurface-layers)
- [Inventory System](#inventory-system)
- [Progression & Upgrades](#progression--upgrades)
- [Stations](#stations)
- [Architecture Overview](#architecture-overview)
- [Scripts Reference](#scripts-reference)
- [Dependencies](#dependencies)
- [Building from Source](#building-from-source)

---

## Gameplay Overview

Dig Deep is a first-person mining sandbox. You start on the surface with a basic pickaxe and dig into a voxel terrain that gets progressively harder — and more rewarding — the deeper you go. Ores are embedded in the terrain as physical objects that pop loose when their surrounding terrain is removed. Collect them, haul them to the Selling Station for XP, spend skill points at the Upgrade Station, and unlock deeper layers by increasing your tool tier.

The game has no enemies. The core tension comes from the weight limit on your inventory and the hardness of deeper materials — you must decide when to head back to the surface to sell, and how to prioritize upgrades.

---

## Controls

| Input | Action |
|-------|--------|
| `W / A / S / D` | Move |
| `Space` | Jump |
| `Mouse` | Look around |
| `Left Mouse Button` (hold) | Swing pickaxe / Mine |
| `E` | Interact (Selling Station, Upgrade Station) |
| `T` | Teleport back to spawn point |
| `Escape` | Pause menu |

---

## Core Gameplay Loop

```
Dig terrain  →  Collect ores & rubble  →  Sell at Selling Station
     ↑                                            |
     └──────── Spend skill points at Upgrade Station ←┘
```

1. **Dig** into the procedurally generated terrain using your pickaxe.
2. **Collect** ores that fall loose as you mine. Rubble (dirt, rock) is also collected automatically but weighs less.
3. **Watch your inventory weight** — when it fills up you cannot dig further until you sell.
4. **Return to the surface** and interact with the **Selling Station** (`E`) to sell all collected resources for XP.
5. **Level up** to earn skill points, then visit the **Upgrade Station** (`E`) to spend them on pickaxe improvements.
6. **Increase your tool tier** (every 5 upgrades = +1 tier) to access deeper, harder, and more valuable layers.
7. Use `T` to instantly teleport back to the spawn point when you are deep underground.

---

## World Generation

The terrain is generated procedurally using **Perlin noise** and the **Marching Cubes** algorithm, which creates smooth, organic-looking cave walls rather than blocky Minecraft-style terrain.

### Chunk Parameters

| Parameter | Value |
|-----------|-------|
| Chunk width (X/Z) | 23 voxels |
| Chunk height (Y) | 40 voxels |
| Voxel size | 1 unit |
| Noise scale | ~0.12 – 0.15 |
| Terrain amplitude | 1 – 4 units |
| Surface height | ~30 – 39 units from bottom |

A configurable seed controls terrain shape. Setting the seed to `-1` randomizes each session.

### Mesh Generation

The terrain mesh is built with the **Marching Cubes** algorithm using precomputed edge and triangle lookup tables (`MarchingCubesTable.cs`). Each vertex is colored based on which subsurface layer it belongs to, producing visible striations as you dig deeper. Normals are recalculated after every dig operation to keep lighting correct.

---

## Ore Types

Ores are spawned as individual GameObjects inside the terrain based on 3D volumetric Perlin noise. Each ore type occupies a specific depth band and has a rarity threshold.

| Ore | Depth Range (from surface) | Rarity | Sell Value | XP Value |
|-----|---------------------------|--------|-----------|---------|
| Copper | Shallow (Y: 25–37) | Common | 5 | 10 |
| Iron | Mid (Y: 15–30) | Common | 10 | 15 |
| Gold | Deep (Y: 10–25) | Uncommon | 20 | 25 |
| Amethyst | Very deep (Y: 5–20) | Rare | 40 | 40 |
| Diamond | Deepest (Y: 2–12) | Very rare | 100 | 50 |

**Rubble** (Dirt, Limestone, Granite, Bedrock, Molten Rock) is also collected as a byproduct of digging. Rubble weighs 0.1 per unit versus 1.0 for ores, so it takes up far less inventory space.

### Ore Node Physics

Ores are not statically embedded in the mesh — each ore is a separate GameObject with an `OreNode` component. The node periodically checks how many of its 7 surrounding sample points have been excavated. When 3 or more surrounding points are in open air, the ore "pops loose" and is automatically added to your inventory. This means clever undermining can cause ore clusters to fall without digging every block individually.

---

## Subsurface Layers

As you dig deeper the terrain changes color, becomes harder, and eventually requires a higher tool tier to break at all.

| Layer | Depth | Color | Hardness | Required Tier |
|-------|-------|-------|----------|---------------|
| Grass | 0 – 2 m | Green | 1.0× | 0 |
| Soil | 2 – 4 m | Brown | 1.0× | 0 |
| Limestone | 4 – 10 m | Tan | 1.5× | 0 |
| Granite | 10 – 25 m | Grey | 3.0× | 1 |
| Deep Granite | 25 – 35 m | Dark grey | 5.0× | 2 |
| Bedrock | 35+ m | Near-black | 10.0× | 3 |

**Hardness** multiplies the number of hits required to remove a block — Bedrock takes 10× as many swings as surface soil. **Required Tier** is a hard gate: attempting to dig a block without the appropriate tool tier shows a warning and does no damage.

---

## Inventory System

The inventory is **weight-based**, not slot-based.

| Parameter | Default |
|-----------|---------|
| Max weight capacity | 200 |
| Ore weight per unit | 1.0 |
| Rubble weight per unit | 0.1 |
| Per-resource stack cap | 9,999 |

When your total carried weight reaches the maximum, the pickaxe stops working and a notification tells you to sell. Upgrading **Storage** at the Upgrade Station raises the weight cap by 25 per upgrade level.

The HUD displays a bar for each resource type as well as a total weight bar so you can monitor your load at a glance.

---

## Progression & Upgrades

### XP and Leveling

XP is earned by selling resources at the Selling Station. Each level-up grants **1 skill point**. XP requirements scale exponentially:

```
XP required for level N = 100 × 1.5^(N-1)
```

| Level | XP Required |
|-------|------------|
| 1 | 100 |
| 2 | 150 |
| 3 | 225 |
| 4 | 338 |
| 5 | 506 |
| … | … |

Excess XP carries over to the next level. The HUD shows a circular XP progress ring and the current level.

### Skill Point Upgrades

Skill points are spent at the Upgrade Station. There are five upgrade categories:

| Upgrade | Effect | Base Value | Per Upgrade |
|---------|--------|-----------|------------|
| Strength | Dig damage per swing | 10 | +0.5 |
| Radius | Mining area size | 2.0 | +0.2 |
| Distance | Max reach | 10 units | +1.0 |
| Attack Speed | Swings per second | 1.0 | +0.1 |
| Storage | Inventory weight cap | 200 | +25 |

### Tool Tier

Every **5 total upgrades** (across all categories) increases your tool tier by 1. Tier determines which terrain layers you can dig:

| Tier | Unlocks |
|------|---------|
| 0 | Grass, Soil, Limestone |
| 1 | Granite |
| 2 | Deep Granite |
| 3 | Bedrock |

The Upgrade Station UI always shows your current tier and progress toward the next one.

---

## Stations

### Selling Station

Approach and press `E` to sell everything in your inventory at once. All ores and rubble are converted to XP based on their individual XP values. A sell sound plays and a popup confirms the transaction.

### Upgrade Station

Approach and press `E` to open the upgrade menu. The menu shows:
- Current values for all five upgrade categories
- Your available skill points
- Current tool tier and upgrades-until-next-tier counter
- Buttons to spend 1 skill point on each upgrade

---

## Architecture Overview

```
Assets/Scripts/
├── Player/
│   ├── PlayerController.cs      # Rigidbody movement & jumping
│   ├── PlayerCam.cs             # First-person mouse look
│   ├── PlayerInventory.cs       # Weight-based resource storage
│   ├── PlayerProgression.cs     # XP, leveling, skill points
│   ├── Interactor.cs            # Raycast interaction detection
│   └── Teleport.cs              # Spawn-point teleport (T key)
├── Tools/
│   ├── DigTool.cs               # Mining logic, upgrades, tier gating
│   └── Swing.cs                 # Pickaxe animation controller
├── Environment/
│   ├── Terrain/
│   │   ├── VoxelData.cs         # Noise generation, voxel state
│   │   ├── TerrainChunk.cs      # Chunk management, ore spawning
│   │   ├── VoxelMeshGenerator.cs# Marching cubes mesh builder
│   │   ├── MarchingCubesTable.cs# Edge/triangle lookup tables
│   │   └── SubsurfaceLayer.cs   # Layer data (depth, color, hardness)
│   ├── Ores/
│   │   ├── OreNode.cs           # Per-ore terrain connection check
│   │   └── OreCrumbleEffect.cs  # Particle effect on destroy
│   ├── SellingStation.cs        # IInteractable sell logic
│   └── UpgradeStation.cs        # IInteractable upgrade UI
├── UI/
│   ├── InventoryUI.cs           # Resource bars HUD
│   ├── PlayerProgressionUI.cs   # XP ring + level display
│   ├── NotificationSystem.cs    # Fade-in/out screen messages
│   ├── PopUpSystem.cs           # Pause menu / pause state
│   └── ButtonUpgrade.cs         # Upgrade button handlers
└── Audio/
    └── AudioManager.cs          # Singleton audio manager
```

### Key Design Patterns

- **Singleton** — `AudioManager` uses `DontDestroyOnLoad` to persist across scene loads and exposes convenience methods (`PlayDig()`, `PlaySell()`, `PlayLevelUp()`, etc.).
- **Interface** — `IInteractable` is implemented by `SellingStation` and `UpgradeStation`, making it straightforward to add new interactable objects.
- **UnityEvents** — `PlayerProgression` and `PlayerInventory` fire events that the UI components subscribe to, keeping game logic decoupled from display logic.
- **Serializable data classes** — `SubsurfaceLayer` and `OreGenerationSettings` are `[Serializable]` structs configured entirely in the Inspector.
- **Animation events** — Dig damage is applied on animation events from the pickaxe swing, not in `Update()`, preventing double-hits and syncing with animation speed.

---

## Scripts Reference

### Player

| Script | Description |
|--------|-------------|
| `PlayerController.cs` | WASD + jump with Rigidbody. Separate drag values for grounded vs. airborne movement. |
| `PlayerCam.cs` | Mouse-look with ±90° vertical clamp. Stores spawn position and rotation for teleport reset. |
| `PlayerInventory.cs` | Tracks amounts per resource type. `AddResource()` returns actual added amount, respecting weight cap. `IsFull()` gates digging. |
| `PlayerProgression.cs` | Exponential XP curve. `AddXP()` handles carry-over. `SpendSkillPoint()` consumed by upgrade buttons. |
| `Interactor.cs` | Single raycast from camera center. On `E` press, calls `Interact()` on the nearest `IInteractable`. |
| `Teleport.cs` | Resets Rigidbody velocity, snaps position to SpawnPoint transform, resets camera Euler angles. |

### Tools

| Script | Description |
|--------|-------------|
| `DigTool.cs` | Core mining script. Casts a sphere at hit point, iterates voxels in radius, applies `strength / hardness` damage per swing. Checks tier gate before dealing damage. Increments tier every 5 upgrades. |
| `Swing.cs` | Drives the Animator `swingSpeed` parameter to match `DigTool.attackSpeed`. Holds the button for continuous swinging. Prevents animation when inventory is full. |

### Environment

| Script | Description |
|--------|-------------|
| `VoxelData.cs` | Stores the 3D density array. `GenerateTerrain()` uses 2D Perlin for the surface and 3D Perlin per ore type. Exposes `Dig()` to remove voxels. |
| `TerrainChunk.cs` | On `Start`, calls `VoxelData` to generate, then `VoxelMeshGenerator` to build the mesh, then spawns ore prefabs. Reacts to `Dig()` calls by regenerating affected mesh sections. |
| `VoxelMeshGenerator.cs` | Pure marching-cubes implementation. Colors vertices by which `SubsurfaceLayer` they fall in. Returns `Mesh` ready for assignment to a `MeshFilter`. |
| `OreNode.cs` | Every 0.5 s, samples 7 world-space points around itself. If ≥ 3 are in open air, destroys self and calls `PlayerInventory.AddResource()`. |
| `SellingStation.cs` | On interaction, iterates all resource types, calls `PlayerInventory.RemoveResource()`, calls `PlayerProgression.AddXP()`. |
| `UpgradeStation.cs` | On interaction, opens upgrade panel. Each button calls `PlayerProgression.SpendSkillPoint()` then the relevant upgrade method on `DigTool` or `PlayerInventory`. |

### Audio

| Script | Description |
|--------|-------------|
| `AudioManager.cs` | Singleton. Separate `AudioSource` components for SFX and music. Volume setters affect the source directly. Convenience play methods called throughout the codebase. |

---

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Universal Render Pipeline | 17.4.0 | Modern physically-based rendering |
| Input System | 1.19.0 | Keyboard/mouse input handling |
| TextMesh Pro | 3.2.0 | High-quality UI text |
| Unity UI (uGUI) | 2.0.0 | Canvas-based HUD and menus |
| Timeline | 1.8.11 | Animation timeline support |
| Visual Scripting | 1.9.11 | (Included, not actively used) |

---

## Building from Source

1. Install **Unity 6** (6000.x LTS recommended — matches `ProjectVersion.txt`).
2. Clone this repository.
3. Open the project folder in Unity Hub.
4. Unity will import all packages from `Packages/manifest.json` automatically.
5. Open `Assets/Scenes/MenuScene` as the startup scene.
6. Use **File → Build Settings** to build for your target platform (tested on Windows Standalone).

> **Note:** The project uses the **Universal Render Pipeline**. If materials appear pink after opening, go to **Edit → Rendering → Render Pipeline → Upgrade Project Materials to URP**.

---

*Made for the UMD Game Developers Club Fall 2025 Game Jam.*
