using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SwordPrototype.Editor
{
    /// <summary>
    /// R7: 기본 모델(Quaternius Universal Base Characters, CC0)을 무협 인물로 입힌다.
    /// - 몸 메시를 "옷"(몸통·팔·다리·발)과 "피부"(머리·목·손)로 나눠 옷은 먹빛 무복, 피부는 수묵 톤 질감
    /// - 질감은 원본 명암을 살리고 채도를 낮춘 수묵 톤 사본(Generated/)을 쓴다
    /// - 머리 모양(원점 기준 메시)은 머리뼈에 붙인다
    /// </summary>
    public static class BaseCharacterDress
    {
        private const string Generated = "Assets/Game/Art/Generated";
        public const string SplitMeshPath = Generated + "/UBC_Male_Split.asset";
        private static Material skin, eyes, hair1;
        private static Mesh split, collarMesh, cuffMesh;
        private static float neckRadius = 0.08f, neckBaseRadius = 0.12f, wristRadius = 0.045f;   // Split에서 실측(배율 1 기준)

        /// <summary>장면 생성마다 한 번: 수묵 톤 질감·재질·나눈 메시를 만든다.</summary>
        public static void Prepare()
        {
            string tex = BaseCharacterImport.Folder + "/Textures/";
            skin = Lit("UBC_Skin", InkTone(tex + "T_Superhero_Male_Ligh.png", "UBC_Skin_Ink", 0.22f, new Color(1f, 0.93f, 0.84f), 0.92f), tex + "T_Superhero_Male_Normal.png", 0.15f, false);
            eyes = Lit("UBC_Eyes", InkTone(tex + "T_Eye_Brown.png", "UBC_Eye_Ink", 0.15f, Color.white, 0.8f), tex + "T_Eye_Normal.png", 0.5f, false);
            hair1 = Lit("UBC_Hair1", InkTone(tex + "T_Hair_1_BaseColor.png", "UBC_Hair1_Ink", 0f, Color.white, 0.32f), tex + "T_Hair_1_Normal.png", 0.2f, true);
            split = collarMesh = cuffMesh = null;
        }

        /// <summary>모델 인스턴스에 옷·피부 재질과 머리 모양을 입힌다. garment는 무복 재질(먹빛).</summary>
        public static void Dress(GameObject model, Animator animator, Material garment, string[] hairStyles)
        {
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (r.name == "Eyes") { r.sharedMaterial = eyes; continue; }
                if (r.name == "Eyebrows") { r.sharedMaterial = hair1; continue; }
                if (split == null) split = Split(r, animator);
                r.sharedMesh = split;
                r.sharedMaterials = new[] { garment, skin };
            }
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            foreach (string style in hairStyles)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{BaseCharacterImport.Folder}/Hair/{style}.fbx");
                if (prefab == null || head == null) continue;
                // 원점 기준 메시라 모델 루트 아래에 원래 값 그대로 두면 제자리(파일마다 회전·배율이 들어 있어 덮어쓰지 않음).
                // 그 상태로 머리뼈 자식이 되어 함께 움직인다.
                var h = (GameObject)Object.Instantiate(prefab, model.transform, false);
                h.name = style;
                foreach (var mr in h.GetComponentsInChildren<MeshRenderer>())
                {
                    mr.sharedMaterial = hair1;   // 남자 머리·수염·눈썹은 모두 Hair_1 질감(glTF 재질 정보)
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                }
                h.transform.SetParent(head, true);
            }
        }

        /// <summary>옷과 피부 경계(목·손목)를 가리는 깃·소매 끝동. 메시는 CharacterBuilder가 만든 원뿔대.</summary>
        public static void Trim(Animator animator, Material material, float scale)
        {
            // 실측 굵기보다 조금 크게: 깃은 목 아래(어깨 쪽)를 넓게 덮고 위는 목을 감싼다
            if (collarMesh == null) collarMesh = GeneratedArt.Frustum("CollarMesh", neckBaseRadius * 1.08f, neckRadius * 1.12f, 0.08f);
            if (cuffMesh == null) cuffMesh = GeneratedArt.Frustum("CuffMesh", wristRadius * 1.35f, wristRadius * 1.2f, 0.09f);
            Mesh collar = collarMesh, cuff = cuffMesh;
            Transform neck = animator.GetBoneTransform(HumanBodyBones.Neck);
            if (neck != null)
            {
                // 깃 아래가 목 아래 고리(경계 6cm 아래), 위가 경계 2cm 위
                Vector3 at = new Vector3(neck.position.x, NeckCutY(animator) - 0.06f * scale, neck.position.z);
                Place("Collar", collar, material, neck, at, scale);
            }
            foreach (bool left in new[] { true, false })
            {
                WristCut(animator, left, out Vector3 point, out Vector3 dir);
                Transform lower = animator.GetBoneTransform(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
                // 넓은 쪽(바닥)이 손목 경계를 살짝 넘어서 덮고, 팔꿈치 쪽으로 뻗는다
                var go = Place("Cuff", cuff, material, lower, point + dir * 0.012f * scale, scale);
                go.transform.rotation = Quaternion.FromToRotation(Vector3.up, -dir);
            }
        }

        private static GameObject Place(string name, Mesh mesh, Material material, Transform bone, Vector3 position, float scale)
        {
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            go.transform.SetPositionAndRotation(position, Quaternion.identity);
            go.transform.localScale = Vector3.one * scale;
            go.transform.SetParent(bone, true);
            return go;
        }

        // 옷/피부 경계: 목은 목뼈~머리뼈 사이 45% 높이의 수평 고리, 손목은 손목 관절 바로 앞의 고리(깃·끝동이 정확히 덮는 자리)
        public static float NeckCutY(Animator a) => Mathf.Lerp(a.GetBoneTransform(HumanBodyBones.Neck).position.y, a.GetBoneTransform(HumanBodyBones.Head).position.y, 0.45f);
        public static void WristCut(Animator a, bool left, out Vector3 point, out Vector3 dir)
        {
            Transform lower = a.GetBoneTransform(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
            Transform hand = a.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            dir = (hand.position - lower.position).normalized;
            point = hand.position - dir * 0.02f * a.transform.lossyScale.y;
        }

        /// <summary>
        /// 몸 메시를 둘로 나눈다: 0 = 옷(몸통·팔·다리·발), 1 = 피부(목 위쪽 머리, 손목 앞 손).
        /// 기본 자세의 정점 위치로 목·손목 고리에서 자르므로 경계가 깔끔하다(삼각형 꼭짓점 둘 이상이 피부 쪽이면 피부).
        /// </summary>
        public static Mesh Split(SkinnedMeshRenderer r, Animator a)
        {
            AssetDatabase.DeleteAsset(SplitMeshPath);
            var src = r.sharedMesh;
            var mesh = Object.Instantiate(src);
            mesh.name = "UBC_Male_Split";
            var baked = new Mesh();
            r.BakeMesh(baked, true);   // 지금(기본 자세) 정점, 렌더러 기준 + 배율 포함
            var toWorld = Matrix4x4.TRS(r.transform.position, r.transform.rotation, Vector3.one);
            Vector3[] world = baked.vertices.Select(v => toWorld.MultiplyPoint3x4(v)).ToArray();
            Object.DestroyImmediate(baked);
            float neckY = NeckCutY(a);
            Vector3 neckAxis = a.GetBoneTransform(HumanBodyBones.Neck).position;
            WristCut(a, true, out Vector3 lp, out Vector3 ld);
            WristCut(a, false, out Vector3 rp, out Vector3 rd);
            float s = a.transform.lossyScale.y;
            bool SkinVertex(int i)
            {
                Vector3 p = world[i];
                bool head = p.y > neckY && new Vector2(p.x - neckAxis.x, p.z - neckAxis.z).magnitude < 0.18f * s;
                return head || Vector3.Dot(p - lp, ld) > 0f || Vector3.Dot(p - rp, rd) > 0f;
            }
            int[] tris = src.triangles;
            var cloth = new List<int>(tris.Length);
            var bare = new List<int>(tris.Length / 4);
            for (int t = 0; t < tris.Length; t += 3)
            {
                int skinCount = 0;
                for (int k = 0; k < 3; k++) if (SkinVertex(tris[t + k])) skinCount++;
                var list = skinCount >= 2 ? bare : cloth;
                list.Add(tris[t]); list.Add(tris[t + 1]); list.Add(tris[t + 2]);
            }
            // 깃·끝동 크기: 경계 고리 근처 정점의 실제 굵기(배율 1 기준으로 저장)
            float Ring(System.Func<Vector3, float> along, System.Func<Vector3, float> radius, float band)
            {
                float r0 = 0f;
                foreach (var p in world) if (Mathf.Abs(along(p)) < band * s) r0 = Mathf.Max(r0, radius(p));
                return r0 / s;
            }
            neckRadius = Ring(p => p.y - neckY, p => new Vector2(p.x - neckAxis.x, p.z - neckAxis.z).magnitude < 0.14f * s ? new Vector2(p.x - neckAxis.x, p.z - neckAxis.z).magnitude : 0f, 0.012f);
            neckBaseRadius = Ring(p => p.y - (neckY - 0.06f * s), p => new Vector2(p.x - neckAxis.x, p.z - neckAxis.z).magnitude < 0.14f * s ? new Vector2(p.x - neckAxis.x, p.z - neckAxis.z).magnitude : 0f, 0.012f);
            wristRadius = Ring(p => Vector3.Dot(p - rp, rd), p => Vector3.ProjectOnPlane(p - rp, rd).magnitude < 0.15f * s ? Vector3.ProjectOnPlane(p - rp, rd).magnitude : 0f, 0.01f);
            Debug.Log($"R7_SPLIT neckY={neckY:0.000} 목 반지름={neckRadius:0.000} 목 아래={neckBaseRadius:0.000} 손목={wristRadius:0.000}");
            Loosen(mesh, src, cloth, bare);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(cloth, 0);
            mesh.SetTriangles(bare, 1);
            AssetDatabase.CreateAsset(mesh, SplitMeshPath);
            return mesh;
        }

        /// <summary>
        /// 옷 보강: 옷 부분 정점을 여러 번 고르게 다듬어(근육 굴곡 → 천의 완만한 면) 살짝 부풀린다. 발은 뭉툭한 천 신발 모양이 된다.
        /// 피부와 맞닿은 정점은 고정해 경계가 벌어지지 않는다. 질감 이음새(같은 위치 다른 정점)는 묶어서 함께 움직여 틈이 생기지 않는다.
        /// </summary>
        private static void Loosen(Mesh mesh, Mesh src, List<int> cloth, List<int> bare, int iterations = 10, float strength = 0.55f, float inflate = 0.014f)
        {
            Vector3[] v = src.vertices;
            Vector3[] n = src.normals;
            // 같은 위치 정점 묶기
            var ids = new Dictionary<Vector3Int, int>();
            int[] weld = new int[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                var key = new Vector3Int(Mathf.RoundToInt(v[i].x * 10000f), Mathf.RoundToInt(v[i].y * 10000f), Mathf.RoundToInt(v[i].z * 10000f));
                if (!ids.TryGetValue(key, out int id)) { id = ids.Count; ids[key] = id; }
                weld[i] = id;
            }
            int count = ids.Count;
            var pos = new Vector3[count];
            var nor = new Vector3[count];
            for (int i = 0; i < v.Length; i++) { pos[weld[i]] = v[i]; nor[weld[i]] += n[i]; }
            var locked = new bool[count];
            var isCloth = new bool[count];
            foreach (int i in bare) locked[weld[i]] = true;
            foreach (int i in cloth) isCloth[weld[i]] = true;
            var neighbors = new HashSet<int>[count];
            for (int i = 0; i < count; i++) neighbors[i] = new HashSet<int>();
            void Link(List<int> tris)
            {
                for (int t = 0; t < tris.Count; t += 3)
                    for (int k = 0; k < 3; k++)
                    {
                        int a = weld[tris[t + k]], b = weld[tris[t + (k + 1) % 3]];
                        neighbors[a].Add(b); neighbors[b].Add(a);
                    }
            }
            Link(cloth); Link(bare);
            for (int it = 0; it < iterations; it++)
            {
                var next = (Vector3[])pos.Clone();
                for (int i = 0; i < count; i++)
                {
                    if (!isCloth[i] || locked[i] || neighbors[i].Count == 0) continue;
                    Vector3 avg = Vector3.zero;
                    foreach (int j in neighbors[i]) avg += pos[j];
                    next[i] = Vector3.Lerp(pos[i], avg / neighbors[i].Count, strength);
                }
                pos = next;
            }
            for (int i = 0; i < count; i++)
                if (isCloth[i] && !locked[i]) pos[i] += nor[i].normalized * inflate;
            var outV = new Vector3[v.Length];
            for (int i = 0; i < v.Length; i++) outV[i] = pos[weld[i]];
            mesh.vertices = outV;
            mesh.RecalculateNormals();
            // 피부 쪽 정점은 원래 법선(얼굴·손 음영 유지)
            var normals = mesh.normals;
            for (int i = 0; i < v.Length; i++) if (locked[weld[i]] || !isCloth[weld[i]]) normals[i] = n[i];
            mesh.normals = normals;
            mesh.RecalculateBounds();
        }

        private static Material Lit(string name, Texture2D baseMap, string normalPath, float smoothness, bool doubleSided)
        {
            string path = $"Assets/Game/Art/{name}.mat";
            AssetDatabase.DeleteAsset(path);
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetTexture("_BaseMap", baseMap);
            m.color = Color.white;
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (normal != null) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
            m.SetFloat("_Smoothness", smoothness);
            if (doubleSided) m.SetFloat("_Cull", 0f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// <summary>원본 명암은 살리고 채도를 낮춰 종이빛으로 물들인 사본(PNG)을 만든다.</summary>
        public static Texture2D InkTone(string sourcePath, string outName, float saturation, Color tint, float brightness)
        {
            var src = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
            if (src == null) return null;
            Color[] px = src.GetPixels();
            for (int i = 0; i < px.Length; i++)
            {
                Color c = px[i];
                float g = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
                Color mixed = Color.Lerp(new Color(g, g, g), c, saturation) * brightness;
                px[i] = new Color(mixed.r * tint.r, mixed.g * tint.g, mixed.b * tint.b, c.a);
            }
            var outTex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, true);
            outTex.SetPixels(px);
            outTex.Apply();
            string path = $"{Generated}/{outName}.png";
            File.WriteAllBytes(path, outTex.EncodeToPNG());
            Object.DestroyImmediate(outTex);
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.maxTextureSize = 2048;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
