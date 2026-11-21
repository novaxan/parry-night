using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace NightShift.Editor
{
    /// <summary>Mid-poly robots, the shotgun viewmodel with hands, and the pixel-art textures.</summary>
    public static partial class PrototypeSceneBuilder
    {
        const string TexDir = Root + "/Textures";

        // ================================================================================
        // Pixel textures (tiny, point-filtered: they read as chunky retro texels)
        // ================================================================================

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xffff) / 65535f;
            }
        }

        static Texture2D Tex(string name, int size, Func<int, int, float> lum)
        {
            string path = $"{TexDir}/{name}.asset";
            System.IO.Directory.CreateDirectory(TexDir);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float v = Mathf.Clamp01(lum(x, y));
                px[y * size + x] = new Color(v, v, v, 1f);
            }
            tex.SetPixels(px);
            tex.Apply(true);
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing)
            {
                EditorUtility.CopySerialized(tex, existing);
                Object.DestroyImmediate(tex);
                return existing;
            }
            AssetDatabase.CreateAsset(tex, path);
            return tex;
        }

        static Texture2D checkerTex, grimeTex, furTex, plasticTex, metalTex, woodTex, fabricTex, tileTex;

        /// <summary>Classic black/white diner checker with dirty grout.</summary>
        static Texture2D Checker() => checkerTex ??= Tex("Checker", 32, (x, y) =>
        {
            bool white = ((x / 8) + (y / 8)) % 2 == 0;
            float baseV = white ? 0.78f : 0.1f;
            bool grout = x % 8 == 0 || y % 8 == 0;
            return (grout ? baseV * 0.7f : baseV) + (Hash(x, y, 1) - 0.5f) * 0.12f;
        });

        /// <summary>Stained plaster with vertical drips and a few dark blotches.</summary>
        static Texture2D Grime() => grimeTex ??= Tex("Grime", 32, (x, y) =>
        {
            float v = 0.82f + (Hash(x, y, 2) - 0.5f) * 0.14f;
            float streak = Hash(x, 0, 3) > 0.75f ? (1f - y / 32f) * 0.25f : 0f;
            float blot = Hash(x / 4, y / 4, 4) > 0.9f ? 0.2f : 0f;
            return v - streak - blot;
        });

        static Texture2D Fur() => furTex ??= Tex("Fur", 32, (x, y) =>
            0.82f + (Hash(x, y, 5) - 0.5f) * 0.3f + (Hash(x / 2, y / 3, 6) - 0.5f) * 0.12f - (Hash(x / 6, y / 6, 13) > 0.85f ? 0.15f : 0f));

        static Texture2D Plastic() => plasticTex ??= Tex("Plastic", 32, (x, y) =>
            0.92f + (Hash(x, y, 7) - 0.5f) * 0.08f - ((x + y * 3) % 23 == 0 ? 0.25f : 0f) - (Hash(x / 5, y / 5, 14) > 0.88f ? 0.12f : 0f));

        static Texture2D MetalPanel() => metalTex ??= Tex("MetalPanel", 32, (x, y) =>
        {
            float v = 0.75f + (Hash(x, y, 8) - 0.5f) * 0.12f;
            if (x % 16 == 0 || y % 16 == 0) v -= 0.35f;
            if ((x % 16 == 3 || x % 16 == 13) && (y % 16 == 3 || y % 16 == 13)) v += 0.2f;   // rivets
            return v;
        });

        static Texture2D WoodGrain() => woodTex ??= Tex("WoodGrain", 32, (x, y) =>
            0.7f + Mathf.Sin(y * 0.9f + Mathf.Sin(x * 0.3f) * 2f) * 0.12f + (Hash(x, y, 9) - 0.5f) * 0.1f);

        static Texture2D Fabric() => fabricTex ??= Tex("Fabric", 32, (x, y) =>
            0.75f + Mathf.Sin(x * 0.8f) * 0.18f + (Hash(x, y, 10) - 0.5f) * 0.1f);

        static Texture2D CeilingTile() => tileTex ??= Tex("CeilingTile", 32, (x, y) =>
            (x % 16 == 0 || y % 16 == 0) ? 0.45f : 0.85f - (Hash(x, y, 11) > 0.85f ? 0.2f : 0f));

        // ================================================================================
        // Part helpers
        // ================================================================================

        static GameObject Part(string name, Transform parent, Mesh mesh, Vector3 pos, Vector3 scale, Material mat, Vector3? euler = null)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static Transform Node(string name, Transform parent, Vector3 pos, Vector3? euler = null)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            if (euler.HasValue) t.localRotation = Quaternion.Euler(euler.Value);
            return t;
        }

        static Mesh Sph => MeshGen.Sphere();
        static Mesh Cap => MeshGen.Capsule();
        static Mesh Cyl => MeshGen.Cylinder();
        static Mesh ConeM => MeshGen.Cone();
        static Mesh RBox(float x, float y, float z, float r) => MeshGen.RoundedBox(new Vector3(x, y, z), r, 4);

        static Material EyeMat() => Mat("RobotEye", Color.white, 0.9f, emission: Color.white * 2f);
        static Material DarkMat() => Mat("RobotDark", new Color(0.03f, 0.03f, 0.035f), 0.4f);
        static Material EndoMat() => Mat("Endo", new Color(0.42f, 0.43f, 0.46f), 0.65f, metallic: 0.8f, tex: MetalPanel());

        // ================================================================================
        // Robots
        // ================================================================================

        class RobotParts
        {
            public GameObject root;
            public Transform visual, eyes, jaw;
            public float jawAngle = 35f;
            public Renderer[] eyeRenderers;
            public Light eyeLight;
        }

        static RobotParts NewRobot(Transform parent, string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            return new RobotParts { root = root, visual = visual };
        }

        static void AddEyes(RobotParts p, Transform parent, Vector3 pos, float spread, float size, Color lightColor)
        {
            var eye = EyeMat();
            p.eyes = Node("Eyes", parent, pos);
            var l = Part("Eye_L", p.eyes, Sph, new Vector3(-spread, 0, 0), Vector3.one * size, eye);
            var r = Part("Eye_R", p.eyes, Sph, new Vector3(spread, 0, 0), Vector3.one * size, eye);
            p.eyeRenderers = new Renderer[] { l.GetComponent<Renderer>(), r.GetComponent<Renderer>() };
            p.eyeLight = MakeLight("EyeGlow", p.eyes, new Vector3(0, 0, 0.25f), LightType.Point, lightColor, 0.4f, 2.5f);
        }

        /// <summary>Tusk: a big, pear-shaped walrus mascot with a bowler hat, bow tie and hinged jaw.</summary>
        static RobotParts BuildTusk(Transform parent)
        {
            var body = Mat("TuskBody", new Color(0.5f, 0.42f, 0.38f), 0.25f, tex: Fur(), tiling: 2f);
            var belly = Mat("TuskBelly", new Color(0.82f, 0.72f, 0.6f), 0.2f, tex: Fur(), tiling: 2f);
            var ivory = Mat("TuskIvory", new Color(0.93f, 0.89f, 0.74f), 0.6f, tex: Plastic());
            var red = Mat("BowRed", new Color(0.65f, 0.06f, 0.08f), 0.4f, tex: Plastic());
            var dark = DarkMat();

            var p = NewRobot(parent, "Tusk");
            var v = p.visual;

            var torso = MeshGen.Lathe("TuskTorso", new[]
            {
                new Vector2(0f, 0f), new Vector2(0.3f, 0.02f), new Vector2(0.45f, 0.14f), new Vector2(0.53f, 0.38f),
                new Vector2(0.55f, 0.62f), new Vector2(0.49f, 0.88f), new Vector2(0.37f, 1.06f), new Vector2(0.2f, 1.16f), new Vector2(0f, 1.18f)
            }, 24);
            Part("Torso", v, torso, new Vector3(0, 0.6f, 0), new Vector3(1f, 1f, 0.82f), body);
            Part("Belly", v, Sph, new Vector3(0, 1.1f, 0.26f), new Vector3(0.66f, 0.8f, 0.36f), belly);

            foreach (int s in new[] { -1, 1 })
            {
                Part("Leg", v, Cap, new Vector3(s * 0.25f, 0.38f, 0), new Vector3(0.3f, 0.2f, 0.3f), body);
                Part("Foot", v, Sph, new Vector3(s * 0.27f, 0.08f, 0.1f), new Vector3(0.34f, 0.16f, 0.5f), body);
                Part("Shoulder", v, Sph, new Vector3(s * 0.53f, 1.43f, 0), Vector3.one * 0.3f, body);
                Part("UpperArm", v, Cap, new Vector3(s * 0.62f, 1.22f, 0.02f), new Vector3(0.22f, 0.17f, 0.22f), body, new Vector3(0, 0, s * 12f));
                Part("Elbow", v, Sph, new Vector3(s * 0.67f, 1.0f, 0.04f), Vector3.one * 0.21f, body);
                Part("Forearm", v, Cap, new Vector3(s * 0.68f, 0.82f, 0.12f), new Vector3(0.2f, 0.15f, 0.2f), body, new Vector3(-25f, 0, 0));
                Part("Flipper", v, Sph, new Vector3(s * 0.69f, 0.63f, 0.22f), new Vector3(0.24f, 0.1f, 0.32f), belly, new Vector3(-30f, 0, s * 10f));
                for (int k = 0; k < 3; k++)
                    Part("Finger", v, Cap, new Vector3(s * 0.69f + (k - 1) * 0.065f, 0.56f, 0.35f), new Vector3(0.05f, 0.045f, 0.05f), dark, new Vector3(-60f, 0, 0));
                // Bow tie wing: cone base outside, tip toward the centre knot.
                Part("BowWing", v, ConeM, new Vector3(s * 0.22f, 1.72f, 0.38f), new Vector3(0.2f, 0.2f, 0.12f), red, new Vector3(0, 0, s * 90f));
            }
            Part("BowKnot", v, Sph, new Vector3(0, 1.72f, 0.4f), Vector3.one * 0.08f, red);

            var head = Node("Head", v, new Vector3(0, 1.98f, 0.04f));
            Part("Skull", head, Sph, Vector3.zero, new Vector3(0.8f, 0.72f, 0.74f), body);
            foreach (int s in new[] { -1, 1 })
            {
                Part("Socket", head, Sph, new Vector3(s * 0.15f, 0.09f, 0.27f), Vector3.one * 0.15f, dark);
                Part("Cheek", head, Sph, new Vector3(s * 0.13f, -0.12f, 0.28f), new Vector3(0.36f, 0.3f, 0.32f), belly);
                Part("Brow", head, RBox(0.18f, 0.05f, 0.06f, 0.015f), new Vector3(s * 0.15f, 0.2f, 0.31f), Vector3.one, dark, new Vector3(0, 0, s * 14f));
                Part("Ear", head, Sph, new Vector3(s * 0.38f, 0.08f, 0f), Vector3.one * 0.12f, body);
                Part("Tusk", head, ConeM, new Vector3(s * 0.11f, -0.16f, 0.38f), new Vector3(0.08f, 0.42f, 0.08f), ivory, new Vector3(170f, 0, s * -4f));
            }
            Part("Nose", head, Sph, new Vector3(0, -0.01f, 0.42f), new Vector3(0.17f, 0.12f, 0.11f), dark);
            Part("MouthDark", head, Sph, new Vector3(0, -0.22f, 0.2f), new Vector3(0.36f, 0.14f, 0.26f), dark);
            for (int i = 0; i < 5; i++)
                Part("TopTooth", head, ConeM, new Vector3(-0.1f + i * 0.05f, -0.19f, 0.31f - Mathf.Abs(i - 2) * 0.02f), new Vector3(0.035f, 0.05f, 0.035f), ivory, new Vector3(180f, 0, 0));

            p.jaw = Node("Jaw", head, new Vector3(0, -0.2f, 0.12f));
            p.jawAngle = 32f;
            Part("LowerJaw", p.jaw, Sph, new Vector3(0, -0.04f, 0.12f), new Vector3(0.44f, 0.16f, 0.36f), belly);
            for (int i = 0; i < 5; i++)
                Part("Tooth", p.jaw, ConeM, new Vector3(-0.1f + i * 0.05f, 0.02f, 0.24f - Mathf.Abs(i - 2) * 0.02f), new Vector3(0.035f, 0.055f, 0.035f), ivory);

            var hat = MeshGen.Lathe("BowlerHat", new[]
            {
                new Vector2(0f, 0f), new Vector2(0.32f, 0f), new Vector2(0.33f, 0.02f), new Vector2(0.2f, 0.035f),
                new Vector2(0.2f, 0.17f), new Vector2(0.17f, 0.25f), new Vector2(0.1f, 0.28f), new Vector2(0f, 0.29f)
            }, 22);
            var hatNode = Node("Hat", head, new Vector3(0, 0.29f, -0.02f), new Vector3(-10f, 0, 6f));
            Part("Bowler", hatNode, hat, Vector3.zero, Vector3.one, dark);
            Part("Band", hatNode, MeshGen.Torus(0.2f, 0.018f), new Vector3(0, 0.06f, 0), Vector3.one, red);

            AddEyes(p, head, new Vector3(0, 0.09f, 0.31f), 0.15f, 0.1f, new Color(1f, 0.95f, 0.7f));
            return p;
        }

        /// <summary>Ribbon: a lanky jester whose head is a gift box. The lid flips open to show teeth.</summary>
        static RobotParts BuildRibbon(Transform parent)
        {
            var purple = Mat("RibbonBody", new Color(0.45f, 0.13f, 0.55f), 0.35f, tex: Fabric(), tiling: 2f);
            var box = Mat("RibbonBox", new Color(0.9f, 0.28f, 0.48f), 0.45f, tex: Plastic());
            var gold = Mat("RibbonGold", new Color(0.95f, 0.75f, 0.15f), 0.7f, metallic: 0.6f, tex: Plastic());
            var white = Mat("RibbonRuff", new Color(0.9f, 0.88f, 0.84f), 0.3f, tex: Fabric());
            var endo = EndoMat();
            var dark = DarkMat();

            var p = NewRobot(parent, "Ribbon");
            var v = p.visual;

            foreach (int s in new[] { -1, 1 })
            {
                Part("Thigh", v, Cap, new Vector3(s * 0.15f, 0.72f, 0), new Vector3(0.13f, 0.14f, 0.13f), purple);
                Part("Knee", v, Sph, new Vector3(s * 0.15f, 0.49f, 0), Vector3.one * 0.12f, endo);
                Part("Shin", v, Cap, new Vector3(s * 0.15f, 0.27f, 0), new Vector3(0.11f, 0.13f, 0.11f), gold);
                Part("Shoe", v, Sph, new Vector3(s * 0.15f, 0.06f, 0.08f), new Vector3(0.16f, 0.12f, 0.38f), purple);
                Part("ShoeCurl", v, ConeM, new Vector3(s * 0.15f, 0.07f, 0.24f), new Vector3(0.09f, 0.16f, 0.09f), purple, new Vector3(-55f, 0, 0));
                Part("ShoeBell", v, Sph, new Vector3(s * 0.15f, 0.2f, 0.33f), Vector3.one * 0.06f, gold);

                Part("Shoulder", v, Sph, new Vector3(s * 0.36f, 1.62f, 0), Vector3.one * 0.13f, endo);
                Part("UpperArm", v, Cap, new Vector3(s * 0.4f, 1.42f, 0), new Vector3(0.1f, 0.11f, 0.1f), purple, new Vector3(0, 0, s * 8f));
                Part("Elbow", v, Sph, new Vector3(s * 0.43f, 1.22f, 0.02f), Vector3.one * 0.1f, endo);
                Part("Forearm", v, Cap, new Vector3(s * 0.45f, 1.02f, 0.07f), new Vector3(0.09f, 0.11f, 0.09f), gold, new Vector3(-14f, 0, 0));
                Part("Hand", v, RBox(0.06f, 0.13f, 0.11f, 0.02f), new Vector3(s * 0.46f, 0.84f, 0.11f), Vector3.one, white);
                for (int k = 0; k < 4; k++)
                    Part("Finger", v, Cap, new Vector3(s * 0.46f, 0.72f, 0.07f + k * 0.028f), new Vector3(0.022f, 0.05f, 0.022f), white, new Vector3(-8f, 0, 0));
            }

            Part("Hips", v, RBox(0.46f, 0.18f, 0.3f, 0.06f), new Vector3(0, 0.98f, 0), Vector3.one, purple);
            Part("Torso", v, RBox(0.6f, 0.66f, 0.38f, 0.1f), new Vector3(0, 1.38f, 0), Vector3.one, purple);
            for (int i = 0; i < 3; i++)
                Part("Diamond", v, RBox(0.1f, 0.1f, 0.02f, 0.01f), new Vector3(0, 1.2f + i * 0.17f, 0.19f), Vector3.one, gold, new Vector3(0, 0, 45f));
            Part("Ruff", v, MeshGen.Torus(0.28f, 0.07f, 40, 10, 0.55f, 10), new Vector3(0, 1.74f, 0), Vector3.one, white);
            Part("Neck", v, Cyl, new Vector3(0, 1.84f, 0), new Vector3(0.09f, 0.16f, 0.09f), endo);
            Part("NeckRing", v, MeshGen.Torus(0.05f, 0.012f), new Vector3(0, 1.88f, 0), Vector3.one, endo);

            var head = Node("Head", v, new Vector3(0, 2.12f, 0));
            Part("Box", head, RBox(0.58f, 0.42f, 0.58f, 0.04f), new Vector3(0, -0.07f, 0), Vector3.one, box);
            Part("StrapV", head, RBox(0.08f, 0.43f, 0.59f, 0.01f), new Vector3(0, -0.07f, 0), Vector3.one, gold);
            Part("StrapH", head, RBox(0.59f, 0.43f, 0.08f, 0.01f), new Vector3(0, -0.07f, 0), Vector3.one, gold);
            Part("Inside", head, RBox(0.52f, 0.04f, 0.52f, 0.01f), new Vector3(0, 0.13f, 0), Vector3.one, dark);
            for (int i = 0; i < 7; i++)
                Part("Tooth", head, ConeM, new Vector3(-0.21f + i * 0.07f, 0.13f, 0.25f), new Vector3(0.04f, 0.08f, 0.04f), white);
            foreach (int s in new[] { -1, 1 })
                Part("Socket", head, Sph, new Vector3(s * 0.13f, 0.0f, 0.27f), Vector3.one * 0.14f, dark);
            Part("Grin", head, MeshGen.Torus(0.13f, 0.016f), new Vector3(0, -0.16f, 0.29f), new Vector3(1f, 1f, 0.45f), dark, new Vector3(90f, 0, 0));

            // Lid hinged on the back edge; negative angle lifts the front.
            p.jaw = Node("Lid", head, new Vector3(0, 0.14f, -0.29f));
            p.jawAngle = -55f;
            Part("LidTop", p.jaw, RBox(0.62f, 0.1f, 0.62f, 0.03f), new Vector3(0, 0.05f, 0.29f), Vector3.one, box);
            Part("LidStrapV", p.jaw, RBox(0.08f, 0.11f, 0.63f, 0.01f), new Vector3(0, 0.05f, 0.29f), Vector3.one, gold);
            Part("LidStrapH", p.jaw, RBox(0.63f, 0.11f, 0.08f, 0.01f), new Vector3(0, 0.05f, 0.29f), Vector3.one, gold);
            foreach (int s in new[] { -1, 1 })
                Part("BowLoop", p.jaw, Sph, new Vector3(s * 0.12f, 0.17f, 0.29f), new Vector3(0.24f, 0.14f, 0.12f), gold, new Vector3(0, 0, s * 25f));
            Part("BowKnot", p.jaw, Sph, new Vector3(0, 0.14f, 0.29f), Vector3.one * 0.08f, gold);

            AddEyes(p, head, new Vector3(0, 0f, 0.3f), 0.13f, 0.09f, new Color(0.4f, 1f, 1f));
            return p;
        }

        /// <summary>Wick: a squat mole in round spectacles, a lit candle melting on its head and huge digging claws.</summary>
        static RobotParts BuildWick(Transform parent)
        {
            var fur = Mat("WickBody", new Color(0.33f, 0.24f, 0.18f), 0.2f, tex: Fur(), tiling: 2f);
            var belly = Mat("WickBelly", new Color(0.55f, 0.42f, 0.32f), 0.2f, tex: Fur(), tiling: 2f);
            var pink = Mat("WickSnout", new Color(0.88f, 0.52f, 0.56f), 0.35f, tex: Plastic());
            var wax = Mat("Wax", new Color(0.93f, 0.9f, 0.8f), 0.35f, tex: Plastic());
            var ivory = Mat("TuskIvory", new Color(0.93f, 0.89f, 0.74f), 0.6f, tex: Plastic());
            var gold = Mat("RibbonGold", new Color(0.95f, 0.75f, 0.15f), 0.7f, metallic: 0.6f, tex: Plastic());
            var flame = Mat("Flame", new Color(1f, 0.6f, 0.1f), 0f, emission: new Color(4f, 1.8f, 0.3f));
            var dark = DarkMat();

            var p = NewRobot(parent, "Wick");
            var v = p.visual;

            Part("Body", v, Sph, new Vector3(0, 0.45f, -0.05f), new Vector3(0.86f, 0.76f, 1.0f), fur);
            Part("Belly", v, Sph, new Vector3(0, 0.42f, 0.28f), new Vector3(0.6f, 0.55f, 0.4f), belly);
            foreach (int s in new[] { -1, 1 })
            {
                Part("BackFoot", v, Sph, new Vector3(s * 0.28f, 0.07f, -0.25f), new Vector3(0.24f, 0.14f, 0.36f), pink);
                Part("Arm", v, Cap, new Vector3(s * 0.34f, 0.3f, 0.28f), new Vector3(0.16f, 0.1f, 0.16f), fur, new Vector3(-60f, 0, 0));
                Part("Palm", v, Sph, new Vector3(s * 0.36f, 0.14f, 0.42f), new Vector3(0.28f, 0.08f, 0.24f), pink);
                for (int k = 0; k < 3; k++)
                    Part("Claw", v, ConeM, new Vector3(s * 0.36f + (k - 1) * 0.07f, 0.13f, 0.5f), new Vector3(0.06f, 0.22f, 0.06f), ivory, new Vector3(82f, 0, 0));
            }

            var head = Node("Head", v, new Vector3(0, 0.74f, 0.32f));
            Part("Skull", head, Sph, Vector3.zero, new Vector3(0.54f, 0.5f, 0.54f), fur);
            Part("Snout", head, ConeM, new Vector3(0, -0.06f, 0.18f), new Vector3(0.22f, 0.3f, 0.2f), fur, new Vector3(90f, 0, 0));
            Part("Nose", head, Sph, new Vector3(0, -0.06f, 0.49f), Vector3.one * 0.13f, pink);
            foreach (int s in new[] { -1, 1 })
            {
                Part("BuckTooth", head, RBox(0.045f, 0.07f, 0.02f, 0.008f), new Vector3(s * 0.026f, -0.16f, 0.33f), Vector3.one, ivory);
                Part("Lens", head, MeshGen.Torus(0.055f, 0.011f), new Vector3(s * 0.09f, 0.08f, 0.25f), Vector3.one, gold, new Vector3(90f, 0, 0));
                Part("Ear", head, Sph, new Vector3(s * 0.24f, 0.1f, -0.04f), new Vector3(0.1f, 0.12f, 0.06f), pink);
            }
            Part("Bridge", head, Cyl, new Vector3(0, 0.08f, 0.27f), new Vector3(0.012f, 0.05f, 0.012f), gold, new Vector3(0, 0, 90f));

            p.jaw = Node("Jaw", head, new Vector3(0, -0.15f, 0.12f));
            p.jawAngle = 30f;
            Part("LowerJaw", p.jaw, Sph, new Vector3(0, -0.02f, 0.14f), new Vector3(0.22f, 0.08f, 0.26f), pink);
            Part("MouthDark", head, Sph, new Vector3(0, -0.16f, 0.24f), new Vector3(0.16f, 0.06f, 0.16f), dark);

            var candle = Node("Candle", head, new Vector3(0, 0.22f, -0.02f));
            Part("Holder", candle, MeshGen.Torus(0.09f, 0.025f), Vector3.zero, Vector3.one, gold);
            Part("Dish", candle, Cyl, new Vector3(0, -0.01f, 0), new Vector3(0.2f, 0.02f, 0.2f), gold);
            Part("Wax", candle, Cyl, new Vector3(0, 0.13f, 0), new Vector3(0.13f, 0.26f, 0.13f), wax);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 1.1f;
                Part("Drip", candle, Cap, new Vector3(Mathf.Cos(a) * 0.064f, 0.2f - (i % 3) * 0.05f, Mathf.Sin(a) * 0.064f), new Vector3(0.03f, 0.04f + (i % 2) * 0.02f, 0.03f), wax);
            }
            Part("Wick", candle, Cyl, new Vector3(0, 0.28f, 0), new Vector3(0.012f, 0.04f, 0.012f), dark);
            var flameMesh = MeshGen.Lathe("Flame", new[]
            {
                new Vector2(0f, 0f), new Vector2(0.035f, 0.015f), new Vector2(0.05f, 0.05f), new Vector2(0.04f, 0.1f),
                new Vector2(0.02f, 0.15f), new Vector2(0f, 0.18f)
            }, 12);
            Part("Flame", candle, flameMesh, new Vector3(0, 0.29f, 0), Vector3.one, flame);
            var fl = MakeLight("CandleLight", candle, new Vector3(0, 0.38f, 0), LightType.Point, new Color(1f, 0.6f, 0.2f), 0.8f, 3f);
            fl.gameObject.AddComponent<FlickerLight>();

            AddEyes(p, head, new Vector3(0, 0.08f, 0.24f), 0.09f, 0.065f, new Color(1f, 0.6f, 0.2f));
            return p;
        }

        static void WireRobot(RobotParts p, RobotData data, Room[] route, int slot)
        {
            var voice = p.root.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 1f;
            voice.minDistance = 2f;
            voice.maxDistance = 25f;
            voice.rolloffMode = AudioRolloffMode.Linear;

            var robot = p.root.AddComponent<Robot>();
            Set(robot, "data", data);
            SetArray(robot, "route", route);
            SetInt(robot, "slot", slot);
            Set(robot, "visual", p.visual);
            Set(robot, "eyes", p.eyes);
            SetArray(robot, "eyeRenderers", p.eyeRenderers);
            Set(robot, "eyeLight", p.eyeLight);
            Set(robot, "voice", voice);
            Set(robot, "jaw", p.jaw);
            SetFloat(robot, "jawOpenAngle", p.jawAngle);

            p.root.transform.SetPositionAndRotation(route[0].GetStandPosition(slot), route[0].GetStandRotation());
        }

        // ================================================================================
        // Shotgun viewmodel + hands
        // ================================================================================

        class ViewModelParts
        {
            public Transform gun, pump, port, reloadHand, shell, pumpHand;
            public Light muzzle;
        }

        static ViewModelParts BuildViewModel(Transform cam)
        {
            var gunMetal = Mat("GunMetal", new Color(0.16f, 0.17f, 0.19f), 0.55f, metallic: 0.85f, tex: MetalPanel(), tiling: 0.5f);
            var wood = Mat("GunWood", new Color(0.45f, 0.25f, 0.12f), 0.35f, tex: WoodGrain());
            var dark = DarkMat();
            var glove = Mat("Glove", new Color(0.42f, 0.28f, 0.17f), 0.35f, tex: Plastic(), tiling: 2f);
            var sleeve = Mat("Uniform", new Color(0.12f, 0.16f, 0.3f), 0.2f, tex: Fabric(), tiling: 3f);
            var shellRed = Mat("ShellRed", new Color(0.75f, 0.08f, 0.08f), 0.5f, tex: Plastic());
            var brass = Mat("Brass", new Color(0.85f, 0.65f, 0.25f), 0.8f, metallic: 0.9f);

            var vm = new ViewModelParts();
            var gun = vm.gun = Node("Gun", cam, new Vector3(0.26f, -0.26f, 0.42f), new Vector3(2f, -4f, 0f));

            Part("Receiver", gun, RBox(0.07f, 0.1f, 0.32f, 0.012f), Vector3.zero, Vector3.one, gunMetal);
            Part("EjectionPort", gun, RBox(0.006f, 0.03f, 0.08f, 0.002f), new Vector3(0.036f, 0.015f, 0.03f), Vector3.one, dark);
            Part("Barrel", gun, Cyl, new Vector3(0, 0.025f, 0.45f), new Vector3(0.045f, 0.6f, 0.045f), gunMetal, new Vector3(90f, 0, 0));
            Part("Muzzle", gun, MeshGen.Torus(0.022f, 0.006f), new Vector3(0, 0.025f, 0.75f), Vector3.one, gunMetal, new Vector3(90f, 0, 0));
            Part("Bead", gun, Sph, new Vector3(0, 0.05f, 0.73f), Vector3.one * 0.012f, brass);
            Part("Tube", gun, Cyl, new Vector3(0, -0.022f, 0.39f), new Vector3(0.04f, 0.46f, 0.04f), gunMetal, new Vector3(90f, 0, 0));
            Part("TubeCap", gun, Cyl, new Vector3(0, -0.022f, 0.62f), new Vector3(0.043f, 0.02f, 0.043f), dark, new Vector3(90f, 0, 0));
            Part("TriggerGuard", gun, MeshGen.Torus(0.026f, 0.006f), new Vector3(0, -0.07f, -0.05f), Vector3.one, gunMetal, new Vector3(0, 0, 90f));
            Part("Trigger", gun, RBox(0.008f, 0.03f, 0.01f, 0.003f), new Vector3(0, -0.065f, -0.045f), Vector3.one, dark, new Vector3(15f, 0, 0));
            Part("Grip", gun, RBox(0.05f, 0.12f, 0.065f, 0.015f), new Vector3(0, -0.1f, -0.11f), Vector3.one, wood, new Vector3(-22f, 0, 0));
            Part("Stock", gun, RBox(0.058f, 0.12f, 0.32f, 0.02f), new Vector3(0, -0.06f, -0.3f), Vector3.one, wood, new Vector3(8f, 0, 0));
            Part("ButtPad", gun, RBox(0.06f, 0.13f, 0.025f, 0.01f), new Vector3(0, -0.085f, -0.46f), Vector3.one, dark, new Vector3(8f, 0, 0));

            vm.pump = Node("Pump", gun, new Vector3(0, -0.03f, 0.34f));
            Part("Forend", vm.pump, RBox(0.072f, 0.065f, 0.19f, 0.018f), Vector3.zero, Vector3.one, wood);
            for (int k = 0; k < 4; k++)
                Part("Groove", vm.pump, RBox(0.075f, 0.05f, 0.007f, 0.002f), new Vector3(0, 0, -0.06f + k * 0.04f), Vector3.one, dark);

            vm.port = Node("LoadingPort", gun, new Vector3(0, -0.056f, 0.03f));
            vm.muzzle = MakeLight("Muzzle Flash", gun, new Vector3(0, 0.03f, 0.8f), LightType.Point, new Color(1f, 0.7f, 0.3f), 0f, 8f);
            vm.muzzle.enabled = false;

            // Hands holding the gun
            var gripHand = Node("RightHand", gun, new Vector3(0.0f, -0.12f, -0.105f), new Vector3(-22f, 0, 0));
            BuildHand(gripHand, glove, curl: 80f);
            Part("Sleeve", gripHand, Cyl, new Vector3(0.01f, -0.03f, -0.13f), new Vector3(0.075f, 0.16f, 0.075f), sleeve, new Vector3(80f, 0, 0));

            vm.pumpHand = Node("LeftHand", vm.pump, new Vector3(0, -0.045f, 0f), new Vector3(0, 0, 0));
            BuildHand(vm.pumpHand, glove, curl: 75f);
            Part("Sleeve", vm.pumpHand, Cyl, new Vector3(-0.03f, -0.06f, -0.13f), new Vector3(0.075f, 0.16f, 0.075f), sleeve, new Vector3(60f, -20f, 0));

            // Reload hand (parented to the camera, animated by Shotgun) holding a shell.
            vm.reloadHand = Node("Reload Hand", cam, new Vector3(-0.05f, -0.6f, 0.3f));
            BuildHand(vm.reloadHand, glove, curl: 35f);
            Part("Sleeve", vm.reloadHand, Cyl, new Vector3(0, -0.02f, -0.14f), new Vector3(0.075f, 0.18f, 0.075f), sleeve, new Vector3(90f, 0, 0));
            vm.shell = Node("Shell", vm.reloadHand, new Vector3(0, 0.035f, 0.075f), new Vector3(90f, 0, 0));
            Part("Hull", vm.shell, Cyl, new Vector3(0, 0.012f, 0), new Vector3(0.026f, 0.06f, 0.026f), shellRed);
            Part("Base", vm.shell, Cyl, new Vector3(0, -0.026f, 0), new Vector3(0.028f, 0.018f, 0.028f), brass);
            Part("Rim", vm.shell, MeshGen.Torus(0.014f, 0.003f), new Vector3(0, -0.034f, 0), Vector3.one, brass);

            foreach (var r in gun.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
            foreach (var r in vm.reloadHand.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
            return vm;
        }

        /// <summary>A gloved hand: palm, four two-segment fingers and a thumb. curl = finger bend in degrees.</summary>
        static void BuildHand(Transform parent, Material mat, float curl)
        {
            Part("Palm", parent, RBox(0.085f, 0.032f, 0.095f, 0.014f), Vector3.zero, Vector3.one, mat);
            for (int i = 0; i < 4; i++)
            {
                float x = -0.03f + i * 0.02f;
                var f = Node("Finger" + i, parent, new Vector3(x, 0f, 0.045f), new Vector3(curl * 0.5f, 0, 0));
                Part("Seg1", f, Cap, new Vector3(0, 0, 0.018f), new Vector3(0.018f, 0.014f, 0.018f), mat, new Vector3(90f, 0, 0));
                var f2 = Node("Joint", f, new Vector3(0, 0, 0.034f), new Vector3(curl * 0.6f, 0, 0));
                Part("Seg2", f2, Cap, new Vector3(0, 0, 0.014f), new Vector3(0.016f, 0.012f, 0.016f), mat, new Vector3(90f, 0, 0));
            }
            var th = Node("Thumb", parent, new Vector3(-0.045f, 0.005f, 0.01f), new Vector3(10f, -45f, 0));
            Part("ThumbSeg", th, Cap, new Vector3(0, 0, 0.022f), new Vector3(0.02f, 0.02f, 0.02f), mat, new Vector3(90f, 0, 0));
        }

        static Material SparkMat()
        {
            string path = $"{MatDir}/Sparks.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", new Color(4f, 3f, 1.6f));
            EditorUtility.SetDirty(m);
            return m;
        }

        static void SetFloat(Object target, string prop, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(prop).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
