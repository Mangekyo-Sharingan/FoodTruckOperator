# Detailed Procedural City Buildings Design (CityBuilder)

## 1. Purpose and Scope

This spec defines a single subsystem for `Assets/Scripts/CityBuilder.cs`: generation of detailed stylized low-poly buildings using modular geometry and procedural textures, with balanced performance suitable for gameplay in FoodTruckOperator.

The subsystem replaces today’s primitive “single cube per building” output with richer building silhouettes, facade variation, roof details, and deterministic material variation while keeping runtime generation practical and stable.

### In Scope
- Building generation inside city blocks produced by `CityBuilder`
- Modular composition of each building (base volume + optional modules)
- Runtime procedural facade/roof/window textures and color variation
- Deterministic seeded variation for reproducibility
- Performance controls and quality tiers for balanced frame time

### Out of Scope
- Roads, sidewalks, props, vehicles, NPCs, interiors, and lighting systems
- Streaming/chunk loading of city sections
- Authoring external texture assets or importing DCC building meshes
- Replacing scene-level architecture outside `CityBuilder`

---

## 2. Design Goals

- **Stylized low-poly look:** clean forms, readable silhouettes, limited noise.
- **High perceived detail:** visual richness via modular pieces and texture patterns, not dense mesh complexity.
- **Procedural textures first:** facade detail is generated in code/material setup before adding custom assets.
- **Balanced performance:** avoid extreme draw-call and memory growth while preserving variation.
- **Deterministic output:** same seed and parameters produce same city for debugging and tuning.

---

## 3. Subsystem Architecture

`CityBuilder` remains the orchestration entry point and exposes tunables in Inspector. Internal generation is split into focused responsibilities within the same file (or nested types) to avoid feature sprawl.

### Core Internal Components

1. **CityBuildingConfig (serialized parameter group)**
   - Holds tunables for geometry, texture style, and performance.
   - Includes validation ranges and sensible defaults.

2. **CitySeedContext**
   - Owns deterministic RNG state (`globalSeed`, block/building derived seeds).
   - Guarantees reproducible variation independent of Unity global `Random`.

3. **BuildingFootprintPlanner**
   - Determines footprint size/placement in each block cell and empty-lot probability.
   - Outputs `BuildingPlan` records (position, width/depth/height target, style id).

4. **BuildingModuleAssembler**
   - Converts `BuildingPlan` into modular low-poly geometry:
     - Base mass
     - Setbacks (upper tier reduction)
     - Corner chamfer variant (simple 45-degree illusion via module choice, no heavy mesh ops)
     - Roof module (flat parapet, mechanical box cluster, small pitched cap)
     - Accent modules (awnings, balcony strips, signage planes)

5. **ProceduralMaterialFactory**
   - Creates/reuses URP Lit materials with procedural texture maps (or generated mask textures) per style family.
   - Encodes facade rhythm (window bands/columns), roughness level, and color palette variance.
   - Uses `_BaseColor` and URP-compatible property workflow.

6. **BuildingPerformanceController**
   - Applies quality rules (module density, texture resolution, material pooling strategy).
   - Enforces hard caps (max buildings, max generated materials/textures).

---

## 4. Data Model and Tunable Parameters

All parameters are serialized in `CityBuilder` under grouped headers. Defaults are selected for "balanced" profile.

| Parameter | Default | Range | Purpose |
|---|---:|---:|---|
| `globalSeed` | `12345` | any int | Deterministic city variation |
| `buildingCellsPerBlockAxis` | `2` | 2-3 | Building slots per block axis |
| `emptyLotChance` | `0.12` | 0.00-0.35 | Keeps skyline breathable |
| `minBuildingHeight` | `6` | 3-12 | Minimum building height |
| `maxBuildingHeight` | `30` | 10-50 | Maximum building height |
| `heightCurvePower` | `1.25` | 0.5-2.5 | Bias toward medium vs tall buildings |
| `footprintFillMin` | `0.58` | 0.45-0.80 | Minimum slot footprint fill ratio |
| `footprintFillMax` | `0.90` | 0.60-0.95 | Maximum slot footprint fill ratio |
| `blockEdgePadding` | `1.8` | 0.5-4.0 | Prevents clipping into sidewalks |
| `setbackChance` | `0.45` | 0.00-0.90 | Upper tier variation |
| `roofModuleChance` | `0.70` | 0.00-1.00 | Roof silhouette detail |
| `accentModuleChance` | `0.35` | 0.00-0.80 | Awnings/sign strips/balcony bands |
| `windowBandDensity` | `0.62` | 0.20-0.95 | Facade detail intensity |
| `windowLitRatio` | `0.28` | 0.00-1.00 | Lit vs dark window pattern |
| `proceduralTextureResolution` | `256` | 64-512 | Per-style generated texture size |
| `styleFamilyCount` | `6` | 3-12 | Distinct facade/roof color families |
| `materialsPerFamilyMax` | `3` | 1-6 | Variation per style without explosion |
| `maxGeneratedMaterials` | `24` | 8-64 | Hard cap for pooled materials |
| `maxGeneratedTextures` | `18` | 6-48 | Hard cap for generated textures |
| `batchParentPerBlock` | `true` | bool | Hierarchy grouping for editor clarity |
| `qualityProfile` | `Balanced` | Low/Balanced/High | Top-level scaling preset |
| `enableColliderOnDetails` | `false` | bool | Colliders only on main mass by default |

### Quality Profile Multipliers

- **Low:** fewer modules, 128 texture res, lower accent chance, strict material reuse.
- **Balanced (default):** values in table.
- **High:** slightly denser modules, up to 512 texture res, broader color variance.

---

## 5. Generation and Data Flow

