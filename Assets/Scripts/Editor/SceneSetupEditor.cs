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

    [MenuItem("FoodTruck/Setup AI Systems")]
    public static void SetupAISystems()
    {
        var spawnerObj = new GameObject("AISpawner");
        var spawner = spawnerObj.AddComponent<AISpawner>();

        var pedMat = new Material(_cachedShader ?? Shader.Find("Universal Render Pipeline/Lit"));
        pedMat.SetColor("_BaseColor", new Color(0.204f, 0.596f, 0.859f));
        AssetDatabase.CreateAsset(pedMat, "Assets/Materials/Pedestrian.mat");

        var custMat = new Material(_cachedShader ?? Shader.Find("Universal Render Pipeline/Lit"));
        custMat.SetColor("_BaseColor", new Color(0.18f, 0.8f, 0.443f));
        AssetDatabase.CreateAsset(custMat, "Assets/Materials/Customer.mat");

        spawner.SetMaterials(pedMat, custMat);

        AssetDatabase.SaveAssets();
        Debug.Log("[SceneSetup] AI Systems setup complete");
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
        SetupNavMesh(cityGO);
    }

    static void SetupNavMesh(GameObject cityGO)
    {
#if UNITY_EDITOR
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        Debug.Log("[SceneSetup] NavMesh baked");
#endif
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

    // ── Food Truck (HD Low-Poly) ─────────────────────────────────────────
    //
    // Exterior built from ~75 primitives with visible panel lines, trim,
    // chrome details, headlights, grille, mirrors, fenders, awning, etc.
    //
    // Local +Z = cab (forward), −Z = cargo rear (entry door).
    // Serving window on right side (+X).
    // Approx 10 m long, 3.4 m wide, 3.0 m tall.

    static GameObject SetupFoodTruck()
    {
        // ── Materials ────────────────────────────────────────────────────
        Material bodyMat      = MakeMat("TruckBody",     new Color(0.95f, 0.70f, 0.10f), 0.30f);
        Material bodyAccent   = MakeMat("TruckAccent",   new Color(0.82f, 0.58f, 0.08f), 0.25f);
        Material cabMat       = MakeMat("TruckCab",      new Color(0.92f, 0.65f, 0.10f), 0.28f);
        Material wheelMat     = MakeMat("TruckWheel",    new Color(0.10f, 0.10f, 0.10f), 0.40f);
        Material windowMat    = MakeMat("TruckWindow",   new Color(0.55f, 0.78f, 0.90f), 0.92f);
        Material metalMat     = MakeMat("TruckMetal",    new Color(0.60f, 0.60f, 0.62f), 0.75f);
        Material chromeMat    = MakeMat("TruckChrome",   new Color(0.82f, 0.82f, 0.85f), 0.92f);
        Material trimMat      = MakeMat("TruckTrim",     new Color(0.25f, 0.25f, 0.27f), 0.55f);
        Material headlightMat = MakeMat("Headlight",     new Color(1.0f,  0.97f, 0.85f), 0.95f);
        Material taillightMat = MakeMat("Taillight",     new Color(0.90f, 0.05f, 0.05f), 0.70f);
        Material indicatorMat = MakeMat("Indicator",     new Color(1.0f,  0.60f, 0.05f), 0.75f);
        Material awningMat    = MakeMat("Awning",        new Color(0.85f, 0.18f, 0.10f), 0.15f);
        Material stepMat      = MakeMat("Step",          new Color(0.38f, 0.38f, 0.40f), 0.50f);

        // ── Root + physics ──────────────────────────────────────────────
        var root = new GameObject("FoodTruck");
        root.transform.position = new Vector3(3f, 0f, 42f);
        root.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        var truck   = root.AddComponent<FoodTruck>();
        var driving = root.AddComponent<FoodTruckDriving>();

        var bodyCol = root.AddComponent<BoxCollider>();
        bodyCol.center = new Vector3(0f, 1.5f, 0f);
        bodyCol.size   = new Vector3(3.6f, 3.0f, 10.0f);

        var rb = root.GetComponent<Rigidbody>() ?? root.AddComponent<Rigidbody>();
        rb.mass          = 1500f;
        rb.linearDamping = 2f;
        rb.angularDamping = 8f;
        rb.constraints   = RigidbodyConstraints.FreezeRotationX
                         | RigidbodyConstraints.FreezeRotationZ
                         | RigidbodyConstraints.FreezePositionY;
        rb.isKinematic   = true;

        var ext = new GameObject("Exterior");
        ext.transform.SetParent(root.transform);
        ext.transform.localPosition = Vector3.zero;

        // ═══════════════════════════════════════════════════════════════
        //  CARGO BODY — z from −4.5 to +1.8
        // ═══════════════════════════════════════════════════════════════

        // ── Left side panels ──
        MakePart("CargoLeftLower", ext.transform,
            new Vector3(-1.68f, 1.0f, -1.35f), new Vector3(0.06f, 1.40f, 6.30f), bodyMat);
        MakePart("CargoLeftUpper", ext.transform,
            new Vector3(-1.68f, 2.35f, -1.35f), new Vector3(0.06f, 1.30f, 6.30f), bodyMat);
        MakePart("CargoLeftTrim", ext.transform,
            new Vector3(-1.69f, 1.72f, -1.35f), new Vector3(0.04f, 0.06f, 6.30f), trimMat);
        MakePart("CargoLeftAccent", ext.transform,
            new Vector3(-1.69f, 0.32f, -1.35f), new Vector3(0.04f, 0.04f, 6.30f), bodyAccent);

        // ── Right side panels — split around serving window ──
        MakePart("CargoRightLower", ext.transform,
            new Vector3(1.68f, 1.0f, -1.35f), new Vector3(0.06f, 1.40f, 6.30f), bodyMat);
        MakePart("CargoRightUpperRear", ext.transform,
            new Vector3(1.68f, 2.35f, -3.0f), new Vector3(0.06f, 1.30f, 3.00f), bodyMat);
        MakePart("CargoRightUpperFront", ext.transform,
            new Vector3(1.68f, 2.35f, 0.8f), new Vector3(0.06f, 1.30f, 2.00f), bodyMat);
        MakePart("CargoRightTrim", ext.transform,
            new Vector3(1.69f, 1.72f, -1.35f), new Vector3(0.04f, 0.06f, 6.30f), trimMat);
        MakePart("CargoRightAccent", ext.transform,
            new Vector3(1.69f, 0.32f, -1.35f), new Vector3(0.04f, 0.04f, 6.30f), bodyAccent);

        // ── Serving window area ──
        MakePart("WindowHeader", ext.transform,
            new Vector3(1.68f, 2.72f, -0.8f), new Vector3(0.06f, 0.56f, 2.60f), bodyMat);
        MakePart("WindowFrameTop", ext.transform,
            new Vector3(1.70f, 2.48f, -0.8f), new Vector3(0.04f, 0.06f, 2.50f), chromeMat);
        MakePart("WindowFrameBot", ext.transform,
            new Vector3(1.70f, 1.72f, -0.8f), new Vector3(0.04f, 0.06f, 2.50f), chromeMat);
        MakePart("WindowFrameL", ext.transform,
            new Vector3(1.70f, 2.10f, -2.03f), new Vector3(0.04f, 0.82f, 0.06f), chromeMat);
        MakePart("WindowFrameR", ext.transform,
            new Vector3(1.70f, 2.10f, 0.43f), new Vector3(0.04f, 0.82f, 0.06f), chromeMat);
        MakePart("WindowLedge", ext.transform,
            new Vector3(1.85f, 1.72f, -0.8f), new Vector3(0.30f, 0.06f, 2.40f), metalMat);

        // Hatch — pivot at top edge of window, panel hangs down
        var hatchPivotGO = new GameObject("ServingHatchPivot");
        hatchPivotGO.transform.SetParent(ext.transform);
        hatchPivotGO.transform.localPosition = new Vector3(1.72f, 2.46f, -0.8f);
        hatchPivotGO.transform.localRotation = Quaternion.identity;
        hatchPivotGO.AddComponent<ServingHatch>();
        MakePart("HatchPanel", hatchPivotGO.transform,
            new Vector3(0f, -0.37f, 0f), new Vector3(0.04f, 0.74f, 2.40f), windowMat);

        // ── Rear face ──
        MakePart("CargoRear", ext.transform,
            new Vector3(0f, 1.65f, -4.52f), new Vector3(3.30f, 2.70f, 0.06f), bodyMat);
        MakePart("RearDoorFrameL", ext.transform,
            new Vector3(-1.30f, 1.65f, -4.56f), new Vector3(0.08f, 2.60f, 0.04f), chromeMat);
        MakePart("RearDoorFrameR", ext.transform,
            new Vector3(1.30f, 1.65f, -4.56f), new Vector3(0.08f, 2.60f, 0.04f), chromeMat);
        MakePart("RearDoorFrameTop", ext.transform,
            new Vector3(0f, 2.96f, -4.56f), new Vector3(2.60f, 0.06f, 0.04f), chromeMat);
        MakePart("RearDoorHandle", ext.transform,
            new Vector3(0.05f, 1.50f, -4.58f), new Vector3(0.20f, 0.06f, 0.04f), chromeMat);

        // ── Cargo roof ──
        MakePart("CargoRoof", ext.transform,
            new Vector3(0f, 3.00f, -1.35f), new Vector3(3.42f, 0.08f, 6.40f), metalMat);
        MakePart("RoofTrimFront", ext.transform,
            new Vector3(0f, 3.04f, 1.50f), new Vector3(3.42f, 0.04f, 0.08f), trimMat);
        MakePart("RoofTrimRear", ext.transform,
            new Vector3(0f, 3.04f, -4.53f), new Vector3(3.42f, 0.04f, 0.06f), trimMat);
        // AC unit
        MakePart("ACUnit", ext.transform,
            new Vector3(0f, 3.15f, -2.50f), new Vector3(1.00f, 0.30f, 0.80f), metalMat);
        MakePart("ACVent", ext.transform,
            new Vector3(0f, 3.32f, -2.50f), new Vector3(0.60f, 0.06f, 0.50f), trimMat);

        // ── Cargo floor / chassis ──
        MakePart("CargoFloorExt", ext.transform,
            new Vector3(0f, 0.30f, -1.35f), new Vector3(3.30f, 0.06f, 6.30f), trimMat);
        MakePart("Chassis", ext.transform,
            new Vector3(0f, 0.18f, 0f), new Vector3(2.80f, 0.20f, 9.20f), trimMat);

        // ═══════════════════════════════════════════════════════════════
        //  CAB — z from +1.8 to +4.95
        // ═══════════════════════════════════════════════════════════════

        MakePart("CabBodyL", ext.transform,
            new Vector3(-1.62f, 1.50f, 3.35f), new Vector3(0.06f, 2.40f, 3.00f), cabMat);
        MakePart("CabBodyR", ext.transform,
            new Vector3(1.62f, 1.50f, 3.35f), new Vector3(0.06f, 2.40f, 3.00f), cabMat);
        MakePart("CabRoof", ext.transform,
            new Vector3(0f, 2.72f, 3.35f), new Vector3(3.30f, 0.06f, 3.00f), cabMat);

        // Hood
        MakePart("Hood", ext.transform,
            new Vector3(0f, 1.00f, 4.60f), new Vector3(3.10f, 0.06f, 0.80f), cabMat);
        MakePart("HoodFront", ext.transform,
            new Vector3(0f, 0.80f, 4.96f), new Vector3(3.10f, 0.50f, 0.06f), cabMat);

        // Grille
        MakePart("Grille", ext.transform,
            new Vector3(0f, 0.55f, 4.97f), new Vector3(2.40f, 0.50f, 0.04f), chromeMat);
        MakePart("GrilleSlat1", ext.transform,
            new Vector3(0f, 0.65f, 4.98f), new Vector3(2.20f, 0.04f, 0.02f), trimMat);
        MakePart("GrilleSlat2", ext.transform,
            new Vector3(0f, 0.50f, 4.98f), new Vector3(2.20f, 0.04f, 0.02f), trimMat);
        MakePart("GrilleSlat3", ext.transform,
            new Vector3(0f, 0.35f, 4.98f), new Vector3(2.20f, 0.04f, 0.02f), trimMat);

        // Windshield + A-pillars
        MakePart("Windshield", ext.transform,
            new Vector3(0f, 2.00f, 4.85f), new Vector3(2.50f, 1.20f, 0.04f), windowMat);
        MakePart("APillarL", ext.transform,
            new Vector3(-1.32f, 2.00f, 4.85f), new Vector3(0.10f, 1.30f, 0.08f), trimMat);
        MakePart("APillarR", ext.transform,
            new Vector3(1.32f, 2.00f, 4.85f), new Vector3(0.10f, 1.30f, 0.08f), trimMat);

        // Rear cab window
        MakePart("RearCabWindow", ext.transform,
            new Vector3(0f, 2.00f, 1.86f), new Vector3(2.00f, 0.90f, 0.04f), windowMat);

        // Cab side windows + doors
        MakePart("CabWindowL", ext.transform,
            new Vector3(-1.64f, 2.00f, 3.80f), new Vector3(0.04f, 0.90f, 1.60f), windowMat);
        MakePart("CabWindowR", ext.transform,
            new Vector3(1.64f, 2.00f, 3.80f), new Vector3(0.04f, 0.90f, 1.60f), windowMat);
        MakePart("DoorL", ext.transform,
            new Vector3(-1.66f, 1.20f, 3.50f), new Vector3(0.02f, 1.40f, 2.20f), cabMat);
        MakePart("DoorR", ext.transform,
            new Vector3(1.66f, 1.20f, 3.50f), new Vector3(0.02f, 1.40f, 2.20f), cabMat);
        MakePart("DoorHandleL", ext.transform,
            new Vector3(-1.68f, 1.30f, 3.20f), new Vector3(0.04f, 0.06f, 0.20f), chromeMat);
        MakePart("DoorHandleR", ext.transform,
            new Vector3(1.68f, 1.30f, 3.20f), new Vector3(0.04f, 0.06f, 0.20f), chromeMat);

        // Side mirrors
        MakePart("MirrorArmL", ext.transform,
            new Vector3(-1.85f, 1.90f, 4.50f), new Vector3(0.30f, 0.04f, 0.04f), trimMat);
        MakePart("MirrorL", ext.transform,
            new Vector3(-2.05f, 1.85f, 4.50f), new Vector3(0.04f, 0.20f, 0.16f), chromeMat);
        MakePart("MirrorArmR", ext.transform,
            new Vector3(1.85f, 1.90f, 4.50f), new Vector3(0.30f, 0.04f, 0.04f), trimMat);
        MakePart("MirrorR", ext.transform,
            new Vector3(2.05f, 1.85f, 4.50f), new Vector3(0.04f, 0.20f, 0.16f), chromeMat);

        // Sun visor
        MakePart("SunVisor", ext.transform,
            new Vector3(0f, 2.70f, 4.92f), new Vector3(2.60f, 0.12f, 0.25f), trimMat);

        // ═══════════════════════════════════════════════════════════════
        //  WHEELS
        // ═══════════════════════════════════════════════════════════════

        Transform wFL = MakeWheel("WheelFL", ext.transform, new Vector3(-1.55f, 0.38f, 3.50f), wheelMat);
        Transform wFR = MakeWheel("WheelFR", ext.transform, new Vector3(1.55f, 0.38f, 3.50f), wheelMat);
        MakeWheel("WheelBL", ext.transform, new Vector3(-1.55f, 0.38f, -3.00f), wheelMat);
        MakeWheel("WheelBR", ext.transform, new Vector3(1.55f, 0.38f, -3.00f), wheelMat);
        driving.wheelFL = wFL;
        driving.wheelFR = wFR;

        // Fenders (above each wheel)
        MakePart("FenderFL", ext.transform,
            new Vector3(-1.68f, 0.65f, 3.50f), new Vector3(0.06f, 0.10f, 0.90f), trimMat);
        MakePart("FenderFR", ext.transform,
            new Vector3(1.68f, 0.65f, 3.50f), new Vector3(0.06f, 0.10f, 0.90f), trimMat);
        MakePart("FenderBL", ext.transform,
            new Vector3(-1.68f, 0.65f, -3.00f), new Vector3(0.06f, 0.10f, 0.90f), trimMat);
        MakePart("FenderBR", ext.transform,
            new Vector3(1.68f, 0.65f, -3.00f), new Vector3(0.06f, 0.10f, 0.90f), trimMat);

        // ═══════════════════════════════════════════════════════════════
        //  DETAILS
        // ═══════════════════════════════════════════════════════════════

        // Headlights + turn signals
        MakePart("HeadlightL", ext.transform,
            new Vector3(-1.15f, 0.85f, 4.98f), new Vector3(0.35f, 0.25f, 0.04f), headlightMat);
        MakePart("HeadlightR", ext.transform,
            new Vector3(1.15f, 0.85f, 4.98f), new Vector3(0.35f, 0.25f, 0.04f), headlightMat);
        MakePart("TurnSignalFL", ext.transform,
            new Vector3(-1.35f, 0.50f, 4.98f), new Vector3(0.18f, 0.10f, 0.04f), indicatorMat);
        MakePart("TurnSignalFR", ext.transform,
            new Vector3(1.35f, 0.50f, 4.98f), new Vector3(0.18f, 0.10f, 0.04f), indicatorMat);

        // Taillights + rear indicators
        MakePart("TaillightL", ext.transform,
            new Vector3(-1.35f, 1.50f, -4.58f), new Vector3(0.25f, 0.50f, 0.04f), taillightMat);
        MakePart("TaillightR", ext.transform,
            new Vector3(1.35f, 1.50f, -4.58f), new Vector3(0.25f, 0.50f, 0.04f), taillightMat);
        MakePart("RearIndicatorL", ext.transform,
            new Vector3(-1.35f, 1.05f, -4.58f), new Vector3(0.25f, 0.18f, 0.04f), indicatorMat);
        MakePart("RearIndicatorR", ext.transform,
            new Vector3(1.35f, 1.05f, -4.58f), new Vector3(0.25f, 0.18f, 0.04f), indicatorMat);

        // Bumpers
        MakePart("FrontBumper", ext.transform,
            new Vector3(0f, 0.35f, 5.00f), new Vector3(3.20f, 0.30f, 0.18f), chromeMat);
        MakePart("FrontBumperLower", ext.transform,
            new Vector3(0f, 0.15f, 5.00f), new Vector3(2.80f, 0.12f, 0.14f), trimMat);
        MakePart("RearBumper", ext.transform,
            new Vector3(0f, 0.40f, -4.68f), new Vector3(3.00f, 0.30f, 0.20f), chromeMat);

        // Running board below serving window
        MakePart("RunningBoard", ext.transform,
            new Vector3(1.82f, 0.28f, -0.80f), new Vector3(0.30f, 0.06f, 2.80f), stepMat);
        MakePart("RunBoardBracketF", ext.transform,
            new Vector3(1.82f, 0.20f, 0.30f), new Vector3(0.20f, 0.12f, 0.08f), trimMat);
        MakePart("RunBoardBracketR", ext.transform,
            new Vector3(1.82f, 0.20f, -1.90f), new Vector3(0.20f, 0.12f, 0.08f), trimMat);

        // Exhaust pipe
        MakePart("Exhaust", ext.transform,
            new Vector3(-1.0f, 0.18f, -4.80f), new Vector3(0.12f, 0.10f, 0.40f), trimMat);

        // Awning over serving window
        MakePart("Awning", ext.transform,
            new Vector3(2.40f, 2.90f, -0.80f), new Vector3(1.50f, 0.10f, 2.80f), awningMat);
        MakePart("AwningEdge", ext.transform,
            new Vector3(2.40f, 2.86f, -0.80f), new Vector3(1.50f, 0.03f, 2.90f), trimMat);
        MakePart("AwningStripe1", ext.transform,
            new Vector3(2.40f, 2.91f, -0.15f), new Vector3(1.50f, 0.04f, 0.12f), bodyMat);
        MakePart("AwningStripe2", ext.transform,
            new Vector3(2.40f, 2.91f, -1.45f), new Vector3(1.50f, 0.04f, 0.12f), bodyMat);
        MakePart("AwningSupportL", ext.transform,
            new Vector3(2.00f, 2.50f, 0.50f), new Vector3(0.05f, 0.80f, 0.05f), metalMat);
        MakePart("AwningSupportR", ext.transform,
            new Vector3(2.00f, 2.50f, -2.10f), new Vector3(0.05f, 0.80f, 0.05f), metalMat);

        // ── Remove colliders from all exterior visuals ───────────────
        foreach (var c in ext.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(c);

        // ═══════════════════════════════════════════════════════════════
        //  TRIGGERS & INTERACTION POINTS
        // ═══════════════════════════════════════════════════════════════

        // Serving hatch trigger (right side, doesn't rotate)
        var hatchTriggerGO = new GameObject("HatchTrigger");
        hatchTriggerGO.transform.SetParent(root.transform);
        hatchTriggerGO.transform.localPosition = new Vector3(2.50f, 1.80f, -0.80f);
        var hatchTriggerCol = hatchTriggerGO.AddComponent<BoxCollider>();
        hatchTriggerCol.size   = new Vector3(1.20f, 2.50f, 2.80f);
        hatchTriggerCol.isTrigger = true;
        var hatchInteract = hatchTriggerGO.AddComponent<HatchInteract>();
        hatchInteract.hatch = ext.GetComponentInChildren<ServingHatch>();

        // Exterior entry trigger — rear face
        var doorTriggerGO = new GameObject("ExteriorDoorTrigger");
        doorTriggerGO.transform.SetParent(root.transform);
        doorTriggerGO.transform.localPosition = new Vector3(0f, 1.50f, -5.30f);
        var doorCol = doorTriggerGO.AddComponent<BoxCollider>();
        doorCol.size        = new Vector3(4.00f, 3.00f, 1.50f);
        doorCol.isTrigger   = true;
        var doorInteract = doorTriggerGO.AddComponent<TruckExteriorDoor>();
        doorInteract.truck  = truck;

        // Exit spawn (behind the truck)
        var extDoorPoint = new GameObject("ExteriorDoorPoint");
        extDoorPoint.transform.SetParent(root.transform);
        extDoorPoint.transform.localPosition = new Vector3(0f, 0f, -5.80f);
        extDoorPoint.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        truck.exteriorDoorPoint = extDoorPoint.transform;

        // Camera target for driving
        var camTarget = new GameObject("CameraTarget");
        camTarget.transform.SetParent(root.transform);
        camTarget.transform.localPosition = new Vector3(0f, 2.50f, 0f);
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

        var hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hub.name = "Hub";
        hub.transform.SetParent(go.transform);
        hub.transform.localPosition = new Vector3(0f, 0.70f, 0f);
        hub.transform.localScale    = new Vector3(0.40f, 0.15f, 0.40f);
        hub.GetComponent<Renderer>().sharedMaterial =
            MakeMat("WheelHub", new Color(0.75f, 0.75f, 0.75f), 0.80f);
        Object.DestroyImmediate(hub.GetComponent<Collider>());

        return go.transform;
    }

    // ── Interior ──────────────────────────────────────────────────────────
    //
    // Two zones inside the truck, both parented to the truck root:
    //   CARGO  z = −3.8 … +1.5  (empty, modular — equip added later)
    //   CAB    z = +1.5 … +4.3  (driver + passenger seats, dashboard)
    //
    // A divider wall with a walk-through doorway separates them.
    // The cargo rear (z = −3.8) is OPEN — the player enters/exits there.
    // The right wall has a gap for the serving window.

    static GameObject SetupInteriorRoom(FoodTruck truck)
    {
        // ── Dimensions ──────────────────────────────────────────────────
        const float IW = 2.6f;       // interior width (X)
        const float IH = 2.5f;       // cargo ceiling height
        const float CH = 2.4f;       // cab ceiling height
        const float T  = 0.10f;      // wall thickness

        const float CZ_MIN  = -3.8f; // cargo rear
        const float CZ_MAX  = 1.5f;  // cargo front / divider
        const float CAB_MAX = 4.3f;  // cab front (behind dashboard)

        float cargoLen   = CZ_MAX - CZ_MIN;
        float cargoMidZ  = (CZ_MIN + CZ_MAX) / 2f;
        float cabLen     = CAB_MAX - CZ_MAX;
        float cabMidZ    = (CZ_MAX + CAB_MAX) / 2f;

        // Serving window opening in right wall
        const float WIN_Z_MIN = -2.0f;
        const float WIN_Z_MAX =  0.4f;
        const float WIN_Y_MIN =  1.0f;
        const float WIN_Y_MAX =  2.0f;
        float winCZ = (WIN_Z_MIN + WIN_Z_MAX) / 2f;
        float winLZ = WIN_Z_MAX - WIN_Z_MIN;

        // ── Materials ───────────────────────────────────────────────────
        Material wallMat    = MakeMat("InteriorWall",   new Color(0.93f, 0.90f, 0.85f), 0.10f);
        Material floorMat   = MakeMat("InteriorFloor",  new Color(0.55f, 0.50f, 0.42f), 0.30f);
        Material ceilMat    = MakeMat("InteriorCeil",   new Color(0.90f, 0.88f, 0.84f), 0.10f);
        Material signMat    = MakeMat("ExitSign",       new Color(0.10f, 0.70f, 0.20f), 0.20f);
        Material dividerMat = MakeMat("Divider",        new Color(0.85f, 0.82f, 0.78f), 0.15f);
        Material dashMat    = MakeMat("Dashboard",      new Color(0.22f, 0.22f, 0.24f), 0.40f);
        Material seatMat    = MakeMat("SeatFrame",      new Color(0.25f, 0.25f, 0.27f), 0.35f);
        Material cushionMat = MakeMat("SeatCushion",    new Color(0.35f, 0.32f, 0.30f), 0.15f);
        Material consoleMat = MakeMat("Console",        new Color(0.30f, 0.30f, 0.32f), 0.30f);
        Material wheelMat2  = MakeMat("SteeringInt",    new Color(0.20f, 0.20f, 0.22f), 0.60f);

        // ── Room parent ─────────────────────────────────────────────────
        var room = new GameObject("TruckInteriorRoom");
        room.transform.SetParent(truck.transform);
        room.transform.localPosition = Vector3.zero;
        room.transform.localRotation = Quaternion.identity;

        // ═══════════════════════════════════════════════════════════════
        //  CARGO AREA — empty, ready for modular equipment
        // ═══════════════════════════════════════════════════════════════

        // Floor
        MakeRoomPart("CargoFloor", room.transform,
            new Vector3(0f, T / 2, cargoMidZ),
            new Vector3(IW, T, cargoLen), floorMat);
        // Ceiling
        MakeRoomPart("CargoCeiling", room.transform,
            new Vector3(0f, IH - T / 2, cargoMidZ),
            new Vector3(IW + T * 2, T, cargoLen), ceilMat);

        // Left wall (solid, full cargo length)
        MakeRoomPart("CargoWallLeft", room.transform,
            new Vector3(-IW / 2 - T / 2, IH / 2, cargoMidZ),
            new Vector3(T, IH, cargoLen), wallMat);

        // Right wall — split around serving window
        float rX = IW / 2 + T / 2;
        float rearMidZ  = (CZ_MIN + WIN_Z_MIN) / 2f;
        float rearSpan  = WIN_Z_MIN - CZ_MIN;
        float frontMidZ = (WIN_Z_MAX + CZ_MAX) / 2f;
        float frontSpan = CZ_MAX - WIN_Z_MAX;
        MakeRoomPart("RightWallRear", room.transform,
            new Vector3(rX, IH / 2, rearMidZ), new Vector3(T, IH, rearSpan), wallMat);
        MakeRoomPart("RightWallFront", room.transform,
            new Vector3(rX, IH / 2, frontMidZ), new Vector3(T, IH, frontSpan), wallMat);
        MakeRoomPart("RightWallBelow", room.transform,
            new Vector3(rX, WIN_Y_MIN / 2, winCZ), new Vector3(T, WIN_Y_MIN, winLZ), wallMat);
        float aboveMid = (WIN_Y_MAX + IH) / 2;
        float aboveSpan = IH - WIN_Y_MAX;
        MakeRoomPart("RightWallAbove", room.transform,
            new Vector3(rX, aboveMid, winCZ), new Vector3(T, aboveSpan, winLZ), wallMat);

        // Rear — OPEN (player entry/exit)

        // ── Divider wall (cargo → cab) with doorway ─────────────────────
        float doorW = 0.8f;
        float doorH = 2.0f;
        float halfIW = IW / 2;
        float sideW = halfIW - doorW / 2;
        // Left section
        MakeRoomPart("DividerL", room.transform,
            new Vector3(-(halfIW - sideW / 2 + doorW / 4), IH / 2, CZ_MAX),
            new Vector3(sideW, IH, T), dividerMat);
        // Right section
        MakeRoomPart("DividerR", room.transform,
            new Vector3((halfIW - sideW / 2 + doorW / 4), IH / 2, CZ_MAX),
            new Vector3(sideW, IH, T), dividerMat);
        // Above doorway
        MakeRoomPart("DividerTop", room.transform,
            new Vector3(0f, (doorH + IH) / 2, CZ_MAX),
            new Vector3(doorW, IH - doorH, T), dividerMat);
        // Door frame trim
        MakeRoomPart("DoorFrameL", room.transform,
            new Vector3(-doorW / 2 - 0.02f, doorH / 2, CZ_MAX + 0.01f),
            new Vector3(0.04f, doorH, 0.04f), consoleMat);
        MakeRoomPart("DoorFrameR", room.transform,
            new Vector3(doorW / 2 + 0.02f, doorH / 2, CZ_MAX + 0.01f),
            new Vector3(0.04f, doorH, 0.04f), consoleMat);

        // Cargo interior light
        var cargoLight = new GameObject("CargoLight");
        cargoLight.transform.SetParent(room.transform);
        cargoLight.transform.localPosition = new Vector3(0f, IH - 0.2f, cargoMidZ);
        var cl = cargoLight.AddComponent<Light>();
        cl.type = LightType.Point; cl.range = 8f;
        cl.intensity = 2.5f;
        cl.color = new Color(1f, 0.97f, 0.88f); cl.shadows = LightShadows.None;

        // Exit floor marker (green strip at rear)
        MakeRoomPart("ExitFloorMarker", room.transform,
            new Vector3(0f, T, CZ_MIN + 0.3f),
            new Vector3(IW - 0.2f, 0.02f, 0.5f), signMat);

        // Exit trigger
        var exitGO = new GameObject("ExitDoorTrigger");
        exitGO.transform.SetParent(room.transform);
        exitGO.transform.localPosition = new Vector3(0f, 1.0f, CZ_MIN + 0.4f);
        var exitCol = exitGO.AddComponent<BoxCollider>();
        exitCol.size   = new Vector3(IW, 2.2f, 1.0f);
        exitCol.isTrigger = true;
        exitGO.AddComponent<TruckInteriorExit>().truck = truck;

        // ═══════════════════════════════════════════════════════════════
        //  CAB — driver & passenger seats
        // ═══════════════════════════════════════════════════════════════

        // Cab floor
        MakeRoomPart("CabFloor", room.transform,
            new Vector3(0f, T / 2, cabMidZ), new Vector3(IW, T, cabLen), floorMat);
        // Cab ceiling
        MakeRoomPart("CabCeiling", room.transform,
            new Vector3(0f, CH - T / 2, cabMidZ), new Vector3(IW, T, cabLen), ceilMat);

        // Dashboard
        MakeRoomPart("Dashboard", room.transform,
            new Vector3(0f, 0.85f, CAB_MAX - 0.15f),
            new Vector3(IW - 0.2f, 0.70f, 0.30f), dashMat);
        MakeRoomPart("DashTop", room.transform,
            new Vector3(0f, 1.22f, CAB_MAX - 0.20f),
            new Vector3(IW - 0.1f, 0.04f, 0.40f), dashMat);
        // Instrument cluster (driver side)
        MakeRoomPart("Instruments", room.transform,
            new Vector3(-0.50f, 1.05f, CAB_MAX - 0.02f),
            new Vector3(0.50f, 0.30f, 0.04f), consoleMat);

        // Steering wheel + column (driver side = −X)
        MakeRoomPart("SteeringCol", room.transform,
            new Vector3(-0.50f, 1.00f, CAB_MAX - 0.50f),
            new Vector3(0.06f, 0.40f, 0.06f), wheelMat2);
        MakeRoomPart("SteeringWheel", room.transform,
            new Vector3(-0.50f, 1.25f, CAB_MAX - 0.65f),
            new Vector3(0.40f, 0.40f, 0.04f), wheelMat2);
        MakeRoomPart("SteeringHub", room.transform,
            new Vector3(-0.50f, 1.25f, CAB_MAX - 0.67f),
            new Vector3(0.12f, 0.12f, 0.03f), consoleMat);

        // ── Seat positions ──────────────────────────────────────────────
        float driverX    = -0.55f;
        float passengerX =  0.55f;
        float seatZ      = cabMidZ - 0.3f;

        // Driver seat
        MakeRoomPart("DriverBase", room.transform,
            new Vector3(driverX, 0.20f, seatZ), new Vector3(0.55f, 0.30f, 0.06f), seatMat);
        MakeRoomPart("DriverCushion", room.transform,
            new Vector3(driverX, 0.45f, seatZ), new Vector3(0.55f, 0.10f, 0.55f), cushionMat);
        MakeRoomPart("DriverBack", room.transform,
            new Vector3(driverX, 0.92f, seatZ + 0.28f), new Vector3(0.55f, 0.85f, 0.10f), cushionMat);
        MakeRoomPart("DriverHeadrest", room.transform,
            new Vector3(driverX, 1.45f, seatZ + 0.28f), new Vector3(0.25f, 0.22f, 0.08f), cushionMat);

        // Passenger seat
        MakeRoomPart("PassBase", room.transform,
            new Vector3(passengerX, 0.20f, seatZ), new Vector3(0.55f, 0.30f, 0.06f), seatMat);
        MakeRoomPart("PassCushion", room.transform,
            new Vector3(passengerX, 0.45f, seatZ), new Vector3(0.55f, 0.10f, 0.55f), cushionMat);
        MakeRoomPart("PassBack", room.transform,
            new Vector3(passengerX, 0.92f, seatZ + 0.28f), new Vector3(0.55f, 0.85f, 0.10f), cushionMat);
        MakeRoomPart("PassHeadrest", room.transform,
            new Vector3(passengerX, 1.45f, seatZ + 0.28f), new Vector3(0.25f, 0.22f, 0.08f), cushionMat);

        // Center console + gear shift
        MakeRoomPart("CenterConsole", room.transform,
            new Vector3(0f, 0.40f, seatZ), new Vector3(0.30f, 0.50f, 0.55f), consoleMat);
        MakeRoomPart("GearShift", room.transform,
            new Vector3(0f, 0.70f, seatZ - 0.05f), new Vector3(0.06f, 0.12f, 0.06f), wheelMat2);

        // Cab light
        var cabLightGO = new GameObject("CabLight");
        cabLightGO.transform.SetParent(room.transform);
        cabLightGO.transform.localPosition = new Vector3(0f, CH - 0.15f, cabMidZ);
        var cabL = cabLightGO.AddComponent<Light>();
        cabL.type = LightType.Point; cabL.range = 4f;
        cabL.intensity = 1.5f;
        cabL.color = new Color(1f, 0.97f, 0.88f); cabL.shadows = LightShadows.None;

        // Driver seat trigger
        var driverGO = new GameObject("DriverSeatTrigger");
        driverGO.transform.SetParent(room.transform);
        driverGO.transform.localPosition = new Vector3(driverX, 0.80f, seatZ);
        var driverCol = driverGO.AddComponent<BoxCollider>();
        driverCol.size   = new Vector3(0.80f, 1.50f, 0.80f);
        driverCol.isTrigger = true;
        driverGO.AddComponent<TruckDriverSeat>().truck = truck;

        // ── Spawn points ────────────────────────────────────────────────
        var interiorSpawn = new GameObject("InteriorSpawnPoint");
        interiorSpawn.transform.SetParent(room.transform);
        interiorSpawn.transform.localPosition = new Vector3(0f, 0.2f, CZ_MIN + 1.0f);
        interiorSpawn.transform.localRotation = Quaternion.identity; // facing +Z (toward cab)

        var driverSpawn = new GameObject("DriverSpawnPoint");
        driverSpawn.transform.SetParent(room.transform);
        driverSpawn.transform.localPosition = new Vector3(driverX, 0.2f, seatZ - 0.5f);

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
