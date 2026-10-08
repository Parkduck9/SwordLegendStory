using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SwordPrototype.Editor
{
    /// <summary>
    /// R7: Quaternius Universal Base Characters(CC0) 남자 기본 모델·머리를 가져오고 구성을 md/ubc-report.txt에 남긴다.
    /// 몸은 Humanoid(동작 없음 — 동작은 UAL1·UAL2에서 재타깃), 머리 모양은 원점 기준 정적 메시.
    /// </summary>
    public static class BaseCharacterImport
    {
        public const string Folder = "Assets/Game/Art/Quaternius/UBC";
        public const string BodyPath = Folder + "/Superhero_Male_FullBody.fbx";
        // 남자 머리 높이에 맞춘 것만(올림머리·긴 머리는 여자 모델용이라 얼굴을 덮음)
        public static readonly string[] Hair = { "Hair_SimpleParted", "Hair_Buzzed", "Hair_Beard", "Eyebrows_Regular" };

        public static bool Available => File.Exists(BodyPath);

        public static void Configure()
        {
            var body = (ModelImporter)AssetImporter.GetAtPath(BodyPath);
            body.animationType = ModelImporterAnimationType.Human;
            body.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            body.importAnimation = false;
            body.materialImportMode = ModelImporterMaterialImportMode.None;
            body.isReadable = true;   // 피부/옷 부분 나누기에 정점이 필요
            body.SaveAndReimport();
            foreach (string h in Hair)
            {
                var hair = (ModelImporter)AssetImporter.GetAtPath($"{Folder}/Hair/{h}.fbx");
                hair.animationType = ModelImporterAnimationType.None;
                hair.importAnimation = false;
                hair.materialImportMode = ModelImporterMaterialImportMode.None;
                hair.SaveAndReimport();
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Folder + "/Textures" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                bool normal = path.Contains("_Normal");
                ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.sRGBTexture = !normal && !path.Contains("Roughness");
                ti.isReadable = !normal;   // 수묵 색으로 바꾼 사본을 만든다
                if (!normal) ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.maxTextureSize = 2048;
                ti.SaveAndReimport();
            }
        }

        public static void ConfigureAndReport()
        {
            Configure();
            var report = new StringBuilder();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BodyPath);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(BodyPath).OfType<Avatar>().FirstOrDefault();
            report.AppendLine($"avatar valid={avatar != null && avatar.isValid} human={avatar != null && avatar.isHuman}");
            foreach (var r in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var m = r.sharedMesh;
                report.AppendLine($"skinned {r.name}: verts={m.vertexCount} submeshes={m.subMeshCount} bounds={m.bounds} bones={r.bones.Length} rootBone={(r.rootBone ? r.rootBone.name : "-")}");
                for (int i = 0; i < m.subMeshCount; i++) report.AppendLine($"  sub{i}: tris={m.GetSubMesh(i).indexCount / 3}");
                report.AppendLine("  bones: " + string.Join(",", r.bones.Select(b => b.name)));
            }
            foreach (var t in prefab.GetComponentsInChildren<Transform>(true).Take(6)) report.AppendLine($"node {t.name} pos={t.localPosition} rot={t.localEulerAngles} scale={t.localScale}");
            foreach (string h in Hair)
            {
                var hp = AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/Hair/{h}.fbx");
                foreach (var mf in hp.GetComponentsInChildren<MeshFilter>(true))
                    report.AppendLine($"hair {h}/{mf.name}: verts={mf.sharedMesh.vertexCount} bounds={mf.sharedMesh.bounds} local={mf.transform.localPosition} rot={mf.transform.localEulerAngles} scale={mf.transform.lossyScale}");
                foreach (var sr in hp.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    report.AppendLine($"hair {h}/{sr.name}: SKINNED verts={sr.sharedMesh.vertexCount}");
            }
            // 기본 자세에서 키(정점 경계)
            var inst = (GameObject)Object.Instantiate(prefab);
            var smr = inst.GetComponentInChildren<SkinnedMeshRenderer>();
            var baked = new Mesh(); smr.BakeMesh(baked);
            var b = baked.bounds;
            report.AppendLine($"baked bounds center={b.center} size={b.size} (renderer world bounds {smr.bounds.size})");
            var anim = inst.GetComponent<Animator>();
            if (anim != null && anim.isHuman)
                foreach (var bone in new[] { HumanBodyBones.Head, HumanBodyBones.Hips, HumanBodyBones.RightHand, HumanBodyBones.LeftToes })
                    report.AppendLine($"bone {bone}: {(anim.GetBoneTransform(bone) ? anim.GetBoneTransform(bone).position.ToString("F3") : "없음")}");
            Object.DestroyImmediate(inst);
            File.WriteAllText("../md/ubc-report.txt", report.ToString());
            Debug.Log("UBC_REPORT_OK");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
