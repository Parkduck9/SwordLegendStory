using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SwordPrototype.Editor
{
    /// <summary>
    /// S8-2: Quaternius 애니메이션 라이브러리 FBX를 Humanoid로 가져오고, 들어 있는 동작·마네킹 정보를 파일로 남긴다.
    /// </summary>
    public static class CharacterImport
    {
        public const string LibraryPath = "Assets/Game/Art/Quaternius/UAL/UAL1_Standard.fbx";

        /// <summary>R6: 두 번째 라이브러리(검 대쉬·연속 베기·넉백·스텝 등). 같은 리그.</summary>
        public const string Library2Path = "Assets/Game/Art/Quaternius/UAL2/UAL2_Standard.fbx";

        public static void ConfigureAndReport()
        {
            Configure(LibraryPath, "../md/ual-clips.txt", false);
            if (System.IO.File.Exists(Library2Path)) Configure(Library2Path, "../md/ual2-clips.txt", true);
            Debug.Log("UAL_REPORT_OK");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        // second=true면 아바타를 첫 라이브러리 것으로 공유(같은 리그) 대신 자체 생성 — Humanoid끼리는 재타깃되므로 자체 생성으로 충분
        private static void Configure(string path, string reportPath, bool second)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            // 루트 모션 없는 판이므로 모든 동작을 제자리 기준으로 굽는다.
            var clips = importer.defaultClipAnimations;
            foreach (var c in clips)
            {
                c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = true;
                // 회전은 몸 방향 기준으로 굽는다(원본 기준이면 캐릭터가 루트 뒤쪽을 봄 — Play에서 발견, 검사로 고정).
                c.keepOriginalOrientation = false; c.keepOriginalPositionY = true; c.keepOriginalPositionXZ = true;
                c.loopTime = Looping(c.name);
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();

            var report = new StringBuilder();
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            var avatar = assets.OfType<Avatar>().FirstOrDefault();
            report.AppendLine($"avatar: {(avatar != null ? avatar.name : "없음")} valid={avatar != null && avatar.isValid} human={avatar != null && avatar.isHuman}");
            foreach (var m in assets.OfType<Mesh>()) report.AppendLine($"mesh: {m.name} verts={m.vertexCount}");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var r in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                report.AppendLine($"skinned: {r.name} bounds={r.bounds.size}");
            foreach (var clip in assets.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).OrderBy(c => c.name))
                report.AppendLine($"clip: {clip.name}\t{clip.length:0.00}s");
            File.WriteAllText(reportPath, report.ToString());
        }

        // 이동·대기·힘겨루기처럼 계속 반복되는 동작
        private static bool Looping(string name)
        {
            string n = name.ToLowerInvariant();
            return n.Contains("idle") || n.Contains("walk") || n.Contains("run") || n.Contains("jog") || n.Contains("sprint") || n.Contains("loop");
        }
    }
}
