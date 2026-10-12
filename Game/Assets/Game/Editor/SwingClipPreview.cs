using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using SwordPrototype.Battle;
using SwordPrototype.Presentation;

namespace SwordPrototype.Editor
{
    /// <summary>
    /// R9 설계용: 플레이어 베기 후보 동작(KayKit·UAL, CC0)을 실제 플레이어 모델에 입혀
    /// 시작·중간·끝 사진(html/images/r9/clips/*.jpg)과 칼끝 궤적(md/r9-swing-paths.txt, 캐릭터 기준 x 오른쪽 · y 위)을 남긴다.
    /// Play 모드 필요(-nographics 없이 배치 실행). 로그 R9_CLIPS_OK.
    /// </summary>
    public static class SwingClipPreview
    {
        private const string Key = "Sword.R9ClipPreviewPending";
        public static readonly string[] Candidates =
        {
            "Melee_1H_Attack_Chop", "Melee_1H_Attack_Slice_Diagonal", "Melee_1H_Attack_Slice_Horizontal", "Melee_1H_Attack_Stab",
            "Melee_2H_Attack_Slice", "Melee_2H_Attack_Chop", "Melee_Dualwield_Attack_Chop", "Melee_Dualwield_Attack_Slice",
            "Melee_Block_Attack", "Sword_Attack", "Sword_Regular_A", "Sword_Regular_B", "Sword_Regular_C", "Sword_Dash",
            "Sword_Regular_Combo", "Sword_Heavy_Combo",
        };
        private static readonly float[] ShotTimes = { 0.15f, 0.4f, 0.6f, 0.85f };

        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Game/Scenes/Foundation.unity");
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (!SessionState.GetBool(Key, false)) return;
            EditorApplication.update += Tick;
        }

        private static int frames;

        private static void Tick()
        {
            if (!EditorApplication.isPlaying || ++frames < 30) return;   // 장면이 자리 잡을 때까지 잠깐 기다림
            EditorApplication.update -= Tick;
            SessionState.SetBool(Key, false);
            try { Capture(); Debug.Log("R9_CLIPS_OK"); }
            catch (System.Exception e) { Debug.Log("R9_CLIPS_FAIL " + e); }
            EditorApplication.Exit(0);
        }

        private static void Capture()
        {
            var player = GameObject.Find("Player");
            var enemy = GameObject.Find("Enemy_StationaryPlaceholder");
            foreach (var b in new Behaviour[] { player.GetComponent<PlayerMovement>(), player.GetComponent<PlayerDash>(), player.GetComponent<PlayerJump>(),
                Object.FindFirstObjectByType<BattleFlow>(), Camera.main.GetComponent<ThirdPersonCamera>(), enemy.GetComponent<Enemy.EnemyBrain>() })
                if (b != null) b.enabled = false;
            player.transform.SetPositionAndRotation(new Vector3(0f, 0f, -7f), Quaternion.identity);
            enemy.transform.position = new Vector3(0f, 0f, 30f);   // 사진에 끼지 않게 치움
            var rig = player.GetComponentInChildren<AnimatorRig>();
            rig.enabled = false;
            var animator = rig.GetComponent<Animator>();
            Transform sword = null, tip = null;
            foreach (var tr in player.GetComponentsInChildren<Transform>()) if (tr.name == "SwordSocket") sword = tr;
            if (sword != null) tip = sword.Find("Blade");
            Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            Transform index = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
            Transform little = animator.GetBoneTransform(HumanBodyBones.RightLittleProximal);
            Transform root = player.transform;
            var cam = Camera.main;
            Directory.CreateDirectory("../html/r9-clip");
            var report = new StringBuilder("# R9 후보 동작 칼끝 궤적 (캐릭터 기준 m: x 오른쪽, y 위, z 앞). t=정규화 시간\n");

            foreach (string name in Candidates)
            {
                var clip = CharacterBuilder.Clip(name);
                if (clip == null) { report.AppendLine($"== {name}: 없음"); continue; }
                var graph = PlayableGraph.Create("R9Preview");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var playable = AnimationClipPlayable.Create(graph, clip);
                AnimationPlayableOutput.Create(graph, "out", animator).SetSourcePlayable(playable);
                report.AppendLine($"== {name} ({clip.length:0.00}s)");
                int shot = 0;
                int samples = Mathf.Clamp(Mathf.RoundToInt(clip.length * 20f), 20, 100);   // 긴 연속 동작은 촘촘히
                for (int i = 0; i <= samples; i++)
                {
                    float t = i / (float)samples;
                    playable.SetTime(t * clip.length);
                    graph.Evaluate(0f);
                    // AnimatorRig의 동작 구동 모드와 같게: 쥔 손(새끼 → 검지 마디) 방향이 칼날
                    sword.position = hand.position;
                    Vector3 grip = index.position - little.position;
                    if (grip.sqrMagnitude > 1e-6f) sword.rotation = Quaternion.FromToRotation(Vector3.up, grip.normalized);
                    Vector3 p = root.InverseTransformPoint(sword.position + sword.up * 1.1f);
                    report.AppendLine($"t={t:0.00} tip=({p.x:0.00},{p.y:0.00},{p.z:0.00})");
                    if (shot < ShotTimes.Length && t >= ShotTimes[shot] - 0.001f)
                    {
                        // 플레이어 뒤 왼쪽 위에서(화면 오른쪽 = 캐릭터 오른쪽)
                        Shot(cam, root.TransformPoint(new Vector3(-1.1f, 2.1f, -2.9f)), root.TransformPoint(new Vector3(0.2f, 1.2f, 0.6f)), $"../html/images/r9/clips/{name}-{shot}.jpg");
                        shot++;
                    }
                }
                graph.Destroy();
            }
            File.WriteAllText("../md/r9-swing-paths.txt", report.ToString());
        }

        private static void Shot(Camera cam, Vector3 position, Vector3 lookAt, string file)
        {
            cam.transform.position = position;
            cam.transform.LookAt(lookAt);
            var rt = new RenderTexture(480, 360, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var request = new RenderPipeline.StandardRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(cam, request)) RenderPipeline.SubmitRenderRequest(cam, request);
            else { cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; }
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.GetFullPath(file), tex.EncodeToJPG(85));
            RenderTexture.active = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
        }
    }
}
