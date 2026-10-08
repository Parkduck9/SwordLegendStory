using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SwordPrototype.Editor
{
    /// <summary>
    /// R8: KayKit Character Animations(CC0) 사람형 중간 체형 동작을 Humanoid로 가져와 목록을 md/kaykit-clips.txt에 남긴다.
    /// 회전은 몸 방향 기준, 제자리(루트 이동·회전 고정) — UAL과 같은 설정.
    /// </summary>
    public static class KayKitImport
    {
        public const string Folder = "Assets/Game/Art/KayKit";
        public static readonly string[] Files = { "Rig_Medium_CombatMelee", "Rig_Medium_General", "Rig_Medium_MovementAdvanced", "Rig_Medium_MovementBasic" };
        public static bool Available => File.Exists($"{Folder}/{Files[0]}.fbx");

        public static void ConfigureAndReport()
        {
            var report = new StringBuilder();
            foreach (string f in Files)
            {
                string path = $"{Folder}/{f}.fbx";
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                var clips = importer.defaultClipAnimations;
                foreach (var c in clips)
                {
                    c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = true;
                    c.keepOriginalOrientation = false; c.keepOriginalPositionY = true; c.keepOriginalPositionXZ = true;
                    string n = c.name.ToLowerInvariant();
                    c.loopTime = n.Contains("idle") || n.Contains("loop") || n.Contains("walk") || n.Contains("run") || n.Contains("strafe");
                }
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
                var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
                report.AppendLine($"== {f}: avatar valid={avatar != null && avatar.isValid} human={avatar != null && avatar.isHuman}");
                foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).OrderBy(c => c.name))
                    report.AppendLine($"clip: {clip.name}\t{clip.length:0.00}s");
            }
            File.WriteAllText("../md/kaykit-clips.txt", report.ToString());
            Debug.Log("KAYKIT_REPORT_OK");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        public static AnimationClip Clip(string name)
        {
            foreach (string f in Files)
            {
                var c = AssetDatabase.LoadAllAssetsAtPath($"{Folder}/{f}.fbx").OfType<AnimationClip>().FirstOrDefault(x => x.name == name);
                if (c != null) return c;
            }
            return null;
        }
    }
}