1. `Build()` clears old generated children and computes block grid.
2. Ground/road/sidewalk generation runs as currently designed.
3. For each block cell, `BuildingFootprintPlanner` decides:
   - empty lot vs occupied
   - footprint dimensions
   - target height and style family
4. `BuildingModuleAssembler` instantiates modular primitives for occupied cells:
   - always one base mass
   - optional upper setbacks
   - optional roof and accents based on probabilities and height thresholds
5. `ProceduralMaterialFactory` provides pooled materials/textures for style family and module type.
6. Parenting and naming follow stable deterministic IDs (`Building_bx_bz_ix_iz_seedHash`).
7. `BuildingPerformanceController` checks counts against caps and downgrades detail dynamically if limits are reached.

---

## 6. Procedural Texture and Style System

### Visual Style Rules
- Low-poly, broad color fields, subtle value shifts.
- Facades use stylized window rhythm masks (bands/columns), not photoreal textures.
- Roof modules use slightly darker tones with low smoothness for readable shape separation.

### Texture Generation Strategy
- Generate a small set of style-family textures at build start, then reuse.
- Texture content includes:
  - Window mask pattern
  - Grime/noise modulation (very mild)
  - Accent stripe mask
- Generated textures are cached during runtime generation pass and shared across multiple materials.

### Material Rules (URP)
- URP Lit shader only.
- Use `_BaseColor` as primary color control.
- Smoothness kept low (`~0.05-0.18`) to maintain stylized matte look.
- Avoid per-building unique material creation beyond cap limits.

---

## 7. Failure Handling and Fallback Behavior

If generation fails at any stage, subsystem must degrade gracefully and still produce playable city geometry.

### Validation and Guards
- Clamp all tunables before use.
- If `max < min` for dimensions/heights, swap or reset to defaults.
- If block/cell computation yields non-positive usable area, skip building for that slot.

### Runtime Fallbacks
- **Shader not found:** fall back to URP Lit lookup path already used; if unavailable, use Standard as last safety.
- **Texture generation failure:** use flat color materials from style palette.
- **Material cap reached:** reuse nearest matching pooled material.
- **Performance cap reached mid-build:** disable optional modules for remaining buildings (base mass only + simple roof).
- **Unexpected exception in one building:** log warning and continue with next building, never abort full city pass.

### Logging Policy
- Warnings include block/cell coordinates and seed fragment for reproducibility.
- One aggregated summary warning per build for cap-triggered downgrades.

---

## 8. Performance Budgets (Balanced Profile)

Target platform assumptions: desktop gameplay scene with current project scope.

- **City build time at scene start:** <= `120 ms` average, <= `180 ms` worst-case
- **Generated building count:** <= `520` occupied masses in default 12x12 city
- **Total building-related renderers:** <= `1,800`
- **Draw calls contributed by buildings:** <= `350` in Game view default camera framing
- **Generated materials:** <= `24`
- **Generated textures memory:** <= `12 MB` total for procedural maps
- **GC allocations during Build():** no recurring per-frame allocations after build completion
- **Runtime FPS impact after build:** maintain >= `60 FPS` on target desktop scene baseline

If budget thresholds are exceeded, subsystem reduces detail density in this order:
1. Disable accents
2. Reduce roof module frequency
3. Remove setbacks
4. Reduce texture resolution
5. Collapse to base mass + pooled flat materials

---

## 9. Acceptance Criteria

The subsystem is accepted when all criteria are true:

1. Buildings are visibly more detailed than single cubes: at least three distinct silhouette classes and multiple roof variants visible in a default city.
2. Output remains stylized low-poly (no photoreal noise dominance, no high-poly mesh complexity).
3. Procedural texture pipeline is active and visible in facades, with clear window rhythm variation.
4. Same `globalSeed` produces identical building placement/styles across repeated runs.
5. Changing `globalSeed` produces noticeably different skyline and facade distribution.
6. Balanced profile stays within defined performance budgets.
7. Failure cases degrade gracefully with no fatal errors and no missing play surface.

---

## 10. Verification Checklist (Design Validation)

### Visual Verification
- [ ] Skyline shows mixed low/medium/tall heights without uniform repetition.
- [ ] Facade patterns vary across style families but remain visually cohesive.
- [ ] Roof details are present on majority of medium/tall buildings.
- [ ] Empty lots occur occasionally and improve readability of streets.
- [ ] Color palette remains stylized and not overly saturated/noisy.
- [ ] No z-fighting or obvious clipping against sidewalks/roads.

### Performance Verification
- [ ] Profile `Build()` timing in Play Mode for 3 seeds; all runs within budget.
- [ ] Confirm material and texture counts respect hard caps.
- [ ] Verify draw call budget in representative camera positions.
- [ ] Confirm no continuous GC spikes after build completion.
- [ ] Validate quality profile switches (Low/Balanced/High) change cost/detail predictably.

### Robustness Verification
- [ ] Invalid parameter combinations clamp safely and still build city.
- [ ] Forced texture generation failure still yields visible buildings.
- [ ] Cap-triggered downgrade path logs once and completes build.
- [ ] Build continues if one building slot errors.

---

## 11. Non-Goals (Scope Control)

- No interior generation for buildings.
- No procedural prop placement (trees, benches, street furniture).
- No dynamic time-of-day emissive window simulation in this subsystem.
- No GPU-instanced custom mesh authoring pipeline in this phase.
- No city streaming, chunking, or runtime destruction/regeneration during gameplay loop.
- No migration of city generation out of `CityBuilder` to separate scene systems in this phase.

---

## 12. Design Rationale Summary

This design achieves high perceived city detail through controlled modular composition and procedural facade textures while preserving startup performance and deterministic behavior. It intentionally limits feature breadth to one subsystem (`CityBuilder` buildings) and uses explicit caps and graceful degradation so visual ambition does not compromise gameplay stability.
