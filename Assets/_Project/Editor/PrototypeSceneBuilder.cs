using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace NightShift.Editor
{
    /// <summary>
    /// Generates the whole greybox prototype scene from primitives: office, openings, camera rooms,
    /// robots, player rig, systems and post-processing. Re-run any time; it overwrites the scene.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/NightShift.unity";
        const string MatDir = Root + "/Materials";
        const string DataDir = Root + "/Data";

        [MenuItem("Tools/Night Shift/Build Prototype Scene")]
        public static void BuildMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
        }

        public static string Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MeshGen.ClearCache();
            checkerTex = grimeTex = furTex = plasticTex = metalTex = woodTex = fabricTex = tileTex = null;

            // ---- materials -----------------------------------------------------------------
            var wall = Mat("Wall", new Color(0.42f, 0.43f, 0.46f), 0.1f, tex: Grime(), tiling: 2f);
            var wallTrim = Mat("WallTrim", new Color(0.16f, 0.3f, 0.26f), 0.2f, tex: Checker(), tiling: 2f);
            var floor = Mat("FloorTile", new Color(0.8f, 0.78f, 0.75f), 0.35f, tex: Checker(), tiling: 3f);
            var ceiling = Mat("Ceiling", new Color(0.3f, 0.3f, 0.3f), 0.05f, tex: CeilingTile(), tiling: 3f);
            var wood = Mat("Wood", new Color(0.45f, 0.28f, 0.16f), 0.3f, tex: WoodGrain());
            var metal = Mat("Metal", new Color(0.3f, 0.31f, 0.34f), 0.55f, metallic: 0.8f, tex: MetalPanel());
            var darkMetal = Mat("DarkMetal", new Color(0.12f, 0.12f, 0.13f), 0.5f, metallic: 0.9f, tex: MetalPanel());
            var curtain = Mat("Curtain", new Color(0.55f, 0.06f, 0.1f), 0.1f, tex: Fabric(), tiling: 3f);
            var table = Mat("TableCloth", new Color(0.75f, 0.72f, 0.66f), 0.1f, tex: Fabric());
            var poster = Mat("Poster", new Color(0.8f, 0.58f, 0.18f), 0.1f, tex: Grime());
            var screen = Mat("MonitorScreen", new Color(0.05f, 0.12f, 0.1f), 0.8f, emission: new Color(0.05f, 0.35f, 0.25f));
            var crate = Mat("Crate", new Color(0.5f, 0.38f, 0.24f), 0.15f, tex: WoodGrain());
            var supply = Mat("SupplyBox", new Color(0.9f, 0.45f, 0.05f), 0.3f, emission: new Color(0.25f, 0.1f, 0f));
            var ribbonBox = Mat("RibbonBox", new Color(0.9f, 0.28f, 0.48f), 0.45f, tex: Plastic());

            // ---- robot data ----------------------------------------------------------------
            // Parry tuning: long, readable telegraphs and generous windows.
            var tuskData = Data("Tusk", "Tusk", hp: 3, interval: 7f, aggro: 6, perHour: 2, lurk: new Vector2(4f, 7f),
                telegraph: 1.2f, lunge: 0.7f, window: 0.45f, stun: 2.5f, body: new Color(0.42f, 0.38f, 0.36f), eyeCol: new Color(1f, 0.95f, 0.7f));
            var ribbonData = Data("Ribbon", "Ribbon", hp: 1, interval: 5f, aggro: 5, perHour: 3, lurk: new Vector2(2.5f, 4.5f),
                telegraph: 0.9f, lunge: 0.55f, window: 0.38f, stun: 2.2f, body: new Color(0.42f, 0.12f, 0.5f), eyeCol: new Color(0.4f, 1f, 1f));
            var wickData = Data("Wick", "Wick", hp: 2, interval: 9f, aggro: 4, perHour: 2, lurk: new Vector2(3f, 6f),
                telegraph: 1.0f, lunge: 0.65f, window: 0.42f, stun: 2.3f, body: new Color(0.3f, 0.22f, 0.16f), eyeCol: new Color(1f, 0.6f, 0.2f));

            // ---- lighting / environment ----------------------------------------------------
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.035f, 0.035f, 0.05f);
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.11f;
            RenderSettings.fogColor = new Color(0.005f, 0.005f, 0.01f);

            // ---- office --------------------------------------------------------------------
            var world = new GameObject("World").transform;
            var office = new GameObject("Office").transform;
            office.SetParent(world);

            Block(office, "Floor", new Vector3(-3, -0.1f, -3), new Vector3(3, 0, 3), floor);
            Block(office, "Ceiling", new Vector3(-3, 3, -3), new Vector3(3, 3.1f, 3), ceiling);
            Block(office, "BackWall", new Vector3(-3, 0, -3.2f), new Vector3(3, 3, -3), wall);
            // Front wall with a vent hole at the bottom
            Block(office, "FrontWall_L", new Vector3(-3, 0, 3), new Vector3(-0.65f, 3, 3.2f), wall);
            Block(office, "FrontWall_R", new Vector3(0.65f, 0, 3), new Vector3(3, 3, 3.2f), wall);
            Block(office, "FrontWall_Top", new Vector3(-0.65f, 1.35f, 3), new Vector3(0.65f, 3, 3.2f), wall);
            Block(office, "VentFrame_Top", new Vector3(-0.7f, 1.3f, 2.95f), new Vector3(0.7f, 1.4f, 3.0f), darkMetal);
            // Left/right walls with door gaps
            foreach (int side in new[] { -1, 1 })
            {
                float x0 = side < 0 ? -3.2f : 3f, x1 = side < 0 ? -3f : 3.2f;
                string n = side < 0 ? "Left" : "Right";
                Block(office, n + "Wall_A", new Vector3(x0, 0, -3), new Vector3(x1, 3, -0.75f), wall);
                Block(office, n + "Wall_B", new Vector3(x0, 0, 0.75f), new Vector3(x1, 3, 3), wall);
                Block(office, n + "Wall_Top", new Vector3(x0, 2.4f, -0.75f), new Vector3(x1, 3, 0.75f), wall);
                float fx = side * 2.97f;
                Block(office, n + "DoorFrame_L", new Vector3(fx - 0.05f, 0, -0.8f), new Vector3(fx + 0.05f, 2.45f, -0.7f), wallTrim);
                Block(office, n + "DoorFrame_R", new Vector3(fx - 0.05f, 0, 0.7f), new Vector3(fx + 0.05f, 2.45f, 0.8f), wallTrim);
                Block(office, n + "DoorFrame_T", new Vector3(fx - 0.05f, 2.4f, -0.8f), new Vector3(fx + 0.05f, 2.5f, 0.8f), wallTrim);
            }
            // Wainscot trim
            Block(office, "Trim_Back", new Vector3(-3, 0, -3), new Vector3(3, 0.9f, -2.97f), wallTrim);

            // Desk (kept low so the vent stays visible over it)
            var desk = new GameObject("Desk").transform;
            desk.SetParent(office);
            Block(desk, "Top", new Vector3(-1.2f, 0.68f, 1.0f), new Vector3(1.2f, 0.74f, 1.55f), wood);
            foreach (var lx in new[] { -1.12f, 1.05f })
                Block(desk, "Leg", new Vector3(lx, 0, 1.05f), new Vector3(lx + 0.07f, 0.68f, 1.5f), darkMetal);
            foreach (var mx in new[] { -0.85f, 0.85f })
            {
                Block(desk, "Monitor", new Vector3(mx - 0.22f, 0.74f, 1.3f), new Vector3(mx + 0.22f, 1.08f, 1.5f), darkMetal);
                Block(desk, "Screen", new Vector3(mx - 0.19f, 0.78f, 1.29f), new Vector3(mx + 0.19f, 1.05f, 1.3f), screen);
            }
            Block(desk, "Cup", new Vector3(0.35f, 0.74f, 1.15f), new Vector3(0.42f, 0.84f, 1.22f), table);
            var supplyBox = Block(desk, "SupplyBox", new Vector3(-0.45f, 0.74f, 1.1f), new Vector3(-0.15f, 0.88f, 1.3f), supply);

            // Posters / clutter
            Block(office, "Poster_A", new Vector3(-2.2f, 1.4f, 2.97f), new Vector3(-1.4f, 2.4f, 3f), poster);
            Block(office, "Poster_B", new Vector3(1.2f, 1.6f, 2.97f), new Vector3(1.9f, 2.3f, 3f), curtain);
            Block(office, "Locker", new Vector3(-2.9f, 0, -2.9f), new Vector3(-2.2f, 2.0f, -2.4f), metal);
            Block(office, "Cabinet", new Vector3(2.2f, 0, -2.9f), new Vector3(2.9f, 1.1f, -2.4f), metal);

            var officeLight = MakeLight("Office Light", office, new Vector3(0, 2.85f, 0.3f), LightType.Point, new Color(1f, 0.85f, 0.65f), 2.2f, 7f);
            officeLight.shadows = LightShadows.Soft;
            officeLight.gameObject.AddComponent<FlickerLight>();
            Block(office, "Lamp", new Vector3(-0.3f, 2.95f, 0.2f), new Vector3(0.3f, 3f, 0.4f), Mat("LampGlow", Color.white, 0f, emission: new Color(1.5f, 1.3f, 1f)));

            // ---- openings (corridor stubs outside the office) -----------------------------
            var openingsRoot = new GameObject("Openings").transform;
            openingsRoot.SetParent(world);
            var leftOpening = Stub(openingsRoot, "Left Door", Opening.Left, side: -1, wall, floor, ceiling);
            var rightOpening = Stub(openingsRoot, "Right Door", Opening.Right, side: 1, wall, floor, ceiling);
            var ventOpening = VentStub(openingsRoot, darkMetal);

            // ---- camera rooms (far away, sealed) -------------------------------------------
            var roomsRoot = new GameObject("Camera Rooms").transform;
            roomsRoot.SetParent(world);

            var stage = CamRoom(roomsRoot, "Show Stage", new Vector3(0, 0, 40), new Vector3(10, 4, 9), wall, floor, ceiling,
                new Color(1f, 0.45f, 0.6f), standOffset: new Vector3(0, 0.6f, 2.2f));
            Block(stage.transform, "Stage", stage.transform.position + new Vector3(-4, 0, 1), stage.transform.position + new Vector3(4, 0.6f, 4.4f), wood, world: true);
            Block(stage.transform, "Curtain_L", stage.transform.position + new Vector3(-5, 0, 4.2f), stage.transform.position + new Vector3(-3.4f, 4, 4.5f), curtain, world: true);
            Block(stage.transform, "Curtain_R", stage.transform.position + new Vector3(3.4f, 0, 4.2f), stage.transform.position + new Vector3(5, 4, 4.5f), curtain, world: true);
            Block(stage.transform, "Curtain_Top", stage.transform.position + new Vector3(-5, 3.2f, 4.2f), stage.transform.position + new Vector3(5, 4, 4.5f), curtain, world: true);

            var dining = CamRoom(roomsRoot, "Dining Hall", new Vector3(-16, 0, 40), new Vector3(11, 3.5f, 10), wall, floor, ceiling,
                new Color(0.7f, 0.75f, 1f), standOffset: new Vector3(0.5f, 0, 1.5f));
            for (int i = 0; i < 3; i++)
            for (int j = 0; j < 2; j++)
            {
                var c = dining.transform.position + new Vector3(-3.5f + i * 3.5f, 0, -2.5f + j * 5f);
                if (i == 1 && j == 1) continue;
                Block(dining.transform, "Table", c + new Vector3(-1.1f, 0.72f, -0.5f), c + new Vector3(1.1f, 0.78f, 0.5f), table, world: true);
                Block(dining.transform, "TableLeg", c + new Vector3(-0.05f, 0, -0.05f), c + new Vector3(0.05f, 0.72f, 0.05f), darkMetal, world: true);
            }

            var westHall = CamRoom(roomsRoot, "West Hall", new Vector3(-32, 0, 40), new Vector3(4, 3, 12), wall, floor, ceiling,
                new Color(0.5f, 1f, 0.6f), standOffset: new Vector3(0, 0, 1.5f));
            Block(westHall.transform, "Poster", westHall.transform.position + new Vector3(-1.98f, 1.2f, -1f), westHall.transform.position + new Vector3(-1.95f, 2.2f, 0.2f), poster, world: true);

            var backstage = CamRoom(roomsRoot, "Backstage", new Vector3(16, 0, 40), new Vector3(9, 3.5f, 9), wall, floor, ceiling,
                new Color(1f, 0.6f, 0.3f), standOffset: new Vector3(-0.5f, 0, 1.2f));
            for (int i = 0; i < 5; i++)
            {
                var c = backstage.transform.position + new Vector3(-3.5f + (i % 3) * 1.2f, 0, 3f - (i / 3) * 1.1f);
                Block(backstage.transform, "Crate", c, c + new Vector3(1f, 0.9f + (i % 2) * 0.4f, 1f), crate, world: true);
            }
            Block(backstage.transform, "SpareHead", backstage.transform.position + new Vector3(3, 0.9f, 3), backstage.transform.position + new Vector3(3.6f, 1.5f, 3.6f), ribbonBox, world: true);

            var eastHall = CamRoom(roomsRoot, "East Hall", new Vector3(32, 0, 40), new Vector3(4, 3, 12), wall, floor, ceiling,
                new Color(0.6f, 0.7f, 1f), standOffset: new Vector3(0, 0, 1.5f));
            Block(eastHall.transform, "Poster", eastHall.transform.position + new Vector3(1.95f, 1.2f, -1f), eastHall.transform.position + new Vector3(1.98f, 2.2f, 0.2f), curtain, world: true);

            var ventShaft = CamRoom(roomsRoot, "Vent Shaft", new Vector3(0, 0, 62), new Vector3(1.6f, 1.4f, 8), darkMetal, darkMetal, darkMetal,
                new Color(1f, 0.5f, 0.2f), standOffset: new Vector3(0, 0, 0.5f), lightIntensity: 3f, camHeight: 1.1f);

            // ---- robots --------------------------------------------------------------------
            var robotsRoot = new GameObject("Robots").transform;
            var tusk = BuildTusk(robotsRoot);
            var ribbon = BuildRibbon(robotsRoot);
            var wick = BuildWick(robotsRoot);
            WireRobot(tusk, tuskData, new[] { stage, dining, westHall, leftOpening }, slot: 0);
            WireRobot(ribbon, ribbonData, new[] { stage, backstage, eastHall, rightOpening }, slot: 2);
            WireRobot(wick, wickData, new[] { stage, ventShaft, ventOpening }, slot: 1);

            // ---- player rig ----------------------------------------------------------------
            var player = new GameObject("Player").transform;
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(UniversalAdditionalCameraData));
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(player);
            camGo.transform.localPosition = new Vector3(0, 1.55f, -0.4f);
            var cam = camGo.GetComponent<Camera>();
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 60f;
            cam.fieldOfView = 68f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGo.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            var view = camGo.AddComponent<OfficeView>();

            var flashlight = MakeLight("Flashlight", camGo.transform, new Vector3(0.15f, -0.1f, 0f), LightType.Spot, new Color(1f, 0.95f, 0.85f), 4f, 14f);
            flashlight.spotAngle = 55f;
            flashlight.innerSpotAngle = 25f;
            flashlight.shadows = LightShadows.Soft;

            var sfx = camGo.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0f;
            var ambience = new GameObject("Ambience", typeof(AudioSource)).GetComponent<AudioSource>();
            ambience.transform.SetParent(camGo.transform, false);
            ambience.playOnAwake = false;
            ambience.volume = 0.5f;
            ambience.spatialBlend = 0f;

            // Shotgun viewmodel with hands, and the reload hand
            var vm = BuildViewModel(camGo.transform);

            // Retro low-res rendering for the world (HUD stays sharp)
            camGo.AddComponent<PixelRenderer>().verticalResolution = 225;

            // Tablet feed camera
            var feedGo = new GameObject("Feed Camera", typeof(Camera), typeof(UniversalAdditionalCameraData));
            var feedCam = feedGo.GetComponent<Camera>();
            feedCam.fieldOfView = 78f;
            feedCam.farClipPlane = 30f;
            feedCam.clearFlags = CameraClearFlags.SolidColor;
            feedCam.backgroundColor = Color.black;
            feedGo.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;

            // ---- systems -------------------------------------------------------------------
            var game = new GameObject("Game");
            game.AddComponent<GameInput>();
            var night = game.AddComponent<NightManager>();
            var shotgun = game.AddComponent<Shotgun>();
            var tablet = game.AddComponent<CameraTablet>();
            var jumpscare = game.AddComponent<Jumpscare>();
            var parryFx = game.AddComponent<ParryFX>();
            var hud = game.AddComponent<HUD>();

            Set(night, "supplyBox", supplyBox);
            Set(night, "shotgun", shotgun);
            Set(night, "jumpscare", jumpscare);
            Set(night, "sfx", sfx);
            Set(night, "ambience", ambience);

            Set(shotgun, "view", view);
            Set(shotgun, "gunRoot", vm.gun);
            Set(shotgun, "pumpSlide", vm.pump);
            Set(shotgun, "muzzleLight", vm.muzzle);
            Set(shotgun, "reloadHand", vm.reloadHand);
            Set(shotgun, "heldShell", vm.shell);
            Set(shotgun, "loadingPort", vm.port);
            Set(shotgun, "pumpHand", vm.pumpHand);
            Set(shotgun, "sfx", sfx);

            Set(tablet, "feedCamera", feedCam);
            SetArray(tablet, "rooms", new Object[] { stage, dining, westHall, backstage, eastHall, ventShaft });
            Set(tablet, "sfx", sfx);
            SetVector2Int(tablet, "feedResolution", new Vector2Int(320, 180));

            Set(parryFx, "shotgun", shotgun);
            Set(parryFx, "cam", cam);
            Set(parryFx, "sparkMaterial", SparkMat());

            Set(jumpscare, "view", view);
            Set(jumpscare, "sfx", sfx);
            SetArray(jumpscare, "flickerLights", new Object[] { officeLight, flashlight });

            Set(hud, "night", night);
            Set(hud, "shotgun", shotgun);
            Set(hud, "view", view);
            Set(hud, "tablet", tablet);
            Set(hud, "jumpscare", jumpscare);
            Set(hud, "parryFx", parryFx);
            Set(hud, "sfx", sfx);

            // ---- post-processing -----------------------------------------------------------
            var volGo = new GameObject("Post Processing", typeof(Volume));
            var vol = volGo.GetComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = BuildVolumeProfile();

            // ---- save ----------------------------------------------------------------------
            EditorSceneManager.SaveScene(scene, ScenePath);
            var buildScenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path != ScenePath) buildScenes.Add(new EditorBuildSettingsScene(s.path, false));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.SaveAssets();
            return ScenePath;
        }

        // ================================================================================
        // Rooms
        // ================================================================================

        static Room Stub(Transform parent, string name, Opening opening, int side, Material wall, Material floor, Material ceiling)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            var t = go.transform;
            float x0 = side < 0 ? -6.4f : 3.2f, x1 = side < 0 ? -3.2f : 6.4f;
            Block(t, "Floor", new Vector3(x0, -0.1f, -0.9f), new Vector3(x1, 0, 0.9f), floor, world: true);
            Block(t, "Ceiling", new Vector3(x0, 2.6f, -0.9f), new Vector3(x1, 2.7f, 0.9f), ceiling, world: true);
            Block(t, "Side_A", new Vector3(x0, 0, -1.0f), new Vector3(x1, 2.7f, -0.9f), wall, world: true);
            Block(t, "Side_B", new Vector3(x0, 0, 0.9f), new Vector3(x1, 2.7f, 1.0f), wall, world: true);
            float ex = side < 0 ? x0 : x1;
            Block(t, "End", new Vector3(ex - 0.1f, 0, -1f), new Vector3(ex + 0.1f, 2.7f, 1f), wall, world: true);

            var room = go.AddComponent<Room>();
            room.displayName = name;
            room.opening = opening;
            var stand = new GameObject("Stand").transform;
            stand.SetParent(t);
            stand.SetPositionAndRotation(new Vector3(side * 4.1f, 0, 0), Quaternion.LookRotation(new Vector3(-side, 0, 0)));
            room.standPoint = stand;
            return room;
        }

        static Room VentStub(Transform parent, Material metal)
        {
            var go = new GameObject("Front Vent");
            go.transform.SetParent(parent);
            var t = go.transform;
            Block(t, "Floor", new Vector3(-0.7f, -0.1f, 3.2f), new Vector3(0.7f, 0, 7f), metal, world: true);
            Block(t, "Top", new Vector3(-0.7f, 1.35f, 3.2f), new Vector3(0.7f, 1.45f, 7f), metal, world: true);
            Block(t, "Side_L", new Vector3(-0.75f, 0, 3.2f), new Vector3(-0.65f, 1.45f, 7f), metal, world: true);
            Block(t, "Side_R", new Vector3(0.65f, 0, 3.2f), new Vector3(0.75f, 1.45f, 7f), metal, world: true);
            Block(t, "End", new Vector3(-0.75f, 0, 7f), new Vector3(0.75f, 1.45f, 7.1f), metal, world: true);

            var room = go.AddComponent<Room>();
            room.displayName = "Front Vent";
            room.opening = Opening.Front;
            var stand = new GameObject("Stand").transform;
            stand.SetParent(t);
            stand.SetPositionAndRotation(new Vector3(0, 0, 4.4f), Quaternion.LookRotation(Vector3.back));
            room.standPoint = stand;
            return room;
        }

        static Room CamRoom(Transform parent, string name, Vector3 center, Vector3 size, Material wall, Material floor, Material ceiling,
            Color lightColor, Vector3 standOffset, float lightIntensity = 9f, float camHeight = -1f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = center;
            var t = go.transform;
            Vector3 h = size * 0.5f;
            Block(t, "Floor", center + new Vector3(-h.x, -0.1f, -h.z), center + new Vector3(h.x, 0, h.z), floor, world: true);
            Block(t, "Ceiling", center + new Vector3(-h.x, size.y, -h.z), center + new Vector3(h.x, size.y + 0.1f, h.z), ceiling, world: true);
            Block(t, "Wall_N", center + new Vector3(-h.x, 0, h.z), center + new Vector3(h.x, size.y, h.z + 0.1f), wall, world: true);
            Block(t, "Wall_S", center + new Vector3(-h.x, 0, -h.z - 0.1f), center + new Vector3(h.x, size.y, -h.z), wall, world: true);
            Block(t, "Wall_W", center + new Vector3(-h.x - 0.1f, 0, -h.z), center + new Vector3(-h.x, size.y, h.z), wall, world: true);
            Block(t, "Wall_E", center + new Vector3(h.x, 0, -h.z), center + new Vector3(h.x + 0.1f, size.y, h.z), wall, world: true);

            MakeLight(name + " Light", t, new Vector3(0, size.y - 0.3f, 0), LightType.Point, lightColor, lightIntensity, Mathf.Max(size.x, size.z) * 0.9f);

            var room = go.AddComponent<Room>();
            room.displayName = name;

            var camAnchor = new GameObject("Camera Anchor").transform;
            camAnchor.SetParent(t);
            float cy = camHeight > 0 ? camHeight : size.y - 0.35f;
            camAnchor.position = center + new Vector3(Mathf.Min(h.x - 0.3f, h.x * 0.8f), cy, -h.z + 0.3f);
            var target = center + standOffset + Vector3.up * 1.0f;
            camAnchor.rotation = Quaternion.LookRotation(target - camAnchor.position);
            room.cameraAnchor = camAnchor;

            var stand = new GameObject("Stand").transform;
            stand.SetParent(t);
            stand.position = center + standOffset;
            var toCam = camAnchor.position - stand.position;
            toCam.y = 0;
            stand.rotation = Quaternion.LookRotation(toCam.sqrMagnitude > 0.01f ? toCam : Vector3.back);
            room.standPoint = stand;

            // Fill light over where robots stand so the feed actually shows them.
            var fill = MakeLight(name + " Fill", stand, new Vector3(0, Mathf.Min(2.4f, size.y - 0.2f), 0.8f), LightType.Point, Color.Lerp(lightColor, Color.white, 0.5f), lightIntensity * 0.7f, 5f);
            fill.transform.rotation = Quaternion.identity;
            return room;
        }

        // ================================================================================
        // Helpers
        // ================================================================================

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>Axis-aligned box spanning min..max (world space when parent is a world-space root).</summary>
        static GameObject Block(Transform parent, string name, Vector3 min, Vector3 max, Material mat, bool world = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, world);
            go.transform.position = (min + max) * 0.5f;
            go.transform.rotation = Quaternion.identity;
            var s = max - min;
            go.transform.localScale = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static Light MakeLight(string name, Transform parent, Vector3 localPos, LightType type, Color color, float intensity, float range)
        {
            var go = new GameObject(name, typeof(Light));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var l = go.GetComponent<Light>();
            l.type = type;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            return l;
        }

        static Material Mat(string name, Color color, float smoothness, Color? emission = null, float metallic = 0f, Texture2D tex = null, float tiling = 1f)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", color);
            m.SetTexture("_BaseMap", tex);
            m.SetTextureScale("_BaseMap", Vector2.one * tiling);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        static RobotData Data(string file, string displayName, float hp, float interval, int aggro, int perHour, Vector2 lurk,
            float telegraph, float lunge, float window, float stun, Color body, Color eyeCol)
        {
            string path = $"{DataDir}/{file}.asset";
            var d = AssetDatabase.LoadAssetAtPath<RobotData>(path);
            bool created = !d;
            if (created) d = ScriptableObject.CreateInstance<RobotData>();
            // Defaults are written on creation, or when the builder tuning is newer than the asset.
            // Otherwise Inspector tuning survives a rebuild.
            const int TuningVersion = 2;
            if (created || d.tuningVersion < TuningVersion)
            {
                d.tuningVersion = TuningVersion;
                d.displayName = displayName;
                d.maxHealth = hp;
                d.moveInterval = interval;
                d.startAggression = aggro;
                d.aggressionPerHour = perHour;
                d.lurkTime = lurk;
                d.telegraphTime = telegraph;
                d.lungeTime = lunge;
                d.parryWindow = window;
                d.stunTime = stun;
                d.bodyColor = body;
                d.eyeColor = eyeCol;
                if (created) AssetDatabase.CreateAsset(d, path);
                else EditorUtility.SetDirty(d);
            }
            return d;
        }

        static VolumeProfile BuildVolumeProfile()
        {
            string path = Root + "/Settings/NightShiftVolume.asset";
            System.IO.Directory.CreateDirectory(Root + "/Settings");
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (existing) AssetDatabase.DeleteAsset(path);

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var vignette = Add<Vignette>(profile);
            vignette.intensity.Override(0.42f);
            vignette.smoothness.Override(0.5f);

            var grain = Add<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Medium3);
            grain.intensity.Override(0.45f);

            var bloom = Add<Bloom>(profile);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(1.1f);
            bloom.scatter.Override(0.6f);

            var color = Add<ColorAdjustments>(profile);
            color.postExposure.Override(0.35f);
            color.contrast.Override(18f);
            color.saturation.Override(-28f);
            color.colorFilter.Override(new Color(0.95f, 1f, 0.95f));

            var ca = Add<ChromaticAberration>(profile);
            ca.intensity.Override(0.15f);

            var tone = Add<Tonemapping>(profile);
            tone.mode.Override(TonemappingMode.Neutral);

            EditorUtility.SetDirty(profile);
            return profile;
        }

        static T Add<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var c = profile.Add<T>(false);
            c.name = typeof(T).Name;
            c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(c, profile);
            return c;
        }

        static void Set(Object target, string prop, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogError($"[Builder] {target.GetType().Name} has no field '{prop}'"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Set(Object target, string prop, GameObject value) => Set(target, prop, (Object)value);

        static void SetVector2Int(Object target, string prop, Vector2Int value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(prop).vector2IntValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetInt(Object target, string prop, int value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(prop).intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(Object target, string prop, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogError($"[Builder] {target.GetType().Name} has no field '{prop}'"); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
