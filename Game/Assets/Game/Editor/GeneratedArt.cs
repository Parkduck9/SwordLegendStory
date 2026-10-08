using System.IO;
using UnityEditor;
using UnityEngine;

namespace SwordPrototype.Editor
{
    /// <summary>
    /// S8-1: 외부 에셋 없이 코드로 만드는 수묵풍 질감(바닥 석판·돌담·나무)과 먼 산 메시.
    /// 결과는 Assets/Game/Art/Generated에 저장해 장면이 참조한다. 같은 시드라 매번 같은 결과.
    /// </summary>
    public static class GeneratedArt
    {
        public const string Folder = "Assets/Game/Art/Generated";

        public static Texture2D FloorSlabs() => Save("ArenaFloor", 512, (x, y) =>
        {
            const int slabs = 4;
            float u = x / 512f * slabs, v = y / 512f * slabs;
            // 줄마다 반 칸 어긋난 석판
            float row = Mathf.Floor(v);
            float uu = u + (row % 2 == 0 ? 0f : 0.5f);
            float fu = uu - Mathf.Floor(uu), fv = v - row;
            float mortar = Mathf.Min(Mathf.Min(fu, 1f - fu), Mathf.Min(fv, 1f - fv));
            float slabTone = Hash(Mathf.Floor(uu), row) * 0.08f;
            float grain = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.08f + Mathf.PerlinNoise(x * 0.21f, y * 0.21f) * 0.04f;
            // 먹이 번진 얼룩
            float stain = Mathf.Clamp01(Mathf.PerlinNoise(x * 0.008f + 11f, y * 0.008f + 7f) - 0.62f) * 1.4f;
            float value = 0.70f - slabTone - grain - stain * 0.35f;
            if (mortar < 0.018f) value -= 0.22f;
            return Tint(value, 0.98f, 0.96f, 0.90f);
        });

        public static Texture2D StoneWall() => Save("StoneWall", 256, (x, y) =>
        {
            float v = y / 256f * 4f, row = Mathf.Floor(v);
            float u = x / 256f * 3f + Hash(row, 3f) * 0.7f;
            float fu = u - Mathf.Floor(u), fv = v - row;
            float gap = Mathf.Min(Mathf.Min(fu, 1f - fu) * 1.5f, Mathf.Min(fv, 1f - fv));
            float value = 0.48f - Hash(Mathf.Floor(u), row) * 0.12f - Mathf.PerlinNoise(x * 0.12f, y * 0.12f) * 0.08f;
            if (gap < 0.04f) value -= 0.2f;
            return Tint(value, 0.97f, 0.97f, 0.95f);
        });

        public static Texture2D Wood() => Save("Wood", 256, (x, y) =>
        {
            float plank = Mathf.Floor(x / 256f * 5f);
            float fx = x / 256f * 5f - plank;
            float grainLine = Mathf.PerlinNoise(x * 0.4f, y * 0.02f + plank * 10f);
            float value = 0.30f + Hash(plank, 1f) * 0.06f - grainLine * 0.08f;
            if (fx < 0.04f || fx > 0.96f) value -= 0.12f;
            return new Color(value * 1.15f, value * 0.9f, value * 0.7f);
        });

        /// <summary>전투장 둘레의 먼 산 실루엣 고리. 아래가 땅 밑까지 내려가 틈이 보이지 않는다.</summary>
        public static Mesh MountainRing(string name, float radius, float minHeight, float maxHeight, float seed)
        {
            string path = $"{Folder}/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            const int segments = 120;
            var vertices = new Vector3[(segments + 1) * 2];
            var triangles = new int[segments * 6];
            for (int i = 0; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                // 원 둘레를 따라 노이즈를 샘플해 고리 끝(0°=360°)이 이어지게 한다.
                float cx = Mathf.Cos(a), cy = Mathf.Sin(a);
                float n = Mathf.PerlinNoise(cx * 2.2f + seed, cy * 2.2f + seed) * 0.7f + Mathf.PerlinNoise(cx * 6f + seed * 2f, cy * 6f + 1f) * 0.3f;
                float h = Mathf.Lerp(minHeight, maxHeight, n * n);
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                vertices[i * 2] = dir * radius + Vector3.down * 4f;
                vertices[i * 2 + 1] = dir * radius + Vector3.up * h;
            }
            for (int i = 0; i < segments; i++)
            {
                int b = i * 2, t = i * 6;
                triangles[t] = b; triangles[t + 1] = b + 1; triangles[t + 2] = b + 2;
                triangles[t + 3] = b + 2; triangles[t + 4] = b + 1; triangles[t + 5] = b + 3;
            }
            var mesh = new Mesh { name = name, vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Directory.CreateDirectory(Folder);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        /// <summary>S8-2: 원뿔대 메시(삿갓·도포 자락). 바닥이 y=0, 위가 y=height. 위·아래 뚜껑 포함.</summary>
        public static Mesh Frustum(string name, float bottomRadius, float topRadius, float height, int segments = 24)
        {
            string path = $"{Folder}/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            var v = new System.Collections.Generic.List<Vector3>();
            var t = new System.Collections.Generic.List<int>();
            for (int i = 0; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                v.Add(d * bottomRadius);
                v.Add(d * topRadius + Vector3.up * height);
            }
            for (int i = 0; i < segments; i++)
            {
                int b = i * 2;
                t.AddRange(new[] { b, b + 1, b + 2, b + 2, b + 1, b + 3 });
            }
            int bottomCenter = v.Count; v.Add(Vector3.zero);
            int topCenter = v.Count; v.Add(Vector3.up * height);
            for (int i = 0; i < segments; i++)
            {
                t.AddRange(new[] { bottomCenter, i * 2 + 2, i * 2 });
                t.AddRange(new[] { topCenter, i * 2 + 1, i * 2 + 3 });
            }
            var mesh = new Mesh { name = name, vertices = v.ToArray(), triangles = t.ToArray() };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Directory.CreateDirectory(Folder);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Texture2D Save(string name, int size, System.Func<int, int, Color> pixel)
        {
            Directory.CreateDirectory(Folder);
            var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, pixel(x, y));
            tex.Apply();
            string path = $"{Folder}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Color Tint(float value, float r, float g, float b) => new Color(value * r, value * g, value * b);

        private static float Hash(float a, float b)
        {
            float h = Mathf.Sin(a * 127.1f + b * 311.7f) * 43758.5453f;
            return h - Mathf.Floor(h);
        }
    }
}
