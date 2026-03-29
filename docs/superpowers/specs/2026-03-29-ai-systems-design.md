# AI Systems Design - Food Truck Operator

**Date:** March 29, 2026  
**Status:** Approved

---

## Overview

Add AI characters to populate the city: wandering pedestrians and visual-only customers walking past the food truck. Uses object pooling for performance.

---

## Architecture

### Components

| Script | Responsibility |
|--------|---------------|
| `AISpawner` | Singleton managing spawn pools for pedestrians and customers |
| `PedestrianAI` | Wanders to random points within city bounds |
| `CustomerAI` | Spawns at edge, walks past truck area, despawns at opposite edge |
| `AIPool` | Generic object pool for reusing AI GameObjects |

### Data Flow

```
Scene Start
    ↓
AISpawner.Initialize()
    ├── Create pedestrian pool (20 objects)
    └── Create customer pool (20 objects)
    ↓
Update Loop
    ├── Spawn pedestrians if below target count
    ├── Spawn customers if below target count
    ├── Update each AI (move, check bounds)
    └── Return idle AI to pool
```

---

## Pedestrian AI

### Behavior
1. Spawn at random position within city bounds
2. Pick random destination within city bounds
3. Walk to destination using NavMeshAgent
4. Upon arrival, wait 1-2 seconds
5. Pick new destination, repeat
6. If distance from city center > 80 units, despawn

### Parameters
- **Speed:** 2 units/second
- **Spawn count:** 20 active
- **Waypoint wait:** 1-2 seconds random
- **Despawn radius:** 80 units from origin

### Visual
- Capsule mesh
- Blue material (#3498db)
- Scale: 1.0 x 1.8 x 1.0

---

## Customer AI

### Behavior
1. Spawn at random edge of city (N/S/E/W)
2. Walk toward opposite edge, passing near food truck area (0, 0, 0)
3. Despawn when reaching opposite edge

### Parameters
- **Speed:** 2.5 units/second (slightly faster)
- **Spawn count:** 20 active
- **Path:** Always passes within 15 units of truck

### Visual
- Capsule mesh
- Green material (#2ecc71)
- Scale: 1.0 x 1.8 x 1.0

---

## Object Pooling

- Pre-spawn 25 of each type (buffer above max)
- When AI needs despawn, return to pool
- When spawning, take from pool first
- Only activate NavMeshAgent when in use
- Disabled objects hidden via SetActive(false)

---

## Integration

### Scene Setup
- Add `AISpawner` to scene via `SceneSetupEditor`
- City bounds defined as: X: -50 to 50, Z: -50 to 50
- Food truck area: radius 15 around (0, 0, 0)

### NavMesh
- Bake NavMesh on city ground plane
- Use NavMeshAgent for all movement

---

## Performance Considerations

- Object pooling eliminates runtime Instantiate/Destroy
- Cached NavMeshAgent reference
- No per-frame calculations in AI scripts
- Target: 40 simultaneous AI with smooth framerate

---

## Acceptance Criteria

- [ ] 20 pedestrians wandering randomly in city
- [ ] 20 customers walking past truck area
- [ ] Smooth NavMesh movement
- [ ] No GC spikes from spawning/despawning
- [ ] AI despawns when leaving play area
- [ ] Works with existing city generation
