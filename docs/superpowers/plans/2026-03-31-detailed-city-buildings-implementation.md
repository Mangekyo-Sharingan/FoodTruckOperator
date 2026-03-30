# Detailed City Buildings Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace cube-only city buildings in `CityBuilder` with deterministic, stylized low-poly modular buildings, procedural facade/roof materials, and cap-based performance downgrades that stay within the approved spec budgets.

**Architecture:** Keep all building-system logic inside `Assets/Scripts/CityBuilder.cs` using focused nested types and helper methods (`CityBuildingConfig`, seed context, planner, assembler, material factory, performance controller). Build flow remains `Build() -> BuildGround() -> BuildRoads() -> BuildBlocks()`, but `BuildBlocks()` uses deterministic plans and module assembly instead of direct `Random` cube creation. Add EditMode tests to lock determinism, parameter clamping, and graceful fallback behavior.

**Tech Stack:** Unity (URP), C#, Unity Test Framework (EditMode), Unity Profiler (`ProfilerMarker`), `unity-mcp-cli` (`tests-run` tool)

---

## File Structure

- Modify: `Assets/Scripts/CityBuilder.cs`  
  Responsibility: all in-scope building generation logic (config, deterministic planning, modular assembly, procedural materials/textures, caps, fallbacks, logging, profiling marker).

- Create: `Assets/Tests/EditMode/CityBuilderTests.cs`  
  Responsibility: deterministic output, validation/clamping, downgrade behavior, and no-throw fallback regression tests.

- Create: `Assets/Tests/EditMode/CityBuilderTests.asmdef`  
  Responsibility: isolate EditMode tests into a compilable test assembly referencing runtime script assembly.

- Create: `docs/superpowers/plans/2026-03-31-detailed-city-buildings-implementation.md`  
  Responsibility: saved copy of this plan for implementation tracking.

---

### Task 1: Add Serialized Building Config + Quality Profile Scaffold

**Files:**
- Modify: `Assets/Scripts/CityBuilder.cs`
- Create: `docs/superpowers/plans/2026-03-31-detailed-city-buildings-implementation.md`

- [ ] **Step 1: Save this plan to target path**
  
  Run:
  ```bash
  mkdir -p docs/superpowers/plans
  ```
  Expected: directory exists.

- [ ] **Step 2: Add config types in `CityBuilder`**
  
  Add nested enum + struct/classes:
  ```csharp
  public enum CityQualityProfile { Low, Balanced, High }

  [System.Serializable]
  public class CityBuildingConfig
  {
      public int globalSeed = 12345;
      public int buildingCellsPerBlockAxis = 2;
      public float emptyLotChance = 0.12f;
      public float minBuildingHeight = 6f;
      public float maxBuildingHeight = 30f;
      public float heightCurvePower = 1.25f;
      public float footprintFillMin = 0.58f;
      public float footprintFillMax = 0.90f;
      public float blockEdgePadding = 1.8f;
      public float setbackChance = 0.45f;
      public float roofModuleChance = 0.70f;
      public float accentModuleChance = 0.35f;
      public float windowBandDensity = 0.62f;
      public float windowLitRatio = 0.28f;
      public int proceduralTextureResolution = 256;
      public int styleFamilyCount = 6;
      public int materialsPerFamilyMax = 3;
      public int maxGeneratedMaterials = 24;
      public int maxGeneratedTextures = 18;
      public bool batchParentPerBlock = true;
      public CityQualityProfile qualityProfile = CityQualityProfile.Balanced;
      public bool enableColliderOnDetails = false;
  }
  ```

- [ ] **Step 3: Replace legacy building fields with grouped inspector settings**
  
  Keep existing road/city layout fields; replace old `buildingsPerBlock/minHeight/maxHeight/buildingPadding` usage with `CityBuildingConfig buildingConfig`.

- [ ] **Step 4: Add clamp/validate method**
  
  Add `ValidateBuildingConfig()` called at start of `Build()`:
  ```csharp
  void ValidateBuildingConfig()
  {
      buildingConfig.buildingCellsPerBlockAxis = Mathf.Clamp(buildingConfig.buildingCellsPerBlockAxis, 2, 3);
      buildingConfig.emptyLotChance = Mathf.Clamp01(buildingConfig.emptyLotChance);
      if (buildingConfig.maxBuildingHeight < buildingConfig.minBuildingHeight)
          (buildingConfig.minBuildingHeight, buildingConfig.maxBuildingHeight) =
              (buildingConfig.maxBuildingHeight, buildingConfig.minBuildingHeight);
      // clamp remaining values to spec ranges...
  }
  ```

