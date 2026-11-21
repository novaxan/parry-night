using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NightShift.Editor
{
    /// <summary>
    /// Procedural mid-poly meshes for the scene builder: lathed (revolved) shapes, rounded boxes, tori.
    /// Each mesh is saved under Assets/_Project/Meshes so scenes reference it as an asset.
    /// </summary>
    public static class MeshGen
    {
        const string Dir = "Assets/_Project/Meshes";
        static readonly Dictionary<string, Mesh> cache = new();

        public static void ClearCache() => cache.Clear();

        /// <summary>
        /// Revolves a profile of (radius, height) points around the Y axis. Points with radius 0 close the
        /// ends. Smooth normals; UVs wrap around (u) and follow the profile (v).
        /// </summary>
        public static Mesh Lathe(string name, Vector2[] profile, int segments = 20)
        {
            return Get(name, () =>
            {
                var verts = new List<Vector3>();
                var uvs = new List<Vector2>();
                var tris = new List<int>();
                int rings = profile.Length;
                for (int i = 0; i < rings; i++)
                for (int s = 0; s <= segments; s++)
                {
                    float a = s / (float)segments * Mathf.PI * 2f;
                    verts.Add(new Vector3(Mathf.Cos(a) * profile[i].x, profile[i].y, Mathf.Sin(a) * profile[i].x));
                    uvs.Add(new Vector2(s / (float)segments, i / (float)(rings - 1)));
                }
                int row = segments + 1;
                for (int i = 0; i < rings - 1; i++)
                for (int s = 0; s < segments; s++)
                {
                    int a = i * row + s, b = a + 1, c = a + row, d = c + 1;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
                return Build(name, verts, uvs, tris);
            });
        }

        /// <summary>A sphere-ish lathe (UV sphere) of radius 0.5.</summary>
        public static Mesh Sphere(int segments = 20, int rings = 12)
        {
            var p = new Vector2[rings + 1];
            for (int i = 0; i <= rings; i++)
            {
                float a = Mathf.PI * i / rings;
                p[i] = new Vector2(Mathf.Sin(a) * 0.5f, -Mathf.Cos(a) * 0.5f);
            }
            return Lathe($"Sphere_{segments}x{rings}", p, segments);
        }

        /// <summary>Capped cylinder of radius 0.5, height 1 (centred), with a small bevel.</summary>
        public static Mesh Cylinder(int segments = 18, float bevel = 0.06f)
        {
            return Lathe($"Cylinder_{segments}", new[]
            {
                new Vector2(0f, -0.5f), new Vector2(0.5f - bevel, -0.5f), new Vector2(0.5f, -0.5f + bevel),
                new Vector2(0.5f, 0.5f - bevel), new Vector2(0.5f - bevel, 0.5f), new Vector2(0f, 0.5f)
            }, segments);
        }

        /// <summary>Tapered cone: base radius 0.5 at y=0, tip at y=1 (rounded tip).</summary>
        public static Mesh Cone(int segments = 14, float tipRadius = 0.06f)
        {
            return Lathe($"Cone_{segments}_{tipRadius:F2}", new[]
            {
                new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(0.38f, 0.35f),
                new Vector2(0.22f, 0.7f), new Vector2(tipRadius, 0.97f), new Vector2(0f, 1f)
            }, segments);
        }

        /// <summary>Capsule matching Unity's built-in proportions: radius 0.5, total height 2, centred.</summary>
        public static Mesh Capsule(int segments = 16, int capRings = 5)
        {
            var p = new List<Vector2>();
            for (int i = 0; i <= capRings; i++)
            {
                float a = Mathf.PI * 0.5f * i / capRings;
                p.Add(new Vector2(Mathf.Sin(a) * 0.5f, -0.5f - Mathf.Cos(a) * 0.5f));
            }
            for (int i = 0; i <= capRings; i++)
            {
                float a = Mathf.PI * 0.5f * i / capRings;
                p.Add(new Vector2(Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f));
            }
            return Lathe($"Capsule_{segments}_{capRings}", p.ToArray(), segments);
        }

        /// <summary>Box of the given half-extents with rounded edges of radius r. Subdivided so the corners are smooth.</summary>
        public static Mesh RoundedBox(Vector3 size, float radius, int subdiv = 6)
        {
            string name = $"RBox_{size.x:F2}_{size.y:F2}_{size.z:F2}_{radius:F2}";
            return Get(name, () =>
            {
                Vector3 h = size * 0.5f;
                Vector3 inner = new Vector3(Mathf.Max(0, h.x - radius), Mathf.Max(0, h.y - radius), Mathf.Max(0, h.z - radius));
                var verts = new List<Vector3>();
                var uvs = new List<Vector2>();
                var tris = new List<int>();
                // six faces of a subdivided cube, each vertex pushed out to the rounded surface
                Vector3[] normals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
                int n = subdiv * 2;
                foreach (var f in normals)
                {
                    Vector3 u = f.x != 0 ? Vector3.forward : Vector3.right;
                    Vector3 v = Vector3.Cross(f, u);
                    if (f.x < 0 || f.y < 0 || f.z < 0) u = -u;
                    int start = verts.Count;
                    for (int j = 0; j <= n; j++)
                    for (int i = 0; i <= n; i++)
                    {
                        // Non-uniform spacing: denser near the edges for the bevel.
                        float su = Remap(i / (float)n), sv = Remap(j / (float)n);
                        Vector3 cube = f + u * su + v * sv;   // on the -1..1 cube
                        Vector3 p = Vector3.Scale(cube, h);
                        Vector3 c = new Vector3(Mathf.Clamp(p.x, -inner.x, inner.x), Mathf.Clamp(p.y, -inner.y, inner.y), Mathf.Clamp(p.z, -inner.z, inner.z));
                        Vector3 dir = p - c;
                        verts.Add(dir.sqrMagnitude > 1e-8f ? c + dir.normalized * radius : p);
                        uvs.Add(new Vector2(i / (float)n, j / (float)n));
                    }
                    int triStart = tris.Count;
                    for (int j = 0; j < n; j++)
                    for (int i = 0; i < n; i++)
                    {
                        int a = start + j * (n + 1) + i, b = a + 1, c = a + n + 1, d = c + 1;
                        tris.Add(a); tris.Add(b); tris.Add(c);
                        tris.Add(b); tris.Add(d); tris.Add(c);
                    }
                    // Make sure this face winds outward.
                    int mid = triStart + (n / 2 * n + n / 2) * 6;
                    Vector3 tn = Vector3.Cross(verts[tris[mid + 1]] - verts[tris[mid]], verts[tris[mid + 2]] - verts[tris[mid]]);
                    if (Vector3.Dot(tn, f) < 0f)   // in Unity, Cross(b-a, c-a) is the outward normal of a front face
                        for (int k = triStart; k < tris.Count; k += 3) (tris[k + 1], tris[k + 2]) = (tris[k + 2], tris[k + 1]);
                }
                return Build(name, verts, uvs, tris, weld: true);
            });
        }

        static float Remap(float t)
        {
            // 0..1 → -1..1, clustering samples near ±1 so the rounded edges get geometry.
            float x = t * 2f - 1f;
            return Mathf.Sign(x) * (1f - Mathf.Pow(1f - Mathf.Abs(x), 1.6f));
        }

        public static Mesh Torus(float major, float minor, int seg = 24, int side = 10, float ripple = 0f, int rippleCount = 0)
        {
            string name = $"Torus_{major:F2}_{minor:F2}_{ripple:F2}_{rippleCount}";
            return Get(name, () =>
            {
                var verts = new List<Vector3>();
                var uvs = new List<Vector2>();
                var tris = new List<int>();
                for (int i = 0; i <= seg; i++)
                {
                    float a = i / (float)seg * Mathf.PI * 2f;
                    float m = minor * (1f + ripple * Mathf.Sin(a * rippleCount));
                    Vector3 c = new Vector3(Mathf.Cos(a) * major, 0, Mathf.Sin(a) * major);
                    for (int j = 0; j <= side; j++)
                    {
                        float b = j / (float)side * Mathf.PI * 2f;
                        Vector3 outDir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                        verts.Add(c + outDir * Mathf.Cos(b) * m + Vector3.up * Mathf.Sin(b) * m);
                        uvs.Add(new Vector2(i / (float)seg, j / (float)side));
                    }
                }
                int row = side + 1;
                for (int i = 0; i < seg; i++)
                for (int j = 0; j < side; j++)
                {
                    int a = i * row + j, b = a + 1, c = a + row, d = c + 1;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(b); tris.Add(d); tris.Add(c);
                }
                return Build(name, verts, uvs, tris);
            });
        }

        // ---- internals --------------------------------------------------------------------

        static Mesh Get(string name, System.Func<Mesh> make)
        {
            if (cache.TryGetValue(name, out var m) && m) return m;
            string path = $"{Dir}/{name}.asset";
            System.IO.Directory.CreateDirectory(Dir);
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var fresh = make();
            if (existing)
            {
                EditorUtility.CopySerialized(fresh, existing);
                Object.DestroyImmediate(fresh);
                fresh = existing;
            }
            else AssetDatabase.CreateAsset(fresh, path);
            cache[name] = fresh;
            return fresh;
        }

        static Mesh Build(string name, List<Vector3> verts, List<Vector2> uvs, List<int> tris, bool weld = false)
        {
            if (weld) Weld(verts, uvs, tris);
            var mesh = new Mesh { name = name };
            if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        /// <summary>Merges coincident vertices so normals are smooth across face seams.</summary>
        static void Weld(List<Vector3> verts, List<Vector2> uvs, List<int> tris)
        {
            var map = new Dictionary<Vector3Int, int>();
            var remap = new int[verts.Count];
            var nv = new List<Vector3>();
            var nu = new List<Vector2>();
            for (int i = 0; i < verts.Count; i++)
            {
                var key = Vector3Int.RoundToInt(verts[i] * 10000f);
                if (!map.TryGetValue(key, out int idx))
                {
                    idx = nv.Count;
                    map[key] = idx;
                    nv.Add(verts[i]);
                    nu.Add(uvs[i]);
                }
                remap[i] = idx;
            }
            for (int i = 0; i < tris.Count; i++) tris[i] = remap[tris[i]];
            verts.Clear(); verts.AddRange(nv);
            uvs.Clear(); uvs.AddRange(nu);
        }
    }
}
