using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SwordPrototype.Battle;
using SwordPrototype.Presentation;

namespace SwordPrototype.Editor
{
    /// <summary>
    /// R6: Play 모드에서 실제로 움직이며 겨눔 자세·하체 방향·베임 반응을 찍는다(IK·위 층은 Play 중에만 보임).
    /// 그래픽 장치가 필요하므로 -nographics 없이 배치 실행. 결과: html/r6-*.png, 로그 R6_PREVIEW_OK.
    /// 플레이어 입력·전투 흐름·카메라 스크립트는 끄고, 루트를 직접 움직인다.
    /// </summary>
    public static class R6Preview
    {
        private const string Key = "Sword.R6PreviewPending";
        private static double started;
        private static int step;
        private static Vector3 origin;
        private static bool errors;

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
            step = 0;
            started = -1;
            Application.logMessageReceived += (m, s, t) => { if (t == LogType.Exception) { errors = true; Debug.Log("R6_PREVIEW_EXCEPTION " + m); } };
            EditorApplication.update += Tick;
        }

        private static Vector3 SwordUp(GameObject character)
        {
            foreach (var tr in character.GetComponentsInChildren<Transform>()) if (tr.name == "SwordSocket") return tr.up;
            return Vector3.zero;
        }

        // 가슴 뼈 위쪽 축이 수직에서 기운 각도(움찔 확인용)
        private static float ChestTilt(AnimatorRig rig)
        {
            var a = rig.GetComponent<Animator>();
            var c = a.GetBoneTransform(HumanBodyBones.UpperChest) ?? a.GetBoneTransform(HumanBodyBones.Chest);
            var h = a.GetBoneTransform(HumanBodyBones.Hips);
            return Quaternion.Angle(c.rotation, h.rotation);   // 골반 대비 가슴 회전량
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            var player = GameObject.Find("Player");
            var enemy = GameObject.Find("Enemy_StationaryPlaceholder");
            if (player == null || enemy == null) return;
            var rig = player.GetComponentInChildren<AnimatorRig>();
            var enemyRig = enemy.GetComponentInChildren<AnimatorRig>();
            var cam = Camera.main;
            if (started < 0)
            {
                started = EditorApplication.timeSinceStartup;
                foreach (var b in new Behaviour[] { player.GetComponent<PlayerMovement>(), player.GetComponent<PlayerDash>(), player.GetComponent<PlayerJump>(),
                    Object.FindFirstObjectByType<BattleFlow>(), cam.GetComponent<ThirdPersonCamera>(), enemy.GetComponent<Enemy.EnemyBrain>() })
                    if (b != null) b.enabled = false;
                origin = new Vector3(0f, 0f, -7f);
                player.transform.SetPositionAndRotation(origin, Quaternion.identity);   // 적(원점)을 바라봄
                enemy.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
            float t = (float)(EditorApplication.timeSinceStartup - started);
            Vector3 head = Vector3.up * 1.1f;

            // 1) 앞으로 달리기 + 하단(↓) — 옆에서
            if (t < 2.2f)
            {
                rig.HoldStance(AttackDirection.Down);
                player.transform.position = origin + Vector3.forward * Mathf.Repeat(t * 4f, 3f);
                if (step == 0 && t > 2.0f) { step++; ScenePreview.Shot(cam, player.transform.position + new Vector3(3.6f, 1.3f, 0.6f), player.transform.position + head, "../html/r6-run-low.png"); }
                return;
            }
            // 2) 왼쪽 옆걸음 + 중단(→) — 앞 비스듬히
            if (t < 4.4f)
            {
                rig.HoldStance(AttackDirection.Right);
                player.transform.position = origin + Vector3.left * Mathf.Repeat((t - 2.2f) * 3f, 3f);
                if (step == 1 && t > 4.2f) {
                    var f = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                    object Get(string n) => typeof(AnimatorRig).GetField(n, f).GetValue(rig);
                    Debug.Log($"R6_DIAG strafe sword.up={SwordUp(player)} rootFwd={player.transform.forward} modelYaw={rig.transform.localEulerAngles.y:0} speed={Get("speed")} moveX={Get("moveX")} moveZ={Get("moveZ")} cue={Get("cueTimer")} dt={Time.deltaTime:0.000} frame={Time.frameCount}");
                }
                if (step == 1 && t > 4.2f) { step++; ScenePreview.Shot(cam, player.transform.position + new Vector3(-1.2f, 1.5f, 3.6f), player.transform.position + head, "../html/r6-strafe-mid.png"); }
                return;
            }
            // 3) 뒷걸음 + 상단(↑) — 옆에서
            if (t < 6.6f)
            {
                rig.HoldStance(AttackDirection.Up);
                player.transform.position = origin + Vector3.back * Mathf.Repeat((t - 4.4f) * 2.5f, 3f);
                if (step == 2 && t > 6.4f) { step++; ScenePreview.Shot(cam, player.transform.position + new Vector3(3.6f, 1.4f, 0.4f), player.transform.position + head, "../html/r6-back-high.png"); }
                return;
            }
            // 4) 적 베임: 스침(→ 방향) 직후 · 베임(↘ 방향) 직후
            Vector3 eye = enemy.transform.position + new Vector3(-3.6f, 1.7f, -2.4f), look = enemy.transform.position + Vector3.up * 1.4f;
            if (step == 3) { Debug.Log($"R6_DIAG before chestTilt={ChestTilt(enemyRig):0.0}"); step++; enemyRig.Wound(Vector3.right, 0); }
            if (step == 4 && t > 6.63f) Debug.Log($"R6_DIAG graze chestTilt={ChestTilt(enemyRig):0.0}");
            if (step == 4 && t > 6.63f) { step++; ScenePreview.Shot(cam, eye, look, "../html/r6-wound-graze.png"); }
            if (step == 5 && t > 8.2f) { step++; enemyRig.Wound((Vector3.right + Vector3.down).normalized, 1); }
            if (step == 6 && t > 8.23f) Debug.Log($"R6_DIAG cut chestTilt={ChestTilt(enemyRig):0.0}");
            if (step == 6 && t > 8.23f) { step++; ScenePreview.Shot(cam, eye, look, "../html/r6-wound-cut.png"); }
            // 5) R7 얼굴 가까이(플레이어·적)
            if (step == 7 && t > 8.6f)
            {
                step++;
                Transform Head(GameObject g) => g.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head);
                var ph = Head(player); var eh = Head(enemy);
                ScenePreview.Shot(cam, ph.position + player.transform.forward * 0.9f + Vector3.up * 0.05f + player.transform.right * 0.25f, ph.position + Vector3.up * 0.08f, "../html/r7-face-player.png");
                ScenePreview.Shot(cam, eh.position + enemy.transform.forward * 1.1f + Vector3.up * 0.05f + enemy.transform.right * 0.3f, eh.position + Vector3.up * 0.08f, "../html/r7-face-enemy.png");
                foreach (var tr in eh.GetComponentsInChildren<Transform>()) if (tr.name.StartsWith("Hair")) Debug.Log($"R7_DIAG enemy {tr.name} pos={tr.position} headPos={eh.position} lossy={tr.lossyScale} renderer={(tr.GetComponent<Renderer>() ? tr.GetComponent<Renderer>().bounds.ToString() : "-")}");
            }
            // 6) R8 KayKit 적 공격 동작(동작이 팔·검을 직접 움직임)
            Vector3 side = enemy.transform.position + enemy.transform.right * 4.2f + Vector3.up * 1.6f + enemy.transform.forward * 1.2f;
            if (step == 8 && t > 8.8f) { step++; enemyRig.Cue(FighterCue.Slice); }
            if (step == 9 && t > 9.05f) { step++; ScenePreview.Shot(cam, side, enemy.transform.position + Vector3.up * 1.3f, "../html/r8-enemy-slice.png"); }
            if (step == 10 && t > 9.6f) { step++; enemyRig.Cue(FighterCue.SpinAttack); }
            if (step == 11 && t > 9.95f) { step++; ScenePreview.Shot(cam, side, enemy.transform.position + Vector3.up * 1.3f, "../html/r8-enemy-spin.png"); }
            if (step == 12 && t > 10.6f) { step++; enemyRig.Cue(FighterCue.Kick); }
            if (step == 13 && t > 10.85f) { step++; ScenePreview.Shot(cam, side, enemy.transform.position + Vector3.up * 1.1f, "../html/r8-enemy-kick.png"); }
            if (step == 14 && t > 11.4f) { step++; rig.Cue(FighterCue.BlockHit); rig.SetBlade(FighterRig.BladeDirection(new Vector2(0.7f, 0.7f), 0.9f), 0.5f); }
            if (step == 15 && t > 11.55f) { step++; ScenePreview.Shot(cam, player.transform.position + new Vector3(2.6f, 1.5f, 2.2f), player.transform.position + Vector3.up * 1.2f, "../html/r8-player-block.png"); }
            if (step == 16 && t > 12f)
            {
                SessionState.SetBool(Key, false);
                EditorApplication.update -= Tick;
                Debug.Log(errors ? "R6_PREVIEW_DONE_WITH_EXCEPTIONS" : "R6_PREVIEW_OK");
                EditorApplication.Exit(0);
            }
        }
    }
}
