#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Menu: FoodTruck > Setup City Scene
/// Builds: Lighting, City, Player, Food Truck, Interior Room, Camera.
/// </summary>
public static class SceneSetupEditor
{
    static Shader _cachedShader;

    [MenuItem("FoodTruck/Setup City Scene")]
    public static void SetupCityScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        _cachedShader = null;

        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        { AssetDatabase.CreateFolder("Assets", "Materials"); AssetDatabase.Refresh(); }
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        SetupLighting();
        SetupCity();
        GameObject player     = SetupPlayer();
        GameObject truck      = SetupFoodTruck();
        GameObject interiorGO = SetupInteriorRoom(truck.GetComponent<FoodTruck>());
        SetupCamera(player);
        AddInteractionSystem(player);
        SetupGameSystems();

        // Wire interior room reference on the truck
        truck.GetComponent<FoodTruck>().interiorRoomRoot = interiorGO;

        EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene(),
            "Assets/Scenes/CityScene.unity");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FoodTruck] Scene built → Assets/Scenes/CityScene.unity");
    }

    // ── Game Systems ─────────────────────────────────────────────────────

    static void SetupGameSystems()
    {
        var gameMgr = new GameObject("GameManager");
        gameMgr.AddComponent<GameManager>();
        Debug.Log("[SceneSetup] Created GameManager");

        var gameUI = new GameObject("GameUI");
        gameUI.AddComponent<GameUI>();
        Debug.Log("[SceneSetup] Created GameUI");
    }

    // ── Lighting & Weather ─────────────────────────────────────────────────

    static void SetupLighting()
    {
        Light sun = null;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { sun = l; break; }

        if (sun == null)
        {
            var sunGO = new GameObject("Sun");
            sun = sunGO.AddComponent<Light>();
            sun.type = LightType.Directional;
        }

        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        sun.intensity = 1.1f;
        sun.color = new Color(1f, 0.96f, 0.88f);
        sun.shadows = LightShadows.Soft;
        sun.gameObject.name = "Sun";

        // Add Day-Night cycle
        var dayNight = sun.gameObject.AddComponent<DayNightCycle>();
        dayNight.sun = sun;
        dayNight.cycleDuration = 90f;

        // Add Weather system
        var weather = sun.gameObject.AddComponent<WeatherSystem>();
        weather.sun = sun;
        weather.changeInterval = 25f;

        // Enable fog
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.47f, 0.55f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.72f, 0.75f, 0.80f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.005f;
    }

    // ── City ──────────────────────────────────────────────────────────────

    static void SetupCity()
    {
        var cityGO = new GameObject("City");
        cityGO.AddComponent<CityBuilder>().Build();
    }

    // ── Player ────────────────────────────────────────────────────────────

    static GameObject SetupPlayer()
    {
        var player = new GameObject("Player");
        player.transform.position = new Vector3(3f, 0f, 55f);

        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.3f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.stepOffset = 0.4f; cc.slopeLimit = 45f;

        BuildCharacterModel(player.transform);

        var pi = player.AddComponent<PlayerInput>();
        var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
            "Assets/InputSystem_Actions.inputactions");
        if (actions != null)
        {
            pi.actions = actions;
            pi.defaultActionMap = "Player";
            pi.notificationBehavior = PlayerNotifications.SendMessages;
        }

        player.AddComponent<PlayerController>();
        return player;
    }

    static void AddInteractionSystem(GameObject player)
    {
        var ia = player.AddComponent<InteractionSystem>();
        ia.interactRadius = 2.5f;
    }

    // ── Food Truck ────────────────────────────────────────────────────────
    // Truck parked on the road, 2 cells ahead of the player start.
    // Player starts at z=55, so put the truck at z=42 (facing +Z).
    //
    // Truck is front-facing +Z:
    //   Cab   at local z = +3.25 (front)
    //   Body  at local z = -1.5  (back)
    //   Total length ≈ 10 m
    //
    // Serving window on the right side (local +X), facing +X.

    static GameObject SetupFoodTruck()
    {
        var truckColor = new Color(0.95f, 0.70f, 0.10f);  // bright yellow
        var cabColor   = new Color(0.92f, 0.65f, 0.10f);
        var wheelColor = new Color(0.15f, 0.15f, 0.15f);
        var windowColor= new Color(0.55f, 0.75f, 0.85f);
        var metalColor = new Color(0.55f, 0.55f, 0.55f);

        Material truckMat  = MakeMat("TruckBody",   truckColor,  0.3f);
        Material cabMat    = MakeMat("TruckCab",    cabColor,    0.25f);
        Material wheelMat  = MakeMat("TruckWheel",  wheelColor,  0.6f);
        Material windowMat = MakeMat("TruckWindow", windowColor, 0.9f);
        Material metalMat  = MakeMat("TruckMetal",  metalColor,  0.7f);
        Material interiorMat = MakeMat("TruckInterior", new Color(0.88f, 0.84f, 0.78f), 0.1f);
        Material awningMat = MakeMat("TruckAwning", new Color(0.85f, 0.18f, 0.10f), 0.15f);

        // ── Root ──────────────────────────────────────────────────────────
        var root = new GameObject("FoodTruck");
        root.transform.position = new Vector3(3f, 0f, 42f);
        // Face -Z so serving window (right side = +X) faces the player walking from z=55
        root.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        var truck   = root.AddComponent<FoodTruck>();
        var driving = root.AddComponent<FoodTruckDriving>();

        // ── Main collider box ─────────────────────────────────────────────
        var bodyCol = root.AddComponent<BoxCollider>();
        bodyCol.center = new Vector3(0f, 1.5f, 0f);
        bodyCol.size   = new Vector3(3.6f, 3.0f, 10.0f);

        // Rigidbody is auto-added by [RequireComponent] on FoodTruckDriving
        var rb = root.GetComponent<Rigidbody>() ?? root.AddComponent<Rigidbody>();
        rb.mass = 1500f;
        rb.linearDamping = 2f;
        rb.angularDamping = 8f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ
                       | RigidbodyConstraints.FreezePositionY;
        rb.isKinematic = true;

        // ── Exterior Visual ───────────────────────────────────────────────
        var ext = new GameObject("Exterior");
        ext.transform.SetParent(root.transform);
        ext.transform.localPosition = Vector3.zero;

        // Cargo body (rear 3/4 of truck)
        MakePart("CargoBody", ext.transform,
            new Vector3(0f, 1.65f, -1.3f), new Vector3(3.4f, 2.7f, 7.0f), truckMat);

        // Cab (front)
        MakePart("Cab", ext.transform,
            new Vector3(0f, 1.55f, 3.3f), new Vector3(3.4f, 2.5f, 3.4f), cabMat);

        // Windshield
        MakePart("Windshield", ext.transform,
            new Vector3(0f, 2.0f, 4.95f), new Vector3(2.6f, 1.2f, 0.08f), windowMat);

        // Cab side windows
        MakePart("WindowL", ext.transform,
            new Vector3(-1.72f, 1.95f, 3.7f), new Vector3(0.08f, 1.0f, 1.8f), windowMat);
        MakePart("WindowR", ext.transform,
            new Vector3( 1.72f, 1.95f, 3.7f), new Vector3(0.08f, 1.0f, 1.8f), windowMat);

        // Serving window (right side of cargo body, cut-out area)
        MakePart("ServingWindowFrame", ext.transform,
            new Vector3(1.71f, 2.0f, -0.8f), new Vector3(0.08f, 1.2f, 2.6f), metalMat);
        MakePart("ServingWindowGlass", ext.transform,
            new Vector3(1.72f, 2.0f, -0.8f), new Vector3(0.04f, 1.05f, 2.4f), windowMat);

        // Awning over serving window
        MakePart("Awning", ext.transform,
            new Vector3(2.3f, 2.85f, -0.8f), new Vector3(1.3f, 0.12f, 2.8f), awningMat);
        MakePart("AwningStripe1", ext.transform,
            new Vector3(2.3f, 2.85f, -0.1f), new Vector3(1.3f, 0.13f, 0.15f), truckMat);
        MakePart("AwningStripe2", ext.transform,
            new Vector3(2.3f, 2.85f, -1.5f), new Vector3(1.3f, 0.13f, 0.15f), truckMat);

        // Roof
        MakePart("Roof", ext.transform,
            new Vector3(0f, 3.02f, 0.5f), new Vector3(3.5f, 0.14f, 10.2f), metalMat);

        // Bumpers
        MakePart("FrontBumper", ext.transform,
            new Vector3(0f, 0.5f, 5.1f), new Vector3(3.2f, 0.4f, 0.3f), metalMat);
        MakePart("RearBumper", ext.transform,
            new Vector3(0f, 0.5f, -4.9f), new Vector3(3.2f, 0.4f, 0.3f), metalMat);

        // Underbody / chassis
        MakePart("Chassis", ext.transform,
            new Vector3(0f, 0.28f, 0.5f), new Vector3(3.0f, 0.3f, 9.6f), metalMat);

        // Wheels (cylinders rotated 90° on X)
        Transform wFL = MakeWheel("WheelFL", ext.transform, new Vector3(-1.6f, 0.38f,  3.0f), wheelMat);
        Transform wFR = MakeWheel("WheelFR", ext.transform, new Vector3( 1.6f, 0.38f,  3.0f), wheelMat);
        MakeWheel("WheelBL", ext.transform, new Vector3(-1.6f, 0.38f, -2.5f), wheelMat);
        MakeWheel("WheelBR", ext.transform, new Vector3( 1.6f, 0.38f, -2.5f), wheelMat);

        driving.wheelFL = wFL;
        driving.wheelFR = wFR;

        // Remove colliders from all exterior visuals
        foreach (var c in ext.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(c);

        // ── Exterior door trigger — spans the full rear face of the truck ────
        // Truck rear is local z = -5.0 (world +Z from truck origin).
        // Player approaches from world +Z, so this is directly in their path.
        var doorTriggerGO = new GameObject("ExteriorDoorTrigger");
        doorTriggerGO.transform.SetParent(root.transform);
        doorTriggerGO.transform.localPosition = new Vector3(0f, 1.5f, -5.3f);
        var doorCol = doorTriggerGO.AddComponent<BoxCollider>();
        doorCol.size = new Vector3(4.0f, 3.0f, 1.5f);   // wide zone across the rear
        doorCol.isTrigger = true;
        var doorInteract = doorTriggerGO.AddComponent<TruckExteriorDoor>();
        doorInteract.truck = truck;

        // ── Exterior door point — player exits to the side of the truck ──────
        var extDoorPoint = new GameObject("ExteriorDoorPoint");
        extDoorPoint.transform.SetParent(root.transform);
        extDoorPoint.transform.localPosition = new Vector3(2.5f, 0f, -2.2f);
        truck.exteriorDoorPoint = extDoorPoint.transform;

        // ── Camera target for driving ─────────────────────────────────────
        var camTarget = new GameObject("CameraTarget");
        camTarget.transform.SetParent(root.transform);
        camTarget.transform.localPosition = new Vector3(0f, 2.5f, 0f);
        truck.cameraTarget = camTarget.transform;

        return root;
    }

    static Transform MakeWheel(string name, Transform parent, Vector3 localPos, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        go.transform.localScale = new Vector3(0.76f, 0.22f, 0.76f);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<Collider>());

        // Hubcap
        var hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hub.name = "Hub";
        hub.transform.SetParent(go.transform);
        hub.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        hub.transform.localScale    = new Vector3(0.4f, 0.15f, 0.4f);
        hub.GetComponent<Renderer>().sharedMaterial =
            MakeMat("WheelHub", new Color(0.75f, 0.75f, 0.75f), 0.8f);
        Object.DestroyImmediate(hub.GetComponent<Collider>());

        return go.transform;
    }

    // ── Interior Room ─────────────────────────────────────────────────────
    // Parented to the truck, positioned at local (0,0,-0.5) inside the cargo body.
    // Dimensions kept within the truck's exterior 3.6 × 3.0 × 10.0 collider.

    static GameObject SetupInteriorRoom(FoodTruck truck)
    {
        const float IW = 2.8f;   // interior width  (X)  — fits inside 3.4 m body
        const float IL = 7.0f;   // interior length (Z)  — spans cargo area
        const float IH = 2.6f;   // interior height (Y)  — fits inside 2.7 m body
        const float T  = 0.15f;  // wall thickness

        Material wallMat   = MakeMat("InteriorWall",   new Color(0.95f, 0.92f, 0.86f), 0.1f);
        Material floorMat  = MakeMat("InteriorFloor",  new Color(0.60f, 0.55f, 0.45f), 0.4f);
        Material equipMat  = MakeMat("CookingEquip",   new Color(0.75f, 0.75f, 0.78f), 0.8f);
        Material counterMat= MakeMat("Counter",        new Color(0.50f, 0.45f, 0.40f), 0.3f);
        Material stoveMat  = MakeMat("Stove",          new Color(0.25f, 0.25f, 0.28f), 0.7f);
        Material signMat   = MakeMat("ExitSign",       new Color(0.10f, 0.70f, 0.20f), 0.2f);

        // Parent directly to the truck so it moves with it
        var room = new GameObject("TruckInteriorRoom");
        room.transform.SetParent(truck.transform);
        room.transform.localPosition = new Vector3(0f, 0f, -0.5f);  // centred on cargo body
        room.transform.localRotation = Quaternion.identity;

        // Floor
        MakeRoomPart("Floor",   room.transform, new Vector3(0, T/2, 0),
            new Vector3(IW, T, IL), floorMat);

        // Ceiling
        MakeRoomPart("Ceiling", room.transform, new Vector3(0, IH - T/2, 0),
            new Vector3(IW + T*2, T, IL + T*2), wallMat);

        // Walls
        MakeRoomPart("WallLeft",  room.transform, new Vector3(-IW/2 - T/2, IH/2, 0),
            new Vector3(T, IH, IL), wallMat);
        MakeRoomPart("WallRight", room.transform, new Vector3( IW/2 + T/2, IH/2, 0),
            new Vector3(T, IH, IL), wallMat);
        MakeRoomPart("WallBack",  room.transform, new Vector3(0, IH/2, -IL/2 - T/2),
            new Vector3(IW + T*2, IH, T), wallMat);
        // Front wall (cab side) — partial, has a "windshield" opening hint
        MakeRoomPart("WallFront", room.transform, new Vector3(0, IH/2, IL/2 + T/2),
            new Vector3(IW + T*2, IH, T), wallMat);

        // ── Counter / cooking station along left wall ─────────────────────
        // Counter runs most of the length
        MakeRoomPart("Counter",        room.transform, new Vector3(-IW/2 + 0.45f, 1.0f,  0.5f),
            new Vector3(0.9f, 1.0f, 5.0f), counterMat);
        MakeRoomPart("CounterTop",     room.transform, new Vector3(-IW/2 + 0.45f, 1.52f,  0.5f),
            new Vector3(0.92f, 0.06f, 5.02f), equipMat);

        // Stove / griddle
        MakeRoomPart("Stove",          room.transform, new Vector3(-IW/2 + 0.45f, 1.6f, -0.8f),
            new Vector3(0.85f, 0.12f, 1.4f), stoveMat);
        MakeRoomPart("StoveBack",      room.transform, new Vector3(-IW/2 + 0.2f, 1.9f, -0.8f),
            new Vector3(0.1f, 0.8f, 1.4f), equipMat);

        // Fryer
        MakeRoomPart("Fryer",          room.transform, new Vector3(-IW/2 + 0.45f, 1.6f, 1.6f),
            new Vector3(0.7f, 0.5f, 0.7f), stoveMat);

        // Serving shelf (right side — serving window side)
        MakeRoomPart("ServingShelf",   room.transform, new Vector3(IW/2 - 0.3f, 1.2f, -0.8f),
            new Vector3(0.6f, 0.08f, 2.4f), counterMat);

        // Overhead shelves (below ceiling inner face at IH - T = 2.45)
        MakeRoomPart("ShelfHigh1",     room.transform, new Vector3(-IW/2 + 0.2f, 2.1f, 1.6f),
            new Vector3(0.25f, 0.08f, 0.8f), equipMat);
        MakeRoomPart("ShelfHigh2",     room.transform, new Vector3(-IW/2 + 0.2f, 2.1f, 0.5f),
            new Vector3(0.25f, 0.08f, 0.8f), equipMat);

        // ── Driver seat area (front = +Z end, all within IL/2 = 3.5) ──────
        MakeRoomPart("Seat",           room.transform, new Vector3(-0.5f, 0.5f,  2.8f),
            new Vector3(0.8f, 0.5f, 0.8f), counterMat);
        MakeRoomPart("SeatBack",       room.transform, new Vector3(-0.5f, 1.0f,  3.2f),
            new Vector3(0.8f, 0.8f, 0.15f), counterMat);
        // Steering wheel hint
        MakeRoomPart("SteeringWheel",  room.transform, new Vector3(-0.5f, 1.4f,  2.6f),
            new Vector3(0.5f, 0.5f, 0.06f), stoveMat);
        MakeRoomPart("SteeringCol",    room.transform, new Vector3(-0.5f, 1.1f,  2.65f),
            new Vector3(0.06f, 0.5f, 0.06f), stoveMat);

        // ── Exit door (side, mid-room) — green marker ─────────────────────
        MakeRoomPart("ExitDoorMarker", room.transform, new Vector3(IW/2 - 0.05f, 1.5f, -2.2f),
            new Vector3(0.12f, 2.0f, 1.0f), signMat);

        // ── Trigger colliders ─────────────────────────────────────────────

        // Exit door trigger — inside the right wall, player walks into it to leave
        var exitGO = new GameObject("ExitDoorTrigger");
        exitGO.transform.SetParent(room.transform);
        exitGO.transform.localPosition = new Vector3(IW/2 - 0.3f, 1.0f, -2.2f);
        var exitCol = exitGO.AddComponent<BoxCollider>();
        exitCol.size = new Vector3(0.8f, 2.0f, 1.2f);
        exitCol.isTrigger = true;
        var exitInteract = exitGO.AddComponent<TruckInteriorExit>();
        exitInteract.truck = truck;

        // Driver seat trigger
        var driverGO = new GameObject("DriverSeatTrigger");
        driverGO.transform.SetParent(room.transform);
        driverGO.transform.localPosition = new Vector3(-0.5f, 1.0f, 2.8f);
        var driverCol = driverGO.AddComponent<BoxCollider>();
        driverCol.size = new Vector3(1.2f, 1.5f, 1.2f);
        driverCol.isTrigger = true;
        var driverInteract = driverGO.AddComponent<TruckDriverSeat>();
        driverInteract.truck = truck;

        // ── Interior lighting ─────────────────────────────────────────────
        var lightGO = new GameObject("InteriorLight");
        lightGO.transform.SetParent(room.transform);
        lightGO.transform.localPosition = new Vector3(0f, IH - 0.3f, 0f);
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 10f;
        light.intensity = 2.5f;
        light.color = new Color(1f, 0.97f, 0.88f);
        light.shadows = LightShadows.None;

        // ── Spawn points ──────────────────────────────────────────────────
        var interiorSpawn = new GameObject("InteriorSpawnPoint");
        interiorSpawn.transform.SetParent(room.transform);
        interiorSpawn.transform.localPosition = new Vector3(0.6f, 0.2f, -2.0f);  // near exit door, above floor
        interiorSpawn.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);   // facing inward

        var driverSpawn = new GameObject("DriverSpawnPoint");
        driverSpawn.transform.SetParent(room.transform);
        driverSpawn.transform.localPosition = new Vector3(-0.5f, 0.2f, 2.5f);

        // Wire spawn points on truck
        truck.interiorSpawnPoint = interiorSpawn.transform;
        truck.driverSeatPoint    = driverSpawn.transform;

        return room;
    }

    static GameObject MakeRoomPart(string name, Transform parent, Vector3 localPos,
        Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    // ── Character Model ───────────────────────────────────────────────────

    static void BuildCharacterModel(Transform parent)
    {
        var model = new GameObject("Model");
        model.transform.SetParent(parent);
        model.transform.localPosition = Vector3.zero;

        Material skin  = MakeMat("CharSkin",  new Color(0.91f, 0.76f, 0.60f), 0.3f);
        Material cloth = MakeMat("CharCloth", new Color(0.22f, 0.35f, 0.55f), 0.15f);
        Material hat   = MakeMat("CharHat",   new Color(0.85f, 0.32f, 0.10f), 0.2f);

        MakePart("Torso",   model.transform, new Vector3(0f,    1.10f,  0f),    new Vector3(0.55f, 0.65f, 0.30f), cloth);
        MakePart("Head",    model.transform, new Vector3(0f,    1.72f,  0f),    new Vector3(0.38f, 0.38f, 0.38f), skin,  PrimitiveType.Sphere);
        MakePart("HatBrim", model.transform, new Vector3(0f,    1.92f,  0f),    new Vector3(0.48f, 0.07f, 0.48f), hat);
        MakePart("HatTop",  model.transform, new Vector3(0f,    2.10f,  0f),    new Vector3(0.30f, 0.38f, 0.30f), hat);
        MakePart("ArmL",    model.transform, new Vector3(-0.38f,1.05f,  0f),    new Vector3(0.18f, 0.60f, 0.18f), cloth);
        MakePart("ArmR",    model.transform, new Vector3( 0.38f,1.05f,  0f),    new Vector3(0.18f, 0.60f, 0.18f), cloth);
        MakePart("HandL",   model.transform, new Vector3(-0.38f,0.70f,  0f),    new Vector3(0.16f, 0.16f, 0.16f), skin,  PrimitiveType.Sphere);
        MakePart("HandR",   model.transform, new Vector3( 0.38f,0.70f,  0f),    new Vector3(0.16f, 0.16f, 0.16f), skin,  PrimitiveType.Sphere);
        MakePart("LegL",    model.transform, new Vector3(-0.16f,0.42f,  0f),    new Vector3(0.22f, 0.72f, 0.22f), cloth);
        MakePart("LegR",    model.transform, new Vector3( 0.16f,0.42f,  0f),    new Vector3(0.22f, 0.72f, 0.22f), cloth);
        MakePart("ShoeL",   model.transform, new Vector3(-0.16f,0.06f,  0.06f), new Vector3(0.22f, 0.12f, 0.32f), hat);
        MakePart("ShoeR",   model.transform, new Vector3( 0.16f,0.06f,  0.06f), new Vector3(0.22f, 0.12f, 0.32f), hat);

        foreach (var col in model.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(col);
    }

    // ── Camera ────────────────────────────────────────────────────────────

    static void SetupCamera(GameObject player)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            cam = camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
        }

        cam.transform.position = new Vector3(0f, 6f, 45f);
        cam.fieldOfView = 65f;

        var tpc = cam.gameObject.AddComponent<ThirdPersonCamera>();
        tpc.target = player.transform;
        tpc.distance = 6f;
        tpc.minDistance = 2f;
        tpc.maxDistance = 14f;
    }

    // ── Shared helpers ────────────────────────────────────────────────────

    static Material MakeMat(string name, Color color, float smoothness)
    {
        if (_cachedShader == null)
            _cachedShader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard");

        string path = $"Assets/Materials/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            existing.shader = _cachedShader;
            existing.SetColor("_BaseColor", color);
            existing.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(existing);
            return existing;
        }
        var mat = new Material(_cachedShader);
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", smoothness);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static GameObject MakePart(string name, Transform parent, Vector3 pos, Vector3 scale,
        Material mat, PrimitiveType type = PrimitiveType.Cube)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        var col = go.GetComponent<Collider>();
        if (col) Object.DestroyImmediate(col);
        return go;
    }
}
#endif
