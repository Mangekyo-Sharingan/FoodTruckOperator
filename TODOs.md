# Food Truck Operator - TODO List

A Unity-based food truck simulation game featuring city generation, player movement, dynamic weather, and day/night cycles.

---

## Project Overview

- **Engine:** Unity 2022.3+
- **Render Pipeline:** Universal Render Pipeline (URP)
- **Target Platform:** PC / Mobile
- **Genre:** Simulation / Casual Game

---

## Core Features

### 1. City Generation & Building System

- [x] Procedural city grid generation
- [x] Randomized building heights and colors
- [x] CityBuilder.cs with optimized rendering
- [x] Multiple building color schemes

### 2. Player Controller

- [x] Third-person character movement
- [x] Smooth camera following
- [x] Ground detection for proper movement
- [x] Jump mechanic

### 3. Food Truck System

- [ ] Food truck parking mechanics
- [ ] Truck customization/upgrade system
- [ ] Interior interaction zones
- [ ] Mobile kitchen equipment

### 4. Game Economy

- [x] Money tracking system
- [x] Day/time progression
- [x] GameManager singleton for state
- [ ] Customer AI system
- [ ] Recipe/menu system
- [ ] Profit calculation

### 5. UI System

- [x] Minimal HUD (Day, Money, Time)
- [x] OnGUI-based display
- [x] DontDestroyOnLoad for persistence

### 6. Environmental Systems

#### Day/Night Cycle
- [x] 90-second full day cycle
- [x] Sun position rotation
- [x] Ambient light color changes
- [x] Fog adjustments

#### Weather System
- [x] Sunny/Clear state
- [x] Cloudy state with transitions
- [x] Rainy state with particle effects
- [x] Weather randomization

---

## Technical Implementation

### Scripts Overview

| Script | Status | Description |
|--------|--------|-------------|
| `GameManager.cs` | ✅ | Singleton for game state (money, day, time) |
| `GameUI.cs` | ✅ | OnGUI-based HUD display |
| `CityBuilder.cs` | ✅ | Procedural city generation |
| `PlayerController.cs` | ✅ | Third-person movement |
| `ThirdPersonCamera.cs` | ✅ | Camera follow system |
| `FoodTruck.cs` | ✅ | Food truck base class |
| `FoodTruckDriving.cs` | ✅ | Truck vehicle physics |
| `InteractionSystem.cs` | ✅ | Player interaction system |
| `IInteractable.cs` | ✅ | Interface for interactable objects |
| `DayNightCycle.cs` | ✅ | Day/night progression |
| `WeatherSystem.cs` | ✅ | Weather state machine |
| `SceneSetupEditor.cs` | ✅ | Editor scene automation |

### Performance Optimizations (COMPLETED)

- [x] CityBuilder - Reverse iteration for Destroy loops
- [x] CityBuilder - Static readonly color array
- [x] CityBuilder - Cached shader reference
- [x] ThirdPersonCamera - Merged Update into LateUpdate
- [x] PlayerController - Removed redundant Camera.main
- [x] SceneSetupEditor - Cached shader and Light references

---

## Game Loop

### Day Cycle
1. Morning - Shop opens, weather set
2. Midday - Peak hours begin
3. Evening - Customer rush
4. Night - Day ends, earnings calculated

### Player Actions
- Drive food truck to locations
- Set up shop at designated spots
- Interact with customers
- Serve food items
- Earn money
- Upgrade equipment

---

## Assets & Resources

### Materials
- TruckWindow.mat
- TruckMetal.mat
- TruckInterior.mat
- TruckWheel.mat
- WheelHub.mat

### Scenes
- CityScene.unity (Main gameplay)
- SampleScene.unity (Template)

### Settings
- URP Assets (PC & Mobile)
- Renderer configurations

---

## Future Enhancements

### High Priority
- [ ] Customer spawning and queue system
- [ ] Food cooking mechanics
- [ ] Recipe unlock system
- [ ] Day summary screen

### Medium Priority
- [ ] Sound effects and music
- [ ] Particle effects for food
- [ ] NPC dialogue system
- [ ] Save/Load system

### Low Priority
- [ ] Multi-language support
- [ ] Achievements system
- [ ] Leaderboards
- [ ] Steam/cloud integration

---

## Development Notes

### Known Issues
- ParticleSystem emission API requires direct access (not main module)
- Unity MCP tools needed for full scene manipulation

### Testing Checklist
- [ ] City generates correctly
- [ ] Player moves smoothly
- [ ] Food truck drives properly
- [ ] Day/night cycle works
- [ ] Weather transitions smoothly
- [ ] UI displays all values

---

## Version History

### v0.1.0 - Initial Setup
- Basic city generation
- Player controller
- Day/night cycle
- Weather system
- Basic UI

---

*Last Updated: March 2026*
*Generated for GitHub Repository*