- [ ] **Step 5: Add `ProfilerMarker` placeholder around build body**
  
  Add static marker:
  ```csharp
  static readonly Unity.Profiling.ProfilerMarker BuildMarker =
      new Unity.Profiling.ProfilerMarker("CityBuilder.Build");
  ```
  Wrap main `Build()` body in `using (BuildMarker.Auto()) { ... }`.

- [ ] **Step 6: Commit**
  
  Run:
  ```bash
  git add "docs/superpowers/plans/2026-03-31-detailed-city-buildings-implementation.md" "Assets/Scripts/CityBuilder.cs"
  git commit -m "feat(city): scaffold detailed building config and profiling marker"
  ```
  Expected: commit created with config scaffold only.

---

### Task 2: Implement Deterministic Seed Context + Building Plan Generation

**Files:**
- Modify: `Assets/Scripts/CityBuilder.cs`

- [ ] **Step 1: Add deterministic RNG helper**
  
  Add internal seed context independent of Unity global `Random`:
  ```csharp
  struct CitySeedContext
  {
      public int globalSeed;
      public int SeedForCell(int bx, int bz, int ix, int iz)
      {
          unchecked
          {
              int h = globalSeed;
              h = (h * 397) ^ bx;
              h = (h * 397) ^ bz;
              h = (h * 397) ^ ix;
              h = (h * 397) ^ iz;
              return h;
          }
      }
  }
  ```

- [ ] **Step 2: Add `BuildingPlan` data record**
  
  Include fields: coordinates, footprint center/size, height, style family id, empty lot flag, derived seed hash.

- [ ] **Step 3: Add planner method for one slot**
  
  Add method returning `bool` or `BuildingPlan`:
  ```csharp
  bool TryPlanBuildingSlot(..., out BuildingPlan plan)
  ```
  Use `System.Random(slotSeed)` and `heightCurvePower` to bias heights.

- [ ] **Step 4: Replace direct `Random.*` in `BuildBlocks()`**
  
  Change flow to:
  1) compute per-block grid by `buildingCellsPerBlockAxis`,  
  2) call planner per slot,  
  3) skip empty lots,  
  4) hand plan to assembler (stub for now).

- [ ] **Step 5: Add deterministic naming format**
  
  Name root building objects as:
  `Building_{bx}_{bz}_{ix}_{iz}_{seedHash}`.

- [ ] **Step 6: Play Mode sanity check**
  
  In Unity Editor: press Play twice with same `globalSeed`, confirm building names/placements match exactly.

- [ ] **Step 7: Commit**
  
  Run:
  ```bash
  git add "Assets/Scripts/CityBuilder.cs"
  git commit -m "feat(city): add deterministic building plan generation by seed"
  ```
  Expected: reproducible plan generation committed.

---

### Task 3: Implement Modular Building Assembler (Base + Setbacks + Roof + Accents)

**Files:**
- Modify: `Assets/Scripts/CityBuilder.cs`

- [ ] **Step 1: Add assembler entry point**
  
  Add:
  ```csharp
  void AssembleBuilding(in BuildingPlan plan, Transform parent)
  ```
  Create root object and always spawn base mass.

- [ ] **Step 2: Add base module helper**
  
  Create primitive cube module and assign transform using plan footprint/height.

- [ ] **Step 3: Add setback module logic**
  
  If height/probability passes: create upper tier with reduced width/depth and elevated Y.

- [ ] **Step 4: Add corner chamfer variant logic**
  
  Implement a lightweight corner chamfer silhouette option per plan/style (module-based 45-degree corner illusion, no custom mesh editing).

- [ ] **Step 5: Add roof variants**
  
  Implement 3 roof modes:
  - flat parapet,
  - mechanical box cluster,
  - small pitched cap (simple primitive composition).

- [ ] **Step 6: Add accent variants**
  
  Add optional awning strip / balcony band / signage plane (low-poly primitives only).

- [ ] **Step 7: Respect collider policy**
  
  Keep collider on base only by default; details use no collider unless `enableColliderOnDetails=true`.

- [ ] **Step 8: Use optional block parent grouping**
  
  If `batchParentPerBlock`, create/reuse `Block_{bx}_{bz}` parent under city root.

- [ ] **Step 9: Commit**
  
  Run:
  ```bash
  git add "Assets/Scripts/CityBuilder.cs"
  git commit -m "feat(city): assemble modular low-poly building silhouettes"
  ```
  Expected: visible silhouette variety without touching non-building systems.

---

