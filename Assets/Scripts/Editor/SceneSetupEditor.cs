#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Menu: FoodTruck > Setup City Scene
/// Builds: Lighting, City, Player, Food Truck (visual only), Camera, Game Systems.
/// No runtime script dependencies beyond what remains in Assets/Scripts.
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
        GameObject player = SetupPlayer();
        SetupFoodTruck();
        SetupCamera(player);
        AddInteractionSystem(player);
        SetupGameSystems();

        EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene(),
            "Assets/Scenes/CityScene.unity");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FoodTruck] Scene built → Assets/Scenes/CityScene.unity");
    }

    // ── Game Systems ──────────────────────────────────────────────────────

    static void SetupGameSystems()
    {
        var gameMgr = new GameObject("GameManager");
        gameMgr.AddComponent<GameManager>();

        var gameUI = new GameObject("GameUI");
        gameUI.AddComponent<GameUI>();
    }

    // ── Lighting & Weather ────────────────────────────────────────────────

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

        var dayNight = sun.gameObject.AddComponent<DayNightCycle>();
        dayNight.sun = sun;
        dayNight.cycleDuration = 90f;

        var weather = sun.gameObject.AddComponent<WeatherSystem>();
        weather.sun = sun;
        weather.changeInterval = 25f;

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
        BakeNavMesh(cityGO);
    }

    static void BakeNavMesh(GameObject cityGO)
    {
        foreach (var r in cityGO.GetComponentsInChildren<Renderer>())
            GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.NavigationStatic);

        #pragma warning disable CS0618
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        #pragma warning restore CS0618
        Debug.Log("[SceneSetup] NavMesh baked");
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

        return player;
    }

    static void AddInteractionSystem(GameObject player)
    {
        var ia = player.AddComponent<InteractionSystem>();
        ia.interactRadius = 2.5f;
    }

    // ── Food Truck (visual + physics, no runtime scripts) ─────────────────
    //
    // Exterior built from primitives with panel lines, trim, chrome details,
    // headlights, grille, mirrors, fenders, and awning.
    // Local +Z = cab (forward), −Z = cargo rear.
    // Approx 10 m long, 3.4 m wide, 3.0 m tall.

    static GameObject SetupFoodTruck()
    {
        // ── Materials ─────────────────────────────────────────────────────
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

        // ── Root + physics ─────────────────────────────────────────────────
        var root = new GameObject("FoodTruck");
        root.transform.position = new Vector3(3f, 0f, 42f);
        root.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        var bodyCol = root.AddComponent<BoxCollider>();
        bodyCol.center = new Vector3(0f, 1.5f, 0f);
        bodyCol.size   = new Vector3(3.6f, 3.0f, 10.0f);

        var rb = root.AddComponent<Rigidbody>();
        rb.mass           = 1500f;
        rb.linearDamping  = 2f;
        rb.angularDamping = 8f;
        rb.constraints    = RigidbodyConstraints.FreezeRotationX
                          | RigidbodyConstraints.FreezeRotationZ
                          | RigidbodyConstraints.FreezePositionY;
        rb.isKinematic    = true;

        var ext = new GameObject("Exterior");
        ext.transform.SetParent(root.transform);
        ext.transform.localPosition = Vector3.zero;

        // ═══════════════════════════════════════════════════════════════
        //  CARGO BODY — z from −4.5 to +1.8
        // ═══════════════════════════════════════════════════════════════

        MakePart("CargoLeftLower",       ext.transform, new Vector3(-1.68f, 1.0f,   -1.35f), new Vector3(0.06f, 1.40f, 6.30f), bodyMat);
        MakePart("CargoLeftUpper",       ext.transform, new Vector3(-1.68f, 2.35f,  -1.35f), new Vector3(0.06f, 1.30f, 6.30f), bodyMat);
        MakePart("CargoLeftTrim",        ext.transform, new Vector3(-1.69f, 1.72f,  -1.35f), new Vector3(0.04f, 0.06f, 6.30f), trimMat);
        MakePart("CargoLeftAccent",      ext.transform, new Vector3(-1.69f, 0.32f,  -1.35f), new Vector3(0.04f, 0.04f, 6.30f), bodyAccent);

        MakePart("CargoRightLower",      ext.transform, new Vector3(1.68f,  1.0f,   -1.35f), new Vector3(0.06f, 1.40f, 6.30f), bodyMat);
        MakePart("CargoRightUpperRear",  ext.transform, new Vector3(1.68f,  2.35f,  -3.0f),  new Vector3(0.06f, 1.30f, 3.00f), bodyMat);
        MakePart("CargoRightUpperFront", ext.transform, new Vector3(1.68f,  2.35f,   0.8f),  new Vector3(0.06f, 1.30f, 2.00f), bodyMat);
        MakePart("CargoRightTrim",       ext.transform, new Vector3(1.69f,  1.72f,  -1.35f), new Vector3(0.04f, 0.06f, 6.30f), trimMat);
        MakePart("CargoRightAccent",     ext.transform, new Vector3(1.69f,  0.32f,  -1.35f), new Vector3(0.04f, 0.04f, 6.30f), bodyAccent);

        // Serving window area
        MakePart("WindowHeader",    ext.transform, new Vector3(1.68f, 2.72f, -0.8f), new Vector3(0.06f, 0.56f, 2.60f), bodyMat);
        MakePart("WindowFrameTop",  ext.transform, new Vector3(1.70f, 2.48f, -0.8f), new Vector3(0.04f, 0.06f, 2.50f), chromeMat);
        MakePart("WindowFrameBot",  ext.transform, new Vector3(1.70f, 1.72f, -0.8f), new Vector3(0.04f, 0.06f, 2.50f), chromeMat);
        MakePart("WindowFrameL",    ext.transform, new Vector3(1.70f, 2.10f, -2.03f), new Vector3(0.04f, 0.82f, 0.06f), chromeMat);
        MakePart("WindowFrameR",    ext.transform, new Vector3(1.70f, 2.10f,  0.43f), new Vector3(0.04f, 0.82f, 0.06f), chromeMat);
        MakePart("WindowLedge",     ext.transform, new Vector3(1.85f, 1.72f, -0.8f), new Vector3(0.30f, 0.06f, 2.40f), metalMat);

        // Serving hatch — static visual panel (no runtime component)
        MakePart("HatchPanel", ext.transform,
            new Vector3(1.72f, 2.09f, -0.8f), new Vector3(0.04f, 0.74f, 2.40f), windowMat);

        // Rear face
        MakePart("CargoRear",       ext.transform, new Vector3( 0f,    1.65f, -4.52f), new Vector3(3.30f, 2.70f, 0.06f), bodyMat);
        MakePart("RearDoorFrameL",  ext.transform, new Vector3(-1.30f, 1.65f, -4.56f), new Vector3(0.08f, 2.60f, 0.04f), chromeMat);
        MakePart("RearDoorFrameR",  ext.transform, new Vector3( 1.30f, 1.65f, -4.56f), new Vector3(0.08f, 2.60f, 0.04f), chromeMat);
        MakePart("RearDoorFrameTop",ext.transform, new Vector3( 0f,    2.96f, -4.56f), new Vector3(2.60f, 0.06f, 0.04f), chromeMat);
        MakePart("RearDoorHandle",  ext.transform, new Vector3( 0.05f, 1.50f, -4.58f), new Vector3(0.20f, 0.06f, 0.04f), chromeMat);

        // Cargo roof
        MakePart("CargoRoof",     ext.transform, new Vector3(0f, 3.00f, -1.35f), new Vector3(3.42f, 0.08f, 6.40f), metalMat);
        MakePart("RoofTrimFront", ext.transform, new Vector3(0f, 3.04f,  1.50f), new Vector3(3.42f, 0.04f, 0.08f), trimMat);
        MakePart("RoofTrimRear",  ext.transform, new Vector3(0f, 3.04f, -4.53f), new Vector3(3.42f, 0.04f, 0.06f), trimMat);
        MakePart("ACUnit",        ext.transform, new Vector3(0f, 3.15f, -2.50f), new Vector3(1.00f, 0.30f, 0.80f), metalMat);
        MakePart("ACVent",        ext.transform, new Vector3(0f, 3.32f, -2.50f), new Vector3(0.60f, 0.06f, 0.50f), trimMat);

        // Cargo floor / chassis
        MakePart("CargoFloorExt", ext.transform, new Vector3(0f, 0.30f, -1.35f), new Vector3(3.30f, 0.06f, 6.30f), trimMat);
        MakePart("Chassis",       ext.transform, new Vector3(0f, 0.18f,  0f),    new Vector3(2.80f, 0.20f, 9.20f), trimMat);

        // ═══════════════════════════════════════════════════════════════
        //  CAB — z from +1.8 to +4.95
        // ═══════════════════════════════════════════════════════════════

        MakePart("CabBodyL", ext.transform, new Vector3(-1.62f, 1.50f, 3.35f), new Vector3(0.06f, 2.40f, 3.00f), cabMat);
        MakePart("CabBodyR", ext.transform, new Vector3( 1.62f, 1.50f, 3.35f), new Vector3(0.06f, 2.40f, 3.00f), cabMat);
        MakePart("CabRoof",  ext.transform, new Vector3( 0f,    2.72f, 3.35f), new Vector3(3.30f, 0.06f, 3.00f), cabMat);

        MakePart("Hood",      ext.transform, new Vector3(0f, 1.00f, 4.60f), new Vector3(3.10f, 0.06f, 0.80f), cabMat);
        MakePart("HoodFront", ext.transform, new Vector3(0f, 0.80f, 4.96f), new Vector3(3.10f, 0.50f, 0.06f), cabMat);

        MakePart("Grille",     ext.transform, new Vector3(0f, 0.55f, 4.97f), new Vector3(2.40f, 0.50f, 0.04f), chromeMat);
        MakePart("GrilleSlat1",ext.transform, new Vector3(0f, 0.65f, 4.98f), new Vector3(2.20f, 0.04f, 0.02f), trimMat);
        MakePart("GrilleSlat2",ext.transform, new Vector3(0f, 0.50f, 4.98f), new Vector3(2.20f, 0.04f, 0.02f), trimMat);
        MakePart("GrilleSlat3",ext.transform, new Vector3(0f, 0.35f, 4.98f), new Vector3(2.20f, 0.04f, 0.02f), trimMat);

        MakePart("Windshield",   ext.transform, new Vector3( 0f,    2.00f, 4.85f), new Vector3(2.50f, 1.20f, 0.04f), windowMat);
        MakePart("APillarL",     ext.transform, new Vector3(-1.32f, 2.00f, 4.85f), new Vector3(0.10f, 1.30f, 0.08f), trimMat);
        MakePart("APillarR",     ext.transform, new Vector3( 1.32f, 2.00f, 4.85f), new Vector3(0.10f, 1.30f, 0.08f), trimMat);
        MakePart("RearCabWindow",ext.transform, new Vector3( 0f,    2.00f, 1.86f), new Vector3(2.00f, 0.90f, 0.04f), windowMat);

        MakePart("CabWindowL",  ext.transform, new Vector3(-1.64f, 2.00f, 3.80f), new Vector3(0.04f, 0.90f, 1.60f), windowMat);
        MakePart("CabWindowR",  ext.transform, new Vector3( 1.64f, 2.00f, 3.80f), new Vector3(0.04f, 0.90f, 1.60f), windowMat);
        MakePart("DoorL",       ext.transform, new Vector3(-1.66f, 1.20f, 3.50f), new Vector3(0.02f, 1.40f, 2.20f), cabMat);
        MakePart("DoorR",       ext.transform, new Vector3( 1.66f, 1.20f, 3.50f), new Vector3(0.02f, 1.40f, 2.20f), cabMat);
        MakePart("DoorHandleL", ext.transform, new Vector3(-1.68f, 1.30f, 3.20f), new Vector3(0.04f, 0.06f, 0.20f), chromeMat);
        MakePart("DoorHandleR", ext.transform, new Vector3( 1.68f, 1.30f, 3.20f), new Vector3(0.04f, 0.06f, 0.20f), chromeMat);

        MakePart("MirrorArmL", ext.transform, new Vector3(-1.85f, 1.90f, 4.50f), new Vector3(0.30f, 0.04f, 0.04f), trimMat);
        MakePart("MirrorL",    ext.transform, new Vector3(-2.05f, 1.85f, 4.50f), new Vector3(0.04f, 0.20f, 0.16f), chromeMat);
        MakePart("MirrorArmR", ext.transform, new Vector3( 1.85f, 1.90f, 4.50f), new Vector3(0.30f, 0.04f, 0.04f), trimMat);
        MakePart("MirrorR",    ext.transform, new Vector3( 2.05f, 1.85f, 4.50f), new Vector3(0.04f, 0.20f, 0.16f), chromeMat);

        MakePart("SunVisor", ext.transform, new Vector3(0f, 2.70f, 4.92f), new Vector3(2.60f, 0.12f, 0.25f), trimMat);

        // ═══════════════════════════════════════════════════════════════
        //  WHEELS
        // ═══════════════════════════════════════════════════════════════

        MakeWheel("WheelFL", ext.transform, new Vector3(-1.55f, 0.38f,  3.50f), wheelMat);
        MakeWheel("WheelFR", ext.transform, new Vector3( 1.55f, 0.38f,  3.50f), wheelMat);
        MakeWheel("WheelBL", ext.transform, new Vector3(-1.55f, 0.38f, -3.00f), wheelMat);
        MakeWheel("WheelBR", ext.transform, new Vector3( 1.55f, 0.38f, -3.00f), wheelMat);

        MakePart("FenderFL", ext.transform, new Vector3(-1.68f, 0.65f,  3.50f), new Vector3(0.06f, 0.10f, 0.90f), trimMat);
        MakePart("FenderFR", ext.transform, new Vector3( 1.68f, 0.65f,  3.50f), new Vector3(0.06f, 0.10f, 0.90f), trimMat);
        MakePart("FenderBL", ext.transform, new Vector3(-1.68f, 0.65f, -3.00f), new Vector3(0.06f, 0.10f, 0.90f), trimMat);
        MakePart("FenderBR", ext.transform, new Vector3( 1.68f, 0.65f, -3.00f), new Vector3(0.06f, 0.10f, 0.90f), trimMat);

        // ═══════════════════════════════════════════════════════════════
        //  DETAILS
        // ═══════════════════════════════════════════════════════════════

        MakePart("HeadlightL",    ext.transform, new Vector3(-1.15f, 0.85f, 4.98f), new Vector3(0.35f, 0.25f, 0.04f), headlightMat);
        MakePart("HeadlightR",    ext.transform, new Vector3( 1.15f, 0.85f, 4.98f), new Vector3(0.35f, 0.25f, 0.04f), headlightMat);
        MakePart("TurnSignalFL",  ext.transform, new Vector3(-1.35f, 0.50f, 4.98f), new Vector3(0.18f, 0.10f, 0.04f), indicatorMat);
        MakePart("TurnSignalFR",  ext.transform, new Vector3( 1.35f, 0.50f, 4.98f), new Vector3(0.18f, 0.10f, 0.04f), indicatorMat);
        MakePart("TaillightL",    ext.transform, new Vector3(-1.35f, 1.50f, -4.58f), new Vector3(0.25f, 0.50f, 0.04f), taillightMat);
        MakePart("TaillightR",    ext.transform, new Vector3( 1.35f, 1.50f, -4.58f), new Vector3(0.25f, 0.50f, 0.04f), taillightMat);
        MakePart("RearIndicatorL",ext.transform, new Vector3(-1.35f, 1.05f, -4.58f), new Vector3(0.25f, 0.18f, 0.04f), indicatorMat);
        MakePart("RearIndicatorR",ext.transform, new Vector3( 1.35f, 1.05f, -4.58f), new Vector3(0.25f, 0.18f, 0.04f), indicatorMat);

        MakePart("FrontBumper",      ext.transform, new Vector3(0f, 0.35f, 5.00f),  new Vector3(3.20f, 0.30f, 0.18f), chromeMat);
        MakePart("FrontBumperLower", ext.transform, new Vector3(0f, 0.15f, 5.00f),  new Vector3(2.80f, 0.12f, 0.14f), trimMat);
        MakePart("RearBumper",       ext.transform, new Vector3(0f, 0.40f, -4.68f), new Vector3(3.00f, 0.30f, 0.20f), chromeMat);

        MakePart("RunningBoard",    ext.transform, new Vector3(1.82f, 0.28f, -0.80f), new Vector3(0.30f, 0.06f, 2.80f), stepMat);
        MakePart("RunBoardBracketF",ext.transform, new Vector3(1.82f, 0.20f,  0.30f), new Vector3(0.20f, 0.12f, 0.08f), trimMat);
        MakePart("RunBoardBracketR",ext.transform, new Vector3(1.82f, 0.20f, -1.90f), new Vector3(0.20f, 0.12f, 0.08f), trimMat);

        MakePart("Exhaust", ext.transform, new Vector3(-1.0f, 0.18f, -4.80f), new Vector3(0.12f, 0.10f, 0.40f), trimMat);

        MakePart("Awning",        ext.transform, new Vector3(2.40f, 2.90f, -0.80f), new Vector3(1.50f, 0.10f, 2.80f), awningMat);
        MakePart("AwningEdge",    ext.transform, new Vector3(2.40f, 2.86f, -0.80f), new Vector3(1.50f, 0.03f, 2.90f), trimMat);
        MakePart("AwningStripe1", ext.transform, new Vector3(2.40f, 2.91f, -0.15f), new Vector3(1.50f, 0.04f, 0.12f), bodyMat);
        MakePart("AwningStripe2", ext.transform, new Vector3(2.40f, 2.91f, -1.45f), new Vector3(1.50f, 0.04f, 0.12f), bodyMat);
        MakePart("AwningSupportL",ext.transform, new Vector3(2.00f, 2.50f,  0.50f), new Vector3(0.05f, 0.80f, 0.05f), metalMat);
        MakePart("AwningSupportR",ext.transform, new Vector3(2.00f, 2.50f, -2.10f), new Vector3(0.05f, 0.80f, 0.05f), metalMat);

        // Remove colliders from all exterior visuals
        foreach (var c in ext.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(c);

        return root;
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

    // ── Helpers ───────────────────────────────────────────────────────────

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

    static void BuildCharacterModel(Transform parent)
    {
        var model = new GameObject("Model");
        model.transform.SetParent(parent);
        model.transform.localPosition = Vector3.zero;

        Material skin  = MakeMat("CharSkin",  new Color(0.91f, 0.76f, 0.60f), 0.3f);
        Material cloth = MakeMat("CharCloth", new Color(0.22f, 0.35f, 0.55f), 0.15f);
        Material hat   = MakeMat("CharHat",   new Color(0.85f, 0.32f, 0.10f), 0.2f);

        MakePart("Torso",   model.transform, new Vector3( 0f,     1.10f,  0f),    new Vector3(0.55f, 0.65f, 0.30f), cloth);
        MakePart("Head",    model.transform, new Vector3( 0f,     1.72f,  0f),    new Vector3(0.38f, 0.38f, 0.38f), skin,  PrimitiveType.Sphere);
        MakePart("HatBrim", model.transform, new Vector3( 0f,     1.92f,  0f),    new Vector3(0.48f, 0.07f, 0.48f), hat);
        MakePart("HatTop",  model.transform, new Vector3( 0f,     2.10f,  0f),    new Vector3(0.30f, 0.38f, 0.30f), hat);
        MakePart("ArmL",    model.transform, new Vector3(-0.38f,  1.05f,  0f),    new Vector3(0.18f, 0.60f, 0.18f), cloth);
        MakePart("ArmR",    model.transform, new Vector3( 0.38f,  1.05f,  0f),    new Vector3(0.18f, 0.60f, 0.18f), cloth);
        MakePart("HandL",   model.transform, new Vector3(-0.38f,  0.70f,  0f),    new Vector3(0.16f, 0.16f, 0.16f), skin,  PrimitiveType.Sphere);
        MakePart("HandR",   model.transform, new Vector3( 0.38f,  0.70f,  0f),    new Vector3(0.16f, 0.16f, 0.16f), skin,  PrimitiveType.Sphere);
        MakePart("LegL",    model.transform, new Vector3(-0.16f,  0.42f,  0f),    new Vector3(0.22f, 0.72f, 0.22f), cloth);
        MakePart("LegR",    model.transform, new Vector3( 0.16f,  0.42f,  0f),    new Vector3(0.22f, 0.72f, 0.22f), cloth);
        MakePart("ShoeL",   model.transform, new Vector3(-0.16f,  0.06f,  0.06f), new Vector3(0.22f, 0.12f, 0.32f), hat);
        MakePart("ShoeR",   model.transform, new Vector3( 0.16f,  0.06f,  0.06f), new Vector3(0.22f, 0.12f, 0.32f), hat);

        foreach (var col in model.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(col);
    }

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
