using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SwordPrototype.Editor
{
    /// <summary>
    /// S8-1: 전투장 미리보기 이미지를 html 폴더에 저장한다(그래픽 장치가 필요하므로 -nographics 없이 배치 실행).
    /// 전경(입구 쪽 높은 곳)과 플레이어 뒤 시점 두 장.
    /// </summary>
    public static class ScenePreview
    {
        /// <summary>S8-2: 캐릭터 자세 미리보기(대기, 베기 동작 중간, 적 대도 예고). 애니메이터를 직접 진행시켜 찍는다.</summary>
        public static void CaptureS82()
        {
            EditorSceneManager.OpenScene("Assets/Game/Scenes/Foundation.unity");
            var cam = Camera.main;
            var player = GameObject.Find("Player");
            var enemy = GameObject.Find("Enemy_StationaryPlaceholder");
            // 두 캐릭터를 가까이 세워 함께 찍는다.
            player.transform.SetPositionAndRotation(new Vector3(-1.4f, 0, -2f), Quaternion.Euler(0, 35, 0));
            enemy.transform.SetPositionAndRotation(new Vector3(1.4f, 0, 0.5f), Quaternion.Euler(0, 215, 0));
            Pose(player, "Locomotion", 0.3f);
            Pose(enemy, "Guard", 0.3f);
            // 장면을 연 직후 첫 렌더는 질감이 덜 올라와 있을 수 있어 한 번 버린다.
            Shot(cam, new Vector3(0, 1.7f, -7.5f), new Vector3(0, 1.1f, -0.6f), "../html/s8-2-preview-idle.png");
            Shot(cam, new Vector3(0, 1.7f, -7.5f), new Vector3(0, 1.1f, -0.6f), "../html/s8-2-preview-idle.png");
            Pose(player, "Swing", 0.45f);
            Pose(enemy, "Hit", 0.2f);
            Shot(cam, new Vector3(0, 1.7f, -7.5f), new Vector3(0, 1.1f, -0.6f), "../html/s8-2-preview-swing.png");
            Debug.Log("S82_PREVIEW_OK");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        // 편집 모드에서 애니메이터를 특정 상태로 진행시켜 자세를 만든다(IK·검 위치는 플레이 중에만 반영).
        private static void Pose(GameObject character, string state, float seconds)
        {
            var animator = character.GetComponentInChildren<Animator>();
            if (animator == null) return;
            animator.Rebind();
            animator.Play(state, 0, 0f);
            animator.Update(0f);
            for (int i = 0; i < 10; i++) animator.Update(seconds / 10f);
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var sword = character.GetComponentsInChildren<Transform>(true);
            foreach (var t in sword)
                if (t.name == "SwordSocket" && hand != null) { t.position = hand.position; t.rotation = Quaternion.LookRotation(character.transform.forward, Vector3.up) * Quaternion.Euler(60, 0, 0); }
        }

        /// <summary>
        /// S8-3: UI 미리보기. 오버레이 캔버스는 카메라 렌더에 안 잡히므로 잠시 카메라 공간 캔버스로 바꿔 찍는다(저장하지 않음).
        /// 편집 모드에서는 Awake/Start가 돌지 않으므로 필요한 초기화를 직접 호출해 샘플 상태를 만든다.
        /// </summary>
        public static void CaptureS83()
        {
            const System.Reflection.BindingFlags any = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
            void Call(object target, string method) => target.GetType().GetMethod(method, any)?.Invoke(target, null);

            // 타이틀 · 스탯 배분
            EditorSceneManager.OpenScene("Assets/Game/Scenes/Title.unity");
            var cam = Camera.main;
            UseCamera(cam);
            var title = Object.FindFirstObjectByType<SwordPrototype.UI.TitleView>();
            Call(title, "Awake"); Call(title, "Start");
            Shot(cam, cam.transform.position, cam.transform.position + Vector3.forward, "../html/s8-3-title.png");
            Shot(cam, cam.transform.position, cam.transform.position + Vector3.forward, "../html/s8-3-title.png");
            var alloc = (GameObject)typeof(SwordPrototype.UI.TitleView).GetField("allocPage", any).GetValue(title);
            typeof(SwordPrototype.UI.TitleView).GetMethod("Show", any).Invoke(title, new object[] { alloc });
            Call(title, "Update");
            Shot(cam, cam.transform.position, cam.transform.position + Vector3.forward, "../html/s8-3-alloc.png");

            // 전투 HUD · 선택 휠
            EditorSceneManager.OpenScene("Assets/Game/Scenes/Foundation.unity");
            cam = Camera.main;
            UseCamera(cam);
            foreach (var h in Object.FindObjectsByType<SwordPrototype.Battle.Health>(FindObjectsSortMode.None)) Call(h, "Awake");
            foreach (var s in Object.FindObjectsByType<SwordPrototype.Battle.PlayerStats>(FindObjectsSortMode.None)) Call(s, "Awake");
            var flow = Object.FindFirstObjectByType<SwordPrototype.Battle.BattleFlow>();
            Call(flow, "Awake");
            var player = GameObject.Find("Player").transform;
            player.position = new Vector3(0, 0.1f, -2.6f);
            flow.EnemyHealth.TakeDamage(70f);
            flow.AddLog("↗ 성공 -10"); flow.AddLog("↘ 막힘 (적 -2)"); flow.AddLog("패링 성공 · 적 무방비 (좌클릭 반격)");
            typeof(SwordPrototype.Battle.BattleFlow).GetMethod("BeginSelection", any).Invoke(flow, null);
            typeof(SwordPrototype.Battle.BattleFlow).GetProperty("State").SetValue(flow, SwordPrototype.Battle.BattleState.Planning);
            flow.Session.SetCursor(new Vector2(0.7f, 0.7f));
            flow.Session.Click();
            Time.timeScale = 1f;
            foreach (var v in Object.FindObjectsByType<SwordPrototype.UI.CombatHudView>(FindObjectsSortMode.None)) Call(v, "Update");
            foreach (var v in Object.FindObjectsByType<SwordPrototype.UI.SelectionWheelView>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Call(v, "Update");
            Canvas.ForceUpdateCanvases();
            foreach (var g in Object.FindObjectsByType<UnityEngine.UI.Graphic>(FindObjectsSortMode.None)) g.Rebuild(UnityEngine.UI.CanvasUpdate.PreRender);
            foreach (var w in Object.FindObjectsByType<SwordPrototype.UI.WheelGraphic>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var vh = new UnityEngine.UI.VertexHelper();
                typeof(SwordPrototype.UI.WheelGraphic).GetMethod("OnPopulateMesh", any, null, new[] { typeof(UnityEngine.UI.VertexHelper) }, null).Invoke(w, new object[] { vh });
                Debug.Log($"WHEEL_DIAG active={w.isActiveAndEnabled} verts={vh.currentVertCount} cull={w.canvasRenderer.cull} alpha={w.canvasRenderer.GetAlpha()} rect={w.rectTransform.rect} seg0={w.segmentColors[0]} depth={w.canvasRenderer.absoluteDepth} mat={(w.materialForRendering != null ? w.materialForRendering.shader.name : "null")}");
            }
            // 실제 선택 화면과 같은 포커스 카메라 구도(오른쪽 어깨 뒤 0.9m·뒤 2.4m, 앞 1.4m를 봄)
            player.rotation = Quaternion.identity;
            Vector3 focusEye = player.position + Vector3.up * 1.7f + player.right * 0.9f - player.forward * 2.4f;
            Vector3 focusLook = player.position + player.forward * 1.4f + Vector3.up * 1.1f;
            Shot(cam, focusEye, focusLook, "../html/s8-3-battle.png");
            Shot(cam, focusEye, focusLook, "../html/s8-3-battle.png");
            Debug.Log("S83_PREVIEW_OK");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void UseCamera(Camera cam)
        {
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 0.5f;
            }
            cam.nearClipPlane = 0.1f;
        }

        public static void CaptureS81()
        {
            EditorSceneManager.OpenScene("Assets/Game/Scenes/Foundation.unity");
            var cam = Camera.main;
            Shot(cam, new Vector3(18, 16, -40), new Vector3(0, 1, -2), "../html/s8-1-preview.png");
            Shot(cam, new Vector3(1.2f, 2.6f, -21f), new Vector3(0, 1.4f, 0), "../html/s8-1-preview-close.png");
            Debug.Log("S81_PREVIEW_OK");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        internal static void Shot(Camera cam, Vector3 position, Vector3 lookAt, string file)
        {
            cam.transform.position = position;
            cam.transform.LookAt(lookAt);
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var request = new RenderPipeline.StandardRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(cam, request)) RenderPipeline.SubmitRenderRequest(cam, request);
            else { cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; }
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.GetFullPath(file), tex.EncodeToPNG());
            RenderTexture.active = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
        }
    }
}