### Task 4: Implement Procedural Material + Texture Factory (URP-Compatible, Pooled)

**Files:**
- Modify: `Assets/Scripts/CityBuilder.cs`

- [ ] **Step 1: Add style-pool caches**
  
  Add dictionaries/lists for generated textures/materials keyed by style family + module type.

- [ ] **Step 2: Add procedural texture generator**
  
  Create small `Texture2D` masks (window rhythm + mild noise + accent stripe), e.g.:
  ```csharp
  Texture2D GenerateFacadeMask(int seed, int resolution, float bandDensity, float litRatio)
  ```
  Set:
  - `wrapMode = TextureWrapMode.Repeat`
  - `filterMode = FilterMode.Point` (or Bilinear if preferred look).

- [ ] **Step 3: Add URP material creation**
  
  Use URP Lit shader first:
  ```csharp
  var shader = Shader.Find("Universal Render Pipeline/Lit");
  var mat = new Material(shader);
  mat.SetColor("_BaseColor", baseColor);
  mat.SetFloat("_Smoothness", smoothness);
  mat.SetTexture("_BaseMap", facadeMask);
  ```

- [ ] **Step 4: Add shader fallback chain**
  
  Implement:
  1) URP Lit,  
  2) reuse known-good generated URP material,  
  3) `Unlit/Color` minimal fallback.

- [ ] **Step 5: Enforce caps**
  
  Ensure no more than `maxGeneratedMaterials` and `maxGeneratedTextures`; when cap reached, reuse nearest pooled style.

- [ ] **Step 6: Assign module materials**
  
  Distinguish base facade, roof tone (darker/lower smoothness), and accents (contrast stripe).

- [ ] **Step 7: Commit**
  
  Run:
  ```bash
  git add "Assets/Scripts/CityBuilder.cs"
  git commit -m "feat(city): add pooled procedural facade and roof materials with URP fallback"
  ```
  Expected: procedural facade variation visible; no per-building material explosion.

---

### Task 5: Add Performance Controller + Quality Profile Downgrade Chain

**Files:**
- Modify: `Assets/Scripts/CityBuilder.cs`

- [ ] **Step 1: Add build counters**
  
  Track during build:
  - occupied building count,
  - renderer count,
  - generated materials/textures,
  - downgrade flags.

- [ ] **Step 2: Apply profile multipliers**
  
  Add method:
  ```csharp
  void ApplyQualityProfile(ref RuntimeBuildSettings s)
  ```
  - Low: less modules + lower texture res + stricter reuse  
  - Balanced: default  
  - High: denser modules + higher texture res (within caps)

- [ ] **Step 3: Enforce occupied-building cap**
  
  Add explicit `maxOccupiedBuildings` enforcement in runtime build settings. Once cap is reached, stop creating new occupied building masses for remaining slots and log cap-hit in summary.

- [ ] **Step 4: Implement downgrade order exactly from spec**
  
  On pressure/cap exceed:
  1) disable accents  
  2) reduce roof chance  
  3) disable setbacks  
  4) reduce texture resolution  
  5) base mass + pooled flat material only

- [ ] **Step 5: Add one aggregated downgrade warning**
  
  Emit one summary warning at end of `Build()` if any downgrade triggered.

- [ ] **Step 6: Keep per-building failures isolated**
  
  Wrap each slot assembly in `try/catch`; log warning with block/cell/seed fragment and continue.

- [ ] **Step 7: Commit**
  
  Run:
  ```bash
  git add "Assets/Scripts/CityBuilder.cs"
  git commit -m "feat(city): enforce building performance caps and graceful downgrade policy"
  ```
  Expected: build never aborts due to one bad slot; cap behavior deterministic and logged.

---

### Task 6: Add EditMode Tests for Determinism, Validation, and Fallback Stability

**Files:**
- Create: `Assets/Tests/EditMode/CityBuilderTests.asmdef`
- Create: `Assets/Tests/EditMode/CityBuilderTests.cs`
- Modify: `Assets/Scripts/CityBuilder.cs` (only if small testability hook is needed)

- [ ] **Step 1: Create EditMode test assembly**
  
  `CityBuilderTests.asmdef` should include `"optionalUnityReferences": ["TestAssemblies"]`.

- [ ] **Step 2: Add determinism test**
  
  Test pattern:
  ```csharp
  [Test]
  public void Build_WithSameSeed_ProducesSameBuildingNamesAndPositions()
  ```
  Build twice (fresh GameObject each time), compare ordered tuples `(name, position, scale)` for building roots.

- [ ] **Step 3: Add seed variation test**
  
  ```csharp
  [Test]
  public void Build_WithDifferentSeed_ChangesBuildingLayout()
  ```
  Assert at least one tuple differs.

- [ ] **Step 4: Add config clamp test**
  
  Force invalid config (`max < min`, out-of-range densities), call `Build()`, assert no exception and corrected behavior (nonzero generated geometry).

- [ ] **Step 5: Add cap/fallback stability test**
  
  Configure tiny caps (`maxGeneratedMaterials=1`, `maxGeneratedTextures=1`), build, assert no exception and buildings still generated.

- [ ] **Step 6: Add forced texture-generation-failure fallback test**
  
  Add a test hook to simulate texture generation failure and assert city build completes with fallback materials and no thrown exceptions.

- [ ] **Step 7: Add per-slot exception isolation test**
  
  Add a test hook to throw during one slot assembly and assert remaining slots still build, with warning recorded.

- [ ] **Step 8: Run EditMode tests**
  
  Run:
  ```bash
  printf '{"testMode":"EditMode","className":"CityBuilderTests"}' | unity-mcp-cli run-tool tests-run --input-file -
  ```
  Expected: all `CityBuilderTests` pass.

- [ ] **Step 9: Commit**
  
  Run:
  ```bash
  git add "Assets/Tests/EditMode/CityBuilderTests.asmdef" "Assets/Tests/EditMode/CityBuilderTests.cs" "Assets/Scripts/CityBuilder.cs"
  git commit -m "test(city): cover deterministic generation, clamping, and fallback behavior"
  ```
  Expected: reproducibility and robustness guarded by automated tests.

---

### Task 7: Verification Pass Against Spec Acceptance + Performance Budgets

**Files:**
- Modify: `Assets/Scripts/CityBuilder.cs` (only if final tuning needed)

- [ ] **Step 1: Verify visual acceptance in Unity Editor**
  
  In Play Mode (Balanced):
  - confirm at least 3 silhouette classes,
  - confirm multiple roof variants,
  - confirm occasional empty lots,
  - confirm no obvious sidewalk clipping/z-fighting.

- [ ] **Step 2: Verify deterministic behavior manually**
  
  Same seed twice => same skyline; change seed => visible skyline/facade variation.

- [ ] **Step 3: Verify profiler marker + CPU timing**
  
  Unity Profiler (CPU module): inspect `CityBuilder.Build` marker over 3 seeds.  
  Expected target: avg <= 120 ms, worst <= 180 ms (Balanced).

- [ ] **Step 4: Verify rendering/material budgets**
  
  Rendering module + runtime counters/log summary:
  - renderers <= 1800,
  - occupied buildings <= configured cap,
  - materials <= 24,
  - textures <= 18,
  - generated texture memory <= 12 MB,
  - draw calls around/under target budget in representative views.

- [ ] **Step 5: Verify FPS budget and profile behavior**
  
  Confirm >= 60 FPS in representative gameplay camera after build stabilization, and verify Low/Balanced/High profiles visibly change detail level and measured cost in expected direction.

- [ ] **Step 6: Verify no recurring post-build GC spikes**
  
  Profiler memory/GC timeline after city is built and camera idles for 10 seconds.

- [ ] **Step 7: Final command-line regression**
  
  Run:
  ```bash
  printf '{"testMode":"EditMode","className":"CityBuilderTests"}' | unity-mcp-cli run-tool tests-run --input-file -
  ```
  Expected: PASS (no regressions after tuning).

- [ ] **Step 8: Commit**
  
  Run:
  ```bash
  git add "Assets/Scripts/CityBuilder.cs"
  git commit -m "chore(city): tune detailed building generation to meet spec budgets"
  ```
  Expected: final implementation aligned with approved spec and budgets.

---

## Implementation Notes (Strict Scope Guard)

- Only touch building-generation behavior in `CityBuilder`; do not modify navmesh, AI, player, camera, food truck, scene setup, or unrelated systems.
- Keep URP-compatible material logic (`_BaseColor`, URP Lit first) to avoid pink materials.
- Prefer reusable pooled textures/materials over per-building instances.
- Keep hierarchy deterministic and debuggable (`Building_bx_bz_ix_iz_seedHash` naming).
- If a feature risks budget overrun, apply downgrade chain rather than adding new systems.

---

## Done Criteria

- All Task commits exist in order.
- `CityBuilder` produces stylized modular buildings with procedural facade variation.
- Determinism + fallback tests pass.
- Balanced profile is within defined performance budgets or degrades gracefully without fatal build failure.
