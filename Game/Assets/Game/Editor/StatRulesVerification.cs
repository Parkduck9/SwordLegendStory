using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SwordPrototype.Battle;
using SwordPrototype.Enemy;

namespace SwordPrototype.Editor
{
    /// <summary>스탯 규칙 계층을 배치 모드에서 검사한다. 성공 시 STAT_RULES_OK, 실패 시 종료 코드 1.</summary>
    public static class StatRulesVerification
    {
        private static int failures;

        /// <summary>씬을 다시 만들고 전투 컴포넌트 연결을 확인한 뒤 규칙 검사를 실행한다.</summary>
        public static void BuildSceneAndVerify()
        {
            PrototypeBuilder.Build();
            failures = 0;
            var flow = UnityEngine.Object.FindFirstObjectByType<BattleFlow>();
            Check(flow != null, "씬에 BattleFlow 존재");
            CheckUI(flow);
            CheckAudio();
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerStats>();
            Check(player != null && player.GetComponent<Health>() != null, "플레이어 스탯·체력");
            Check(player != null && player.GetComponentsInChildren<Transform>().Any(t => t.name == "SwordSocket"), "검 부착점");
            Check(player != null && player.GetComponent<PlayerDash>() != null, "플레이어 대쉬");
            var brain = UnityEngine.Object.FindFirstObjectByType<SwordPrototype.Enemy.EnemyBrain>();
            Check(brain != null && brain.GetComponent<Health>() != null && Near(brain.GetComponent<Health>().Max, 220f), "적 AI·체력 220");
            Check(brain != null && brain.GetComponent<SwordPrototype.Enemy.EnemyPose>() != null, "적 자세");
            Check(player != null && player.GetComponentsInChildren<SwordPrototype.Presentation.IFighterVisual>().Length == 1
                && brain != null && brain.GetComponentsInChildren<SwordPrototype.Presentation.IFighterVisual>().Length == 1, "연출 리그(플레이어·적 각 1개)");
            CheckCharacters(player != null ? player.gameObject : null, brain != null ? brain.gameObject : null);
            Check(player != null && player.GetComponent<PlayerJump>() != null, "플레이어 점프");
            Check(UnityEngine.Object.FindObjectsByType<SwordPrototype.World.DestructibleProp>(FindObjectsSortMode.None).Length == 14, "파괴 소품 14개(기둥 8 + 바위 6)");
            var arenaGate = UnityEngine.Object.FindFirstObjectByType<SwordPrototype.World.ArenaGate>();
            Check(arenaGate != null && !arenaGate.GetComponent<Collider>().enabled, "입구 닫힘 장치(시작 시 열림)");
            Check(UnityEngine.Object.FindFirstObjectByType<SwordPrototype.UI.ResultView>() != null
                && UnityEngine.Object.FindFirstObjectByType<SwordPrototype.UI.PauseView>() != null, "결과 화면·일시정지 메뉴(uGUI)");
            CheckRendering();
            var scenes = UnityEditor.EditorBuildSettings.scenes;
            Check(scenes.Length == 2 && scenes[0].path.EndsWith("Title.unity") && scenes[1].path.EndsWith("Foundation.unity")
                && System.IO.File.Exists("Assets/Game/Scenes/Title.unity"), "빌드 목록: Title → Foundation");
            CheckArenaReachable();
            var probe = new GameObject("HealthProbe").AddComponent<Health>();
            probe.SetMax(50f);
            bool parryOpen = true;
            probe.ParryCheck = () => parryOpen;
            Check(probe.TakeDamage(10f) == 0f, "패링 구간 피해 무효");
            parryOpen = false;
            Check(Near(probe.TakeDamage(10f), 10f), "패링 없으면 피해");
            probe.Invulnerable = true;
            Check(probe.TakeDamage(10f) == 0f && Near(probe.TakeDamage(5f, true), 5f), "선택 무적 / 시간 초과 예외");
            UnityEngine.Object.DestroyImmediate(probe.gameObject);
            int sceneFailures = failures;
            Run(sceneFailures);
        }

        /// <summary>시작점에서 전투장 중앙까지 플레이어 크기의 캡슐이 막힘 없이 지나가고, 바닥 높이가 0인지 확인.</summary>
        private static void CheckArenaReachable()
        {
            Physics.SyncTransforms();
            const QueryTriggerInteraction ignore = QueryTriggerInteraction.Ignore;
            bool blocked = Physics.CapsuleCast(new Vector3(0, 0.5f, -29f), new Vector3(0, 1.7f, -29f), 0.3f, Vector3.forward, out RaycastHit hit, 27f, ~0, ignore);
            Check(!blocked, "진입로 → 중앙 이동 경로 막힘 없음" + (blocked ? $" (막힘: {hit.collider.name} z={hit.point.z:0.0})" : ""));
            foreach (float z in new[] { -28f, -24f, -15f, 0f, 15f })
            {
                bool ground = Physics.Raycast(new Vector3(0, 10f, z), Vector3.down, out RaycastHit g, 20f, ~0, ignore);
                Check(ground && Mathf.Abs(g.point.y) < 0.05f, $"바닥 높이 0 (z={z})" + (ground ? $" 실제 {g.point.y:0.00} {g.collider.name}" : " 바닥 없음"));
            }
        }

        /// <summary>S8-1: URP 지정·먹선 기능·후처리·모든 렌더러 셰이더 호환·소품 충돌체 크기 유지.</summary>
        private static void CheckRendering()
        {
            var pipeline = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            Check(pipeline != null, "URP 파이프라인 지정");
            var renderer = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>(RenderSetup.RendererPath);
            Check(renderer != null && renderer.rendererFeatures.Count == 1
                && renderer.rendererFeatures[0] is UnityEngine.Rendering.Universal.FullScreenPassRendererFeature f && f.passMaterial != null && f.passMaterial.shader.isSupported,
                "먹선 기능(셰이더 컴파일 포함)");
            var volume = UnityEngine.Object.FindFirstObjectByType<UnityEngine.Rendering.Volume>();
            Check(volume != null && volume.isGlobal && volume.sharedProfile != null && volume.sharedProfile.components.Count == 4, "후처리 볼륨(색보정·블룸·비네트·톤매핑)");
            Check(RenderSettings.fog && Camera.main != null && Camera.main.backgroundColor == RenderSetup.Paper, "한지색 안개·배경");
            Check(GameObject.Find("MountainsNear") && GameObject.Find("MountainsMid") && GameObject.Find("MountainsFar"), "먼 산 3겹");

            int bad = 0; string firstBad = null;
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                foreach (var m in r.sharedMaterials)
                {
                    bool ok = m != null && m.shader != null && m.shader.isSupported && !m.shader.name.Contains("InternalError")
                        && (m.shader.name.StartsWith("Universal Render Pipeline") || m.shader.name.StartsWith("Hidden/GeomHyeop"));
                    if (!ok) { bad++; if (firstBad == null) firstBad = r.name + ":" + (m == null ? "없음" : m.shader.name); }
                }
            Check(bad == 0, "모든 렌더러 URP 셰이더(분홍 깨짐 없음)" + (bad > 0 ? $" — {bad}개, 첫 번째 {firstBad}" : ""));

            bool sizes = true, models = true;
            foreach (var p in UnityEngine.Object.FindObjectsByType<SwordPrototype.World.DestructibleProp>(FindObjectsSortMode.None))
            {
                Vector3 s = Vector3.Scale(p.GetComponent<BoxCollider>().size, p.transform.lossyScale);
                bool pillar = p.name == "StonePillar";
                sizes &= pillar ? Near(s.x, 1.2f) && Near(s.y, 4f) : Near(s.x, 2f) && Near(s.y, 1.4f);
                // Kenney 모델이 들어갔고 높이가 상자와 맞는지
                var renderers = p.GetComponentsInChildren<Renderer>();
                Bounds b = renderers.Length > 0 ? renderers[0].bounds : default;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                models &= p.transform.Find(p.name + "Model") != null && renderers.Length > 0 && Mathf.Abs(b.size.y - s.y) < 0.15f && Mathf.Abs(b.min.y) < 0.1f;
            }
            Check(sizes, "소품 충돌체 크기 유지");
            Check(models, "소품 Kenney 모델 교체(높이·바닥 맞춤)");
            Check(Camera.main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>()?.renderPostProcessing == true, "카메라 후처리 켜짐");
        }

        /// <summary>S8-2: 애니메이션 캐릭터 구성(아바타·상태·리그·의상·키·검).</summary>
        private static void CheckCharacters(GameObject player, GameObject enemy)
        {
            if (!CharacterBuilder.Available) { Check(false, "Quaternius 애니메이션 팩 없음"); return; }
            var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(CharacterBuilder.ControllerPath);
            var states = controller.layers[0].stateMachine.states.Select(s => s.state.name).ToList();
            string[] needed = { "Locomotion", "Air", "Land", "Swing", "Roll", "Crouch", "Guard", "Hit", "HitHead", "Struggle", "Throw", "Stagger", "Death", "Victory" };
            var missing = needed.Where(n => !states.Contains(n)).ToList();
            Check(missing.Count == 0, "애니메이터 상태 14종" + (missing.Count > 0 ? " — 없음: " + string.Join(",", missing) : ""));
            bool clipsOk = controller.layers[0].stateMachine.states.All(s => s.state.motion != null);
            Check(clipsOk && controller.layers[0].iKPass, "모든 상태에 동작 연결 · IK 켜짐");

            // R6: UAL2 동작(검 막기·넉백), 두 팔 위 층, 뒷걸음 거꾸로 재생
            UnityEditor.Animations.AnimatorState St(string n) => controller.layers[0].stateMachine.states.Select(s => s.state).FirstOrDefault(s => s.name == n);
            Check(St("Guard")?.motion?.name == "Armature|Sword_Block" && St("Knockback")?.motion?.name == "Armature|Hit_Knockback", "R6 막기 = Sword_Block · 깊게 베임 = Hit_Knockback (UAL2)");
            // R8: KayKit 옆으로 달리기·뒷걸음을 넣은 2D 방향 섞기, 적 공격·회피·막기 동작
            var tree = St("Locomotion")?.motion as UnityEditor.Animations.BlendTree;
            Check(tree != null && tree.blendType == UnityEditor.Animations.BlendTreeType.FreeformDirectional2D && tree.blendParameter == "MoveX" && tree.blendParameterY == "MoveZ"
                && tree.children.Any(c => c.motion != null && c.motion.name == "Running_Strafe_Left" && c.position.x < 0f)
                && tree.children.Any(c => c.motion != null && c.motion.name == "Running_Strafe_Right" && c.position.x > 0f)
                && tree.children.Any(c => c.motion != null && c.motion.name == "Walking_Backwards" && c.position.y < 0f), "R8 이동: 앞·옆·뒤 실제 동작 2D 섞기(KayKit)");
            string[] r8States = { "DodgeBack", "DodgeLeft", "DodgeRight", "BlockHit", "Spinning", "Slice", "Chop", "Stab", "SpinAttack", "Kick", "JumpChop" };
            var noMotion = r8States.Where(n => St(n)?.motion == null).ToList();
            Check(noMotion.Count == 0 && St("Throw")?.motion?.name == "Throw" && St("Hit")?.motion?.name == "Hit_A",
                "R8 KayKit 동작 상태 연결(회피·막기·적 공격·던지기·피격)" + (noMotion.Count > 0 ? " — 없음: " + string.Join(",", noMotion) : ""));
            string kayLicense = KayKitImport.Folder + "/License.txt";
            Check(System.IO.File.Exists(kayLicense) && System.IO.File.ReadAllText(kayLicense).Contains("CC0"), "R8 KayKit 라이선스 문서(CC0) 보관");
            var arms = controller.layers.Length > 1 ? controller.layers[1] : null;
            Check(arms != null && arms.name == "Arms" && arms.iKPass && arms.avatarMask != null
                && arms.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm) && arms.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm)
                && !arms.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Body) && !arms.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg)
                && arms.stateMachine.defaultState?.motion?.name == "Armature|Sword_Idle", "R6 두 팔만 덮는 검 겨눔 층(몸통·다리는 달리기 그대로)");
            string ual2License = System.IO.Path.GetDirectoryName(CharacterImport.Library2Path) + "/License.txt";
            Check(System.IO.File.Exists(ual2License) && System.IO.File.ReadAllText(ual2License).Contains("CC0"), "R6 UAL2 라이선스 문서(CC0) 보관");
            string ubcLicense = BaseCharacterImport.Folder + "/License.txt";
            Check(!CharacterBuilder.UseBaseCharacter || (System.IO.File.Exists(ubcLicense) && System.IO.File.ReadAllText(ubcLicense).Contains("CC0")), "R7 기본 모델 라이선스 문서(CC0) 보관");
            foreach (var (go, label, height) in new[] { (player, "플레이어", 1.8f), (enemy, "적", 2.25f) })
            {
                var animator = go != null ? go.GetComponentInChildren<Animator>() : null;
                Check(animator != null && animator.avatar != null && animator.avatar.isValid && animator.avatar.isHuman && !animator.applyRootMotion, label + " Humanoid 아바타(루트 모션 끔)");
                Check(go != null && go.GetComponentInChildren<SwordPrototype.Presentation.AnimatorRig>() != null, label + " AnimatorRig");
                var model = go != null ? go.transform.Find("Model") : null;
                Check(model != null && Mathf.Abs(model.localScale.y * CharacterBuilder.ModelHeight - height) < 0.01f, label + $" 키 {height}m");
                Check(go != null && go.GetComponentsInChildren<Transform>().Any(t => t.name == "RobeSkirt") && go.GetComponentsInChildren<Transform>().Any(t => t.name == "Belt"), label + " 도포·허리띠");
                if (animator != null) CheckFacing(animator, go.transform, label);
                if (animator != null && CharacterBuilder.UseBaseCharacter)
                {
                    // R7: 기본 모델 — 옷/피부 두 부분, 머리 모양이 머리뼈에 붙음
                    var bodyRenderer = go.GetComponentsInChildren<SkinnedMeshRenderer>().FirstOrDefault(r => r.name != "Eyes" && r.name != "Eyebrows");
                    var mesh = bodyRenderer != null ? bodyRenderer.sharedMesh : null;
                    float skinShare = mesh != null && mesh.subMeshCount == 2 ? mesh.GetSubMesh(1).indexCount / (float)(mesh.GetSubMesh(0).indexCount + mesh.GetSubMesh(1).indexCount) : 0f;
                    // 얼굴·손가락은 삼각형이 촘촘해 개수 비율은 절반 가까이 나온다(면적 비율 아님)
                    Check(bodyRenderer != null && bodyRenderer.sharedMaterials.Length == 2 && skinShare > 0.05f && skinShare < 0.7f,
                        label + $" R7 몸 = 무복(옷) + 피부(머리·목·손) 두 부분 (피부 {skinShare:P0})");
                    var headBone = animator.GetBoneTransform(HumanBodyBones.Head);
                    string[] styles = label == "플레이어" ? new[] { "Hair_SimpleParted" } : new[] { "Hair_Buzzed", "Hair_Beard" };
                    Check(headBone != null && styles.All(s => headBone.Find(s) != null), label + " R7 머리 모양이 머리뼈에 붙음(" + string.Join(", ", styles) + ")");
                }
            }
            Check(player != null && player.GetComponentsInChildren<Transform>().Any(t => t.name == "Hat"), "플레이어 삿갓");
            Check(player != null && player.GetComponentsInChildren<Transform>().Any(t => t.name == "Tassel")
                && enemy != null && enemy.GetComponentsInChildren<Transform>().First(t => t.name == "Blade").localScale.x > 0.1f, "직검·대도");
        }

        /// <summary>캐릭터가 루트 앞(+Z)을 보는지: 대기·걷기·달리기·베기에서 발끝과 가슴 방향을 잰다.</summary>
        private static void CheckFacing(Animator animator, Transform root, string label)
        {
            Quaternion saved = root.rotation;
            root.rotation = Quaternion.identity;
            var bad = new List<string>();
            foreach (var (state, speed) in new[] { ("Locomotion", 0f), ("Locomotion", 1.6f), ("Locomotion", 4.5f), ("Swing", 0f) })
            {
                animator.Rebind();
                animator.SetFloat("Speed", speed);
                animator.Play(state, 0, 0f);
                animator.Update(0f);
                for (int i = 0; i < 10; i++) animator.Update(0.03f);
                Vector3 Toe(HumanBodyBones foot, HumanBodyBones toes) =>
                    Vector3.ProjectOnPlane(animator.GetBoneTransform(toes).position - animator.GetBoneTransform(foot).position, Vector3.up);
                Vector3 toe = (Toe(HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes) + Toe(HumanBodyBones.RightFoot, HumanBodyBones.RightToes)).normalized;
                Vector3 hipsRight = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).position - animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position;
                Vector3 body = Vector3.Cross(hipsRight, Vector3.up).normalized;   // 오른쪽 × 위 = 앞
                // 베기는 허리를 옆으로 트는 자세라 "뒤를 보지 않음"만 본다.
                float need = state == "Swing" ? 0f : 0.5f;
                if (Vector3.Dot(toe, Vector3.forward) < need || Vector3.Dot(body, Vector3.forward) < need)
                    bad.Add($"{state}({speed}) 발끝 {Vector3.Dot(toe, Vector3.forward):0.00} 몸 {Vector3.Dot(body, Vector3.forward):0.00}");
            }
            animator.Rebind();
            root.rotation = saved;
            Check(bad.Count == 0, label + " 앞(이동 방향)을 봄" + (bad.Count > 0 ? " — " + string.Join(", ", bad) : ""));
        }

        /// <summary>S8-3: 수묵 UI — 글꼴(한글·기호 생성 가능), 화면 구성, 이벤트 시스템, IMGUI 제거, OFL 문서 포함.</summary>
        private static void CheckUI(BattleFlow flow)
        {
            var skin = UnityEditor.AssetDatabase.LoadAssetAtPath<SwordPrototype.UI.InkUISkin>(UIBuilder.SkinPath);
            Check(skin != null && skin.body != null && skin.title != null && skin.panel != null && skin.brush != null && skin.circle != null && skin.seal != null, "UI 스킨(글꼴 2·스프라이트 4)");
            if (skin != null && skin.body != null)
            {
                string needed = "검협전 승리 패배 체력 기력 속도 힘 특수기 일시정지 다시 하기 스탯 배분 하드코어 무방비 지나가며 베기 회전 마무리 예약 불가" + UIBuilder.Symbols + "0123456789";
                bool bodyOk = skin.body.TryAddCharacters(needed, out string missingBody);
                Check(bodyOk, "본문 글꼴 한글·화살표·●○" + (bodyOk ? "" : " — 없음: " + missingBody));
                bool titleOk = skin.title.TryAddCharacters("검협전승리패배조작법스탯배분일시정지설정", out string missingTitle);
                Check(titleOk, "제목 글꼴(붓)" + (titleOk ? "" : " — 없음: " + missingTitle));
                Check(skin.body.atlasPopulationMode == TMPro.AtlasPopulationMode.Dynamic && skin.body.sourceFontFile != null, "동적 한글 글꼴(원본 글꼴 참조)");
            }
            var hud = UnityEngine.Object.FindFirstObjectByType<SwordPrototype.UI.CombatHudView>();
            var wheel = UnityEngine.Object.FindFirstObjectByType<SwordPrototype.UI.SelectionWheelView>(FindObjectsInactive.Include);
            Check(hud != null && wheel != null && wheel.Root != null && !wheel.Root.activeSelf, "전투 HUD · 선택 휠(평소 숨김)");
            var ringGraphic = wheel != null ? wheel.GetComponentInChildren<SwordPrototype.UI.WheelGraphic>(true) : null;
            Check(ringGraphic != null && ringGraphic.GetComponent<CanvasRenderer>() != null, "선택 휠 고리 그리기 부품(CanvasRenderer)");
            Check(wheel != null && wheel.GetComponentInChildren<SwordPrototype.UI.WheelGraphic>(true) != null
                && wheel.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Length >= 8 + 1 + 1 + 3 + 3, "선택 휠 8칸·가운데·타이머·슬롯 3·안내 3");
            Check(UnityEngine.Object.FindFirstObjectByType<SwordPrototype.UI.ControlsHintView>() != null, "조작 안내(F1)");
            if (wheel != null && wheel.Root != null) CheckWheelLayout(wheel.Root.GetComponent<RectTransform>());
            Check(UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null, "이벤트 시스템");
            bool noImgui = !typeof(BattleFlow).Assembly.GetTypes().Any(t => t.GetMethod("OnGUI", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public) != null && typeof(MonoBehaviour).IsAssignableFrom(t));
            Check(noImgui, "IMGUI 화면 스크립트 제거");
            Check(System.IO.File.Exists("Assets/StreamingAssets/Licenses/OFL-NotoSansKR.txt") && System.IO.File.Exists("Assets/StreamingAssets/Licenses/OFL-NanumBrushScript.txt"), "빌드 포함 OFL 라이선스 문서");
            // 타이틀 장면에도 화면이 있는지(파일 내용 검사)
            string title = System.IO.File.ReadAllText("Assets/Game/Scenes/Title.unity");
            var titleGuid = UnityEditor.AssetDatabase.AssetPathToGUID("Assets/Game/Scripts/UI/TitleView.cs");
            Check(title.Contains(titleGuid), "타이틀 장면 TitleView");
        }

        /// <summary>
        /// R4: 선택 휠 배치 — 16:9·16:10 캔버스 크기에서 휠 고리가 화면 가운데(적)를 비우고,
        /// 휠의 모든 글자·그림이 화면 안에 있으며 조작 안내 상자·쿨타임 원과 겹치지 않는지 계산으로 확인.
        /// </summary>
        private static void CheckWheelLayout(RectTransform wheelRoot)
        {
            var all = UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var others = all.Where(t => t.name == "ControlsHint" || t.name.StartsWith("Cooldown_")).ToList();
            var parts = wheelRoot.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).Select(g => g.rectTransform).ToList();
            var ring = wheelRoot.Find("Ring") as RectTransform;
            // 1920×1080 기준, 화면 비율 맞춤 0.5 → 16:10 화면의 캔버스는 약 1822×1138
            foreach (var canvas in new[] { new Vector2(1920, 1080), new Vector2(1822, 1138) })
            {
                var bad = new List<string>();
                Rect screen = new Rect(Vector2.zero, canvas);
                Rect ringRect = Layout(ring, canvas).rect;
                if (ringRect.xMin < canvas.x * 0.52f) bad.Add($"휠이 가운데를 가림(왼쪽 끝 {ringRect.xMin:0})");
                foreach (var p in parts)
                {
                    Rect r = Layout(p, canvas).rect;
                    if (r.xMin < 0 || r.yMin < 0 || r.xMax > canvas.x || r.yMax > canvas.y) bad.Add(p.name + " 화면 밖");
                    foreach (var o in others)
                        if (r.Overlaps(Layout(o, canvas).rect)) bad.Add($"{p.name}↔{o.name}");
                }
                Check(bad.Count == 0, $"선택 휠 배치 {canvas.x}×{canvas.y}: 가운데 비움 · 화면 안 · 안내/쿨타임과 안 겹침" + (bad.Count > 0 ? " — " + string.Join(", ", bad.Distinct().Take(6)) : ""));
            }
        }

        // 캔버스 기준 사각형(늘어나는 앵커 포함, 부모 배율 누적)
        private static (Rect rect, float scale) Layout(RectTransform rt, Vector2 canvas)
        {
            Rect parentRect; float parentScale;
            var parent = rt.parent as RectTransform;
            if (parent == null || parent.GetComponent<Canvas>() != null) { parentRect = new Rect(Vector2.zero, canvas); parentScale = 1f; }
            else (parentRect, parentScale) = Layout(parent, canvas);
            Vector2 lo = parentRect.min + Vector2.Scale(rt.anchorMin, parentRect.size);
            Vector2 hi = parentRect.min + Vector2.Scale(rt.anchorMax, parentRect.size);
            float s = parentScale * rt.localScale.x;
            Vector2 size = (hi - lo) + rt.sizeDelta * s;
            Vector2 pivotPoint = lo + Vector2.Scale(rt.pivot, hi - lo) + rt.anchoredPosition * parentScale;
            return (new Rect(pivotPoint - Vector2.Scale(rt.pivot, size), size), s);
        }

        /// <summary>S8-4: 소리 이름마다 클립 연결, 예고음 5종 서로 다름, 합성 길이, 배경음 스트리밍, 장면별 AudioService, 라이선스 문서, 음량 설정 범위.</summary>
        private static void CheckAudio()
        {
            var bank = UnityEditor.AssetDatabase.LoadAssetAtPath<SwordPrototype.Audio.SoundBank>(SoundBankBuilder.BankPath);
            Check(bank != null, "SoundBank 에셋");
            if (bank == null) return;
            var missing = SwordPrototype.Audio.Sfx.All.Where(n => { var e = bank.Find(n); return e == null || e.clips == null || e.clips.Length == 0 || e.clips.Any(c => c == null); }).ToList();
            Check(missing.Count == 0, $"소리 {SwordPrototype.Audio.Sfx.All.Length}종 모두 클립 연결" + (missing.Count > 0 ? " — 없음: " + string.Join(",", missing) : ""));
            var cues = new[] { EnemyAttackKind.Sweep, EnemyAttackKind.ChargeSlash, EnemyAttackKind.Throw, EnemyAttackKind.LeapSlam, EnemyAttackKind.RetreatShot }
                .Select(k => bank.Find(SwordPrototype.Audio.Sfx.Cue(k))?.clips.FirstOrDefault()).ToList();
            Check(cues.All(c => c != null) && cues.Distinct().Count() == 5, "적 패턴 5종 예고음 서로 다름");
            float Len(string n) => bank.Find(n).clips[0].length;
            Check(Mathf.Abs(Len("BgmTitle") - 44f) < 0.2f && Mathf.Abs(Len("BgmBattle") - 37f) < 0.2f && Len("Swing") < 0.4f && Mathf.Abs(Len("CueLeap") - 1.3f) < 0.05f, "합성 소리 길이(배경음 44·37초, 베기 짧게, 징 1.3초)");
            var bgm = (UnityEditor.AudioImporter)UnityEditor.AssetImporter.GetAtPath(UnityEditor.AssetDatabase.GetAssetPath(bank.Find("BgmBattle").clips[0]));
            Check(bank.Find("BgmBattle").category == SwordPrototype.Audio.SoundCategory.Music && bgm.defaultSampleSettings.loadType == AudioClipLoadType.Streaming, "배경음 분류·스트리밍");
            Check(UnityEngine.Object.FindFirstObjectByType<SwordPrototype.Audio.AudioService>() != null, "전투장 AudioService");
            string title = System.IO.File.ReadAllText("Assets/Game/Scenes/Title.unity");
            Check(title.Contains(UnityEditor.AssetDatabase.AssetPathToGUID("Assets/Game/Scripts/Audio/AudioService.cs")), "타이틀 AudioService");
            Check(new[] { "impact-sounds", "rpg-audio", "interface-sounds" }.All(p => System.IO.File.Exists($"Assets/Game/Art/Audio/Kenney/{p}/License.txt")), "Kenney 라이선스 문서 동봉");
            SwordPrototype.Flow.GameSettings.MasterVolume = 2f;
            Check(Near(SwordPrototype.Flow.GameSettings.MasterVolume, 1f), "음량 설정 0~1 제한");
            SwordPrototype.Flow.GameSettings.MasterVolume = 0.8f;
        }

        public static void Run() => Run(0);

        private static void Run(int carried)
        {
            failures = carried;
            var t = new StatTuning();
            CheckStats(); CheckSuccess(t); CheckDamage(t); CheckCombo(); CheckSideBias(t); CheckKnife(t); CheckParry(t); CheckFront();
            CheckSelection(t); CheckResolver(t); CheckEnemy(); CheckPresentation(t); CheckJumpAndProps(t); CheckFlow(t); CheckReviewFixes(); CheckStance(); CheckKamaeAndWounds(); CheckDefenseAndPacing();
            Debug.Log(failures == 0 ? "STAT_RULES_OK" : $"STAT_RULES_FAILED {failures}");
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        private static void Check(bool condition, string label)
        {
            if (condition) return;
            failures++;
            Debug.LogError("[스탯 규칙 실패] " + label);
        }

        private static bool Near(float a, float b) => Mathf.Abs(a - b) < 0.0001f;

        private static void CheckStats()
        {
            Check(new CharacterStats(2, 2, 2, 2, 2).IsValid(out _), "2/2/2/2/2 허용");
            Check(!new CharacterStats(3, 3, 3, 2, 0).IsValid(out _), "11포인트 거부");
            Check(!new CharacterStats(4, 0, 0, 0, 0).IsValid(out _), "레벨 4 거부");
        }

        private static void CheckSuccess(StatTuning t)
        {
            var front = new StrikeContext { attackerInFront = true };
            var back = new StrikeContext { attackerInFront = false };
            Check(Near(StatEffects.SuccessChance(new CharacterStats(0, 2, 2, 2, 2), t, front), 0.40f), "체력 0 정면 하락");
            Check(Near(StatEffects.SuccessChance(new CharacterStats(2, 2, 2, 2, 2), t, front), 0.65f), "체력 2 정면 상승");
            Check(Near(StatEffects.SuccessChance(new CharacterStats(2, 2, 3, 2, 1), t, back), 0.70f), "속도 3 후면 상승");
            Check(Near(StatEffects.SuccessChance(new CharacterStats(2, 2, 0, 3, 3), t, back), 0.40f), "속도 0 후면 + 전체 하락");
            var repeat = new StrikeContext { attackerInFront = true, sameDirectionSecond = true, sideBiasPenalty = 0.1f, knifePenalty = 0.15f };
            Check(Near(StatEffects.SuccessChance(new CharacterStats(2, 2, 2, 2, 2), t, repeat), 0.25f), "반복·좌우·비도 패널티 합산");
            var worst = new StrikeContext { attackerInFront = true, sameDirectionSecond = true, sideBiasPenalty = 0.3f, knifePenalty = 0.15f };
            Check(Near(StatEffects.SuccessChance(new CharacterStats(0, 3, 0, 3, 3), t, worst), t.minSuccess), "성공률 하한");
        }

        private static void CheckDamage(StatTuning t)
        {
            Check(Near(StatEffects.HitDamage(new CharacterStats(2, 2, 2, 2, 2), t, false, 0.99f, out _), 10f), "힘 2 기본 피해");
            Check(Near(StatEffects.HitDamage(new CharacterStats(1, 2, 2, 0, 2), t, false, 0.99f, out _), 4f), "힘 0 매우 낮은 피해");
            Check(Near(StatEffects.HitDamage(new CharacterStats(0, 2, 2, 2, 2), t, false, 0.99f, out _), 8f), "체력 0 피해 감소");
            Check(Near(StatEffects.HitDamage(new CharacterStats(2, 2, 2, 2, 2), t, true, 0.99f, out _), 13f), "같은 방향 둘째 타 D추가");
            float crit = StatEffects.HitDamage(new CharacterStats(2, 2, 1, 3, 2), t, false, 0.05f, out bool isCrit);
            Check(isCrit && Near(crit, 21f), "힘 3 치명타");
            StatEffects.HitDamage(new CharacterStats(2, 2, 2, 2, 2), t, false, 0.05f, out bool noCrit);
            Check(!noCrit, "힘 2 치명타 없음");
            Check(StatEffects.SpecialDamage(new CharacterStats(2, 2, 2, 0, 3), t, 1f) < StatEffects.SpecialDamage(new CharacterStats(2, 2, 2, 2, 2), t, 1f), "힘 0 특수기 피해 감소");
            StatEffects.FailFeedback(new CharacterStats(1, 3, 2, 2, 2), t, out float self, out float enemy);
            Check(self > 0f && enemy == 0f, "체력 낮음 실패 시 자신 피해");
            StatEffects.FailFeedback(new CharacterStats(2, 2, 2, 2, 2), t, out self, out enemy);
            Check(self == 0f && enemy > 0f, "체력 높음 실패 시 적 피해");
        }

        private static void CheckCombo()
        {
            var s3 = new CharacterStats(1, 1, 3, 2, 3);
            var s2 = new CharacterStats(2, 1, 3, 2, 2);
            var two = new List<AttackDirection> { AttackDirection.UpRight, AttackDirection.UpRight };
            Check(ComboRules.CanReserve(two, AttackDirection.UpRight, s3, true) == ReserveResult.AcceptedAsFinisher, "특수기 3 같은 방향 3번째 허용");
            Check(ComboRules.CanReserve(two, AttackDirection.UpRight, s3, false) == ReserveResult.FinisherUnavailable, "쿨타임 중 불가");
            Check(ComboRules.CanReserve(two, AttackDirection.UpRight, s2, true) == ReserveResult.FinisherUnavailable, "특수기 2 불가");
            Check(ComboRules.CanReserve(two, AttackDirection.DownRight, s2, true) == ReserveResult.Accepted, "방향 변경 3번째 허용");
            Check(ComboRules.CanReserve(new List<AttackDirection> { AttackDirection.Up }, AttackDirection.Up, new CharacterStats(3, 3, 1, 3, 0), true) == ReserveResult.ComboFull, "속도 1은 1타");
            Check(ComboRules.MaxHits(0) == 1 && ComboRules.MaxHits(2) == 2 && ComboRules.MaxHits(3) == 3, "속도별 타수");
            Check(ComboRules.RepeatColor(1) == ComboRules.FirstColor && ComboRules.RepeatColor(2) == ComboRules.SecondColor && ComboRules.RepeatColor(3) == ComboRules.ThirdColor, "반복 색상");
        }

        private static void CheckSideBias(StatTuning t)
        {
            var bias = new SideBiasTracker();
            Check(AttackDirection.Up.Side() == AttackSide.None && AttackDirection.DownLeft.Side() == AttackSide.Left, "좌우 묶음");
            bias.RecordCombo(AttackSide.Left, true, false);
            Check(Near(bias.FailPenalty(AttackSide.Left, t), 0.1f), "왼쪽 공격 후 다음 콤보 +10%");
            bias.RecordCombo(AttackSide.Left, true, true);
            Check(Near(bias.FailPenalty(AttackSide.Left, t), 0.2f), "연속 성공 시 다다음 +20%");
            bias.RecordCombo(AttackSide.Left, true, true);
            bias.RecordCombo(AttackSide.Left, true, true);
            Check(Near(bias.FailPenalty(AttackSide.Left, t), t.sideBiasMax), "상한");
            bias.RecordCombo(AttackSide.Left, false, false);
            Check(bias.FailPenalty(AttackSide.Left, t) == 0f && bias.FailPenalty(AttackSide.Right, t) == 0f, "미사용 시 초기화");
        }

        private static void CheckKnife(StatTuning t)
        {
            var knife = new ThrowingKnifeState();
            Check(knife.Throw(0f, t), "첫 투척 100% 성공");
            Check(Near(knife.FailChance, t.knifeFailAfterSuccess), "성공 후 실패 확률 급증");
            Check(Near(knife.ConsumeNextAttackPenalty(t), t.knifeNextAttackPenalty) && knife.ConsumeNextAttackPenalty(t) == 0f, "다음 선택만 패널티");
            for (int i = 0; i < 200; i++) knife.Tick(0.1f, t);
            Check(Near(knife.FailChance, t.knifeFailFloor), "시간 경과 후 하한 30%");
            Check(!knife.Throw(0.29f, t) && Near(knife.FailChance, t.knifeFailFloor), "하한에서 실패 가능");
        }

        private static void CheckParry(StatTuning t)
        {
            var parry = new GuardParry();
            Check(!parry.TryStart(new CharacterStats(2, 2, 2, 2, 2), t), "체력 2 가드 불가");
            var s = new CharacterStats(3, 2, 2, 2, 1);
            Check(parry.TryStart(s, t) && parry.IsParrying, "체력 3 가드 시작");
            Check(!parry.TryStart(s, t), "쿨타임 중 재시작 불가");
            Check(parry.TryParry(t) && Near(parry.Cooldown.Remaining, t.parryCooldown * 0.5f), "패링 성공 쿨타임 환급");
            parry.Tick(0.31f);
            Check(!parry.IsParrying, "0.3초 후 종료");
            Check(StatEffects.DodgeInvulnerability(new CharacterStats(2, 2, 2, 2, 0), t) == 0f && Near(StatEffects.DodgeInvulnerability(s, t), 0.3f), "특수기 0 무적 없음 / 1 이상 0.3초");
            Check(StatEffects.HasDashSlash(new CharacterStats(1, 1, 2, 3, 3)), "특수기 단계 누적");
        }

        private static void CheckSelection(StatTuning t)
        {
            Check(SelectionSession.FromVector(new Vector2(0, 1)) == AttackDirection.Up && SelectionSession.FromVector(new Vector2(1, 1)) == AttackDirection.UpRight
                && SelectionSession.FromVector(new Vector2(-1, -1)) == AttackDirection.DownLeft && SelectionSession.FromVector(new Vector2(-1, 0.1f)) == AttackDirection.Left, "8방향 변환");

            var s3 = new SelectionSession(new CharacterStats(1, 1, 3, 2, 3), t, true);
            Check(Near(s3.Duration, 2f) && s3.MaxHits == 3, "속도 3: 2초·3타");
            s3.SetCursor(new Vector2(0.7f, 0.7f));
            s3.Click(); s3.Click();
            Check(s3.Evaluate(AttackDirection.UpRight) == ReserveResult.AcceptedAsFinisher, "↗↗ 후 마무리 가능");
            s3.Click();
            Check(s3.End == SelectionEnd.Completed && s3.Reserved.Count == 3, "3타 예약 완료 즉시 종료");

            var s2 = new SelectionSession(new CharacterStats(2, 2, 3, 1, 2), t, true);
            s2.SetCursor(new Vector2(0.7f, 0.7f));
            s2.Click(); s2.Click(); s2.Click();
            Check(s2.Reserved.Count == 2 && s2.LastRejection == ReserveResult.FinisherUnavailable, "특수기 2: 3번째 불가 표시");
            s2.Tick(2.1f);
            Check(s2.End == SelectionEnd.TimedOut && s2.Reserved.Count == 2, "부분 예약 후 만료");

            var knife = new SelectionSession(new CharacterStats(1, 3, 2, 2, 2), t, false);
            knife.SetCursor(Vector2.zero);
            knife.Click();
            Check(knife.End == SelectionEnd.KnifeThrown, "가운데 클릭 → 비도, 선택 종료");
            var noKnife = new SelectionSession(new CharacterStats(2, 2, 2, 2, 2), t, false);
            noKnife.SetCursor(Vector2.zero);
            noKnife.Click();
            Check(noKnife.End == SelectionEnd.None && noKnife.Reserved.Count == 0, "기력 2는 가운데 클릭 무시");

            var slow = new SelectionSession(new CharacterStats(2, 2, 0, 2, 2), t, false);
            slow.Tick(0.5f);
            Check(slow.End == SelectionEnd.None, "속도 0: 0.5초엔 진행 중");
            slow.Tick(0.6f);
            Check(slow.End == SelectionEnd.TimedOut, "속도 0: 1초 만료");
        }

        private static void CheckResolver(StatTuning t)
        {
            var s = new CharacterStats(2, 1, 3, 2, 2);
            var plan = new List<AttackDirection> { AttackDirection.Left, AttackDirection.Left, AttackDirection.Right };
            var bias = new SideBiasTracker();
            var hits = CombatResolver.Resolve(plan, s, t, true, bias, 0f, () => 0f);
            Check(hits.Count == 3 && hits.TrueForAll(h => h.success), "난수 0 → 모두 성공");
            Check(Near(hits[0].damageToEnemy, 10f) && Near(hits[1].damageToEnemy, 13f) && Near(hits[2].damageToEnemy, 10f), "같은 방향 둘째 타만 추가 피해");
            Check(hits[1].chance < hits[0].chance && Near(hits[2].chance, hits[0].chance), "둘째 타만 실패 확률 증가");
            Check(bias.Level(AttackSide.Left) == 1 && bias.Level(AttackSide.Right) == 1, "좌우 기록");

            var fails = CombatResolver.Resolve(new List<AttackDirection> { AttackDirection.Up }, new CharacterStats(1, 2, 2, 2, 3), t, true, new SideBiasTracker(), 0f, () => 0.999f);
            Check(!fails[0].success && Near(fails[0].damageToPlayer, t.failSelfDamage), "실패 시 체력 낮음 반동");

            var fin = CombatResolver.Resolve(new List<AttackDirection> { AttackDirection.Up, AttackDirection.Up, AttackDirection.Up }, new CharacterStats(1, 1, 3, 2, 3), t, false, new SideBiasTracker(), 0f, () => 1f);
            Check(fin[2].finisher && fin[2].success && Near(fin[2].damageToEnemy, 40f) && Near(fin[2].chance, 1f) && !fin[0].success, "마무리: 난수 1에도 무조건 성공, ×4 피해");

            var counter = CombatResolver.Resolve(new List<AttackDirection> { AttackDirection.Left }, new CharacterStats(0, 3, 0, 3, 3), t, true, new SideBiasTracker(), 0.15f, () => 1f, true);
            Check(counter[0].success && Near(counter[0].chance, 1f), "패링 반격: 무조건 성공");

            var knife = new ThrowingKnifeState();
            var throwHit = CombatResolver.ResolveKnife(knife, new CharacterStats(1, 3, 2, 2, 2), t, () => 0.99f);
            Check(throwHit.success && Near(throwHit.damageToEnemy, 8f), "비도 첫 투척 명중");

            var slashStats = new CharacterStats(2, 2, 2, 2, 2);
            var slash = CombatResolver.ResolveDashSlash(slashStats, t);
            Check(slash.dashSlash && slash.success && Near(slash.damageToEnemy, 45f), "특수기 2: 무조건 성공, ×4.5 피해");
            var bestCombo = CombatResolver.Resolve(new List<AttackDirection> { AttackDirection.Up, AttackDirection.Up, AttackDirection.Right }, new CharacterStats(1, 1, 3, 2, 2), t, true, new SideBiasTracker(), 0f, () => 0f);
            float comboSum = 0f; foreach (var h in bestCombo) comboSum += h.damageToEnemy;
            Check(slash.damageToEnemy > comboSum, "베기 피해 > 일반 3타 합");

            var noSlash = new SelectionSession(new CharacterStats(2, 2, 2, 2, 1), t, false, true);
            noSlash.RequestDashSlash();
            Check(noSlash.End == SelectionEnd.None, "특수기 1은 지나가며 베기 불가");
            var withSlash = new SelectionSession(slashStats, t, false, true);
            withSlash.SetCursor(new Vector2(0, 1)); withSlash.Click();
            withSlash.RequestDashSlash();
            Check(withSlash.End == SelectionEnd.None && withSlash.DashSlashBlockedByReservation, "예약 후에는 베기 불가");
            var instant = new SelectionSession(slashStats, t, false, true);
            instant.RequestDashSlash();
            Check(instant.End == SelectionEnd.DashSlash && instant.Reserved.Count == 0, "예약 없이 바로 베기");

            var counterSession = new SelectionSession(new CharacterStats(1, 3, 3, 0, 3), t, true, true, true);
            Check(counterSession.MaxHits == 1 && !counterSession.KnifeSelectable && !counterSession.DashSlashSelectable, "반격: 1타, 비도·베기 불가");
            counterSession.SetCursor(new Vector2(0, 1)); counterSession.Click(); counterSession.Click();
            Check(counterSession.End == SelectionEnd.Completed && counterSession.Reserved.Count == 1, "반격: 1타 예약 후 종료");
            var cooling = new SelectionSession(slashStats, t, false, false);
            cooling.RequestDashSlash();
            Check(cooling.End == SelectionEnd.None, "쿨타임 중 지나가며 베기 불가");
        }

        private static void CheckEnemy()
        {
            var m = new SwordPrototype.Enemy.EnemyMoveset();
            // R8: 거리 구간 확률표(사용자 확인) · 직전 절반 · 3연속 금지 · 큰 공격 뒤 근접 후퇴 사격 40% · 예고·후딜 ×0.7
            System.Collections.Generic.Dictionary<EnemyAttackKind, float> Freq(float dist, bool hasLast, EnemyAttackKind last, int streak)
            {
                var f = new System.Collections.Generic.Dictionary<EnemyAttackKind, float>();
                const int N = 1000;
                for (int i = 0; i < N; i++) { var k = m.Choose(dist, hasLast, last, streak, (i + 0.5f) / N); f[k] = (f.TryGetValue(k, out float v) ? v : 0f) + 1f / N; }
                return f;
            }
            bool Near01(System.Collections.Generic.Dictionary<EnemyAttackKind, float> f, EnemyAttackKind k, float p) => Mathf.Abs((f.TryGetValue(k, out float v) ? v : 0f) - p) < 0.011f;
            var near = Freq(2f, false, EnemyAttackKind.Sweep, 0);
            Check(Near01(near, EnemyAttackKind.Sweep, 0.35f) && Near01(near, EnemyAttackKind.SpinSlash, 0.2f) && Near01(near, EnemyAttackKind.Kick, 0.15f)
                && Near01(near, EnemyAttackKind.LeapSlam, 0.15f) && Near01(near, EnemyAttackKind.ComboAssault, 0.15f), "R8 0~3m: 휘두르기35·회전20·차기15·도약15·합15");
            var mid = Freq(4.5f, false, EnemyAttackKind.Sweep, 0);
            Check(Near01(mid, EnemyAttackKind.ComboAssault, 0.4f) && Near01(mid, EnemyAttackKind.ChargeSlash, 0.2f) && Near01(mid, EnemyAttackKind.Thrust, 0.2f) && Near01(mid, EnemyAttackKind.LeapSlam, 0.2f), "R8 3~6m: 합40·돌진20·찌르기20·도약20");
            var far = Freq(10f, false, EnemyAttackKind.Sweep, 0);
            Check(Near01(far, EnemyAttackKind.Throw, 0.3f) && Near01(far, EnemyAttackKind.FanThrow, 0.2f) && Near01(far, EnemyAttackKind.ChargeSlash, 0.2f) && Near01(far, EnemyAttackKind.LeapSlam, 0.3f), "R8 6~15m: 투척30·부채20·돌진20·도약30");
            var veryFar = Freq(30f, false, EnemyAttackKind.Sweep, 0);
            Check(Near01(veryFar, EnemyAttackKind.LeapSlam, 0.6f) && Near01(veryFar, EnemyAttackKind.FanThrow, 0.4f), "R8 15m~: 도약60·부채40");
            var halved = Freq(4.5f, true, EnemyAttackKind.ComboAssault, 1);
            Check(Near01(halved, EnemyAttackKind.ComboAssault, 20f / 80f), "R8 직전 기술 확률 절반");
            Check(!Freq(4.5f, true, EnemyAttackKind.ComboAssault, 2).ContainsKey(EnemyAttackKind.ComboAssault), "R8 같은 기술 3연속 금지");
            Check(m.Choose(2f, true, EnemyAttackKind.Sweep, 1, 0f, 0.1f) == EnemyAttackKind.RetreatShot && m.Choose(2f, true, EnemyAttackKind.Sweep, 1, 0f, 0.9f) != EnemyAttackKind.RetreatShot,
                "R8 큰 근접 공격 뒤 3.5m 안: 40% 확률 후퇴 사격");
            Check(Mathf.Abs(m.Get(EnemyAttackKind.Sweep).telegraphSeconds - 0.35f) < 0.001f && Mathf.Abs(m.Get(EnemyAttackKind.LeapSlam).recoverySeconds - 0.7f) < 0.001f,
                "R8 적 예고·후딜 ×0.7 (휘두르기 예고 0.35초, 도약 후딜 0.7초)");
            Check(m.attacks.Select(a => a.kind).Distinct().Count() == System.Enum.GetValues(typeof(EnemyAttackKind)).Length, "R8 모든 기술 데이터 존재(10종)");

            var fan = SwordPrototype.Enemy.AttackShape.Fan(3.5f, 140f);
            Check(fan.Contains(Vector3.zero, Vector3.forward, new Vector3(0, 0, 3f)) && fan.Contains(Vector3.zero, Vector3.forward, new Vector3(2f, 0, 1f))
                && !fan.Contains(Vector3.zero, Vector3.forward, new Vector3(0, 0, -2f)) && !fan.Contains(Vector3.zero, Vector3.forward, new Vector3(0, 0, 4.5f)), "부채꼴 판정");
            var line = SwordPrototype.Enemy.AttackShape.Line(1.5f, 8f);
            Check(line.Contains(Vector3.zero, Vector3.forward, new Vector3(0.7f, 0, 7f)) && !line.Contains(Vector3.zero, Vector3.forward, new Vector3(1.5f, 0, 4f))
                && !line.Contains(Vector3.zero, Vector3.forward, new Vector3(0, 0, 9f)), "직선 판정");
            var circle = SwordPrototype.Enemy.AttackShape.Circle(4f);
            Check(circle.Contains(Vector3.zero, Vector3.forward, new Vector3(3f, 2f, 2f)) && !circle.Contains(Vector3.zero, Vector3.forward, new Vector3(4f, 0, 2f)), "원형 판정(높이 무시)");
            Check(circle.Contains(Vector3.zero, Vector3.forward, new Vector3(4.2f, 0, 0), 0.35f), "몸 반경 포함");
        }

        private static void CheckPresentation(StatTuning t)
        {
            var stance = new SwordStance();
            Check(stance.Current == AttackDirection.DownRight && stance.IsSpin(AttackDirection.DownRight), "기본 끝자세 ↘");
            stance.Commit(AttackDirection.UpRight);
            Check(stance.IsSpin(AttackDirection.UpRight) && !stance.IsSpin(AttackDirection.DownLeft), "같은 방향 → 회전 베기");
            Check(stance.IsSpin(AttackDirection.DownRight) && stance.IsSpin(AttackDirection.Right) && !stance.IsSpin(AttackDirection.Up) && !stance.IsSpin(AttackDirection.Left),
                "R8 같은 쪽 묶음(↗→↘)끼리 → 회전 베기, ↑·반대쪽은 가로지름");
            var vs = new CharacterStats(2, 2, 3, 2, 1);   // 속도 3 → 3타
            Check(ComboRules.CanReserve(new List<AttackDirection> { AttackDirection.Up }, AttackDirection.Up, vs, true) == ReserveResult.VerticalRepeat
                && ComboRules.CanReserve(new List<AttackDirection> { AttackDirection.Down }, AttackDirection.Down, vs, true) == ReserveResult.VerticalRepeat
                && ComboRules.CanReserve(new List<AttackDirection> { AttackDirection.Up }, AttackDirection.Down, vs, true) == ReserveResult.Accepted
                && ComboRules.CanReserve(new List<AttackDirection> { AttackDirection.Left }, AttackDirection.Left, vs, true) == ReserveResult.Accepted, "R8 ↑↑·↓↓ 연속 예약 불가, ↑↓ 크게 베기 · ←← 가능");
            Check(!stance.TickRealtime(1.9f) && stance.Current == AttackDirection.UpRight, "2초 전 유지");
            Check(stance.TickRealtime(0.2f) && stance.Current == AttackDirection.DownRight, "2초 후 기본 자세 복귀");
            Check(stance.Predict(new List<AttackDirection> { AttackDirection.Left }) == AttackDirection.Left && stance.Predict(new List<AttackDirection>()) == AttackDirection.DownRight, "예약 반영 끝자세");

            StrikeOutcome Hit(AttackDirection d) => new StrikeOutcome { direction = d, success = true };
            Check(SwordPrototype.Presentation.ReactionLibrary.ChooseSuccess(Hit(AttackDirection.Left)) == SwordPrototype.Presentation.SuccessReaction.SideStagger
                && SwordPrototype.Presentation.ReactionLibrary.ChooseSuccess(Hit(AttackDirection.DownLeft)) == SwordPrototype.Presentation.SuccessReaction.KneeBuckle
                && SwordPrototype.Presentation.ReactionLibrary.ChooseSuccess(Hit(AttackDirection.Up)) == SwordPrototype.Presentation.SuccessReaction.LeanBack, "성공 반응: 방향별");
            var crit = Hit(AttackDirection.Left); crit.critical = true;
            var counter = Hit(AttackDirection.Up); counter.counter = true;
            Check(SwordPrototype.Presentation.ReactionLibrary.ChooseSuccess(crit) == SwordPrototype.Presentation.SuccessReaction.Tumble
                && SwordPrototype.Presentation.ReactionLibrary.ChooseSuccess(counter) == SwordPrototype.Presentation.SuccessReaction.Tumble, "성공 반응: 치명타·반격 → 크게 튕김");

            var f = new StrikeOutcome { direction = AttackDirection.Left, sameDirectionSecond = true, sideBlocked = true, fromBehind = true, damageToPlayer = 3f };
            Check(SwordPrototype.Presentation.ReactionLibrary.ChooseFail(f) == SwordPrototype.Presentation.FailReaction.Deflect, "실패 우선 1: 흘려냄");
            f.sameDirectionSecond = false;
            Check(SwordPrototype.Presentation.ReactionLibrary.ChooseFail(f) == SwordPrototype.Presentation.FailReaction.Sidestep, "실패 우선 2: 비켜 피함");
            f.sideBlocked = false;
            Check(SwordPrototype.Presentation.ReactionLibrary.ChooseFail(f) == SwordPrototype.Presentation.FailReaction.BodyBlock, "실패 우선 3: 몸으로 막음");
            f.fromBehind = false;
            Check(SwordPrototype.Presentation.ReactionLibrary.ChooseFail(f) == SwordPrototype.Presentation.FailReaction.Struggle, "실패 우선 4: 힘겨루기");
            f.damageToPlayer = 0f;
            Check(SwordPrototype.Presentation.ReactionLibrary.ChooseFail(f) == SwordPrototype.Presentation.FailReaction.Clash, "실패 기본: 맞부딪힘");
            Check(SwordPrototype.Presentation.ReactionLibrary.ChooseCrit(AttackDirection.Right) == SwordPrototype.Presentation.CritMotion.DoubleCut
                && SwordPrototype.Presentation.ReactionLibrary.ChooseCrit(AttackDirection.UpLeft) == SwordPrototype.Presentation.CritMotion.SpinThrust
                && SwordPrototype.Presentation.ReactionLibrary.ChooseCrit(AttackDirection.Down) == SwordPrototype.Presentation.CritMotion.ShoulderCut, "치명타 모션: 방향별");

            var three = new List<StrikeOutcome> { Hit(AttackDirection.Up), Hit(AttackDirection.Right), Hit(AttackDirection.DownLeft) };
            var withSpin = new List<StrikeOutcome> { Hit(AttackDirection.UpRight), Hit(AttackDirection.UpRight), Hit(AttackDirection.DownLeft) };
            Check(SwordPrototype.Presentation.ReactionLibrary.EstimateSeconds(three, AttackDirection.DownRight) <= 1.5f
                && SwordPrototype.Presentation.ReactionLibrary.EstimateSeconds(withSpin, AttackDirection.DownRight) <= 1.5f, "3타 + 분리 ≤ 1.5초");

            var flags = CombatResolver.Resolve(new List<AttackDirection> { AttackDirection.Left, AttackDirection.Left }, new CharacterStats(2, 2, 2, 2, 2), t, false, new SideBiasTracker(), 0f, () => 0.999f);
            Check(!flags[0].sameDirectionSecond && flags[1].sameDirectionSecond && flags[1].fromBehind, "판정 결과에 반응용 기록");
        }

        private static void CheckJumpAndProps(StatTuning t)
        {
            float v = PlayerJump.LaunchSpeed(1.2f, 0.55f), g = PlayerJump.GravityFor(1.2f, 0.55f);
            Check(Mathf.Abs(v * v / (2f * g) - 1.2f) < 0.001f && Mathf.Abs(2f * v / g - 0.55f) < 0.001f, "점프: 정점 1.2m · 체공 0.55초");

            var m = new EnemyMoveset();
            Check(m.Get(EnemyAttackKind.Sweep).groundOnly && m.Get(EnemyAttackKind.LeapSlam).groundOnly
                && !m.Get(EnemyAttackKind.ChargeSlash).groundOnly && !m.Get(EnemyAttackKind.Throw).groundOnly && !m.Get(EnemyAttackKind.RetreatShot).groundOnly,
                "점프로 피함: 휘두르기·도약 / 못 피함: 돌진·투사체");

            var s = new CharacterStats(2, 2, 2, 2, 2);
            var none = new List<AttackDirection>();
            float groundDown = CombatResolver.PreviewChance(none, AttackDirection.DownLeft, s, t, true, new SideBiasTracker(), 0f, false);
            float airDown = CombatResolver.PreviewChance(none, AttackDirection.DownLeft, s, t, true, new SideBiasTracker(), 0f, true);
            float airUp = CombatResolver.PreviewChance(none, AttackDirection.Up, s, t, true, new SideBiasTracker(), 0f, true);
            Check(Near(airDown - groundDown, 0.10f) && Near(airUp, groundDown), "공중 베기: ↓↙↘만 +10%");

            var made = new List<SwordPrototype.World.DestructibleProp>();
            SwordPrototype.World.DestructibleProp Prop(Vector3 at)
            {
                var p = new GameObject("PropProbe").AddComponent<SwordPrototype.World.DestructibleProp>();
                p.transform.position = at;
                p.Configure(0.8f, 4f, 5);
                made.Add(p);
                return p;
            }
            var near = Prop(new Vector3(3f, 2f, 0f));
            var far = Prop(new Vector3(6f, 2f, 0f));
            var ahead = Prop(new Vector3(0f, 2f, 5f));
            int broken = SwordPrototype.World.PropBreaker.BreakInShape(made, AttackShape.Circle(4f), Vector3.zero, Vector3.forward);
            Check(broken == 1 && near.Broken && !far.Broken && !ahead.Broken, "원형 공격 범위 안 소품만 파괴");
            Check(ahead.Blocks(new Vector3(0.3f, 1.3f, 5f)) && !ahead.Blocks(new Vector3(0f, 1.3f, 6.2f)) && !ahead.Blocks(new Vector3(0f, 4.5f, 5f)), "투사체 엄폐 판정");
            SwordPrototype.World.PropBreaker.BreakInShape(made, AttackShape.Line(1.5f, 8f), Vector3.zero, Vector3.forward);
            Check(ahead.Broken && !far.Broken && !ahead.Blocks(new Vector3(0f, 1.3f, 5f)), "직선 돌진 경로 소품 파괴 · 부서진 소품은 엄폐 안 함");
            foreach (var p in made) UnityEngine.Object.DestroyImmediate(p.gameObject);
        }

        /// <summary>R8: 합 공격 방어(패턴·속도 스탯·입력·시간 초과) · 적 적응 · 재진입 대기 값.</summary>
        private static void CheckDefenseAndPacing()
        {
            var t = new StatTuning();
            var rng = new System.Random(7);
            bool noRepeat = true;
            for (int k = 0; k < 200; k++)
            {
                var p = DefenseSession.MakePattern(() => (float)rng.NextDouble());
                noRepeat &= p.Length == 3 && p[0] != p[1] && p[1] != p[2];
            }
            Check(noRepeat, "R8 합 공격 3칸 · 바로 앞과 같은 칸 없음");
            // CharacterStats(체력, 기력, 속도, 힘, 특수기)
            var slow = new DefenseSession(new CharacterStats(2, 2, 0, 3, 3), t, () => 0.1f);
            var fast = new DefenseSession(new CharacterStats(2, 2, 3, 2, 1), t, () => 0.1f);
            Check(Near(slow.FlashSeconds, 0.25f) && Near(slow.InputSeconds, 1.4f) && Near(fast.FlashSeconds, 0.32f) && Near(fast.InputSeconds, 1.85f),
                "R8 속도 스탯이 높을수록 깜빡임·입력 시간 약간 길게(속도0 0.25/1.4초 · 속도3 0.32/1.85초)");

            DefenseSession Play(bool[] correct)
            {
                var d = new DefenseSession(new CharacterStats(2, 2, 2, 2, 2), t, () => 0.3f);
                d.SetCursor(SelectionSession.ToVector(d.Pattern[0]));
                d.Click();   // 보여주기 중 클릭은 무시
                d.Tick(d.ShowSeconds + 0.01f);
                for (int i = 0; i < correct.Length; i++)
                {
                    var target = d.Pattern[i];
                    d.SetCursor(SelectionSession.ToVector(correct[i] ? target : (AttackDirection)(((int)target + 4) % 8)));
                    d.Click();
                }
                return d;
            }
            var perfect = Play(new[] { true, true, true });
            var miss = Play(new[] { true, false, true });
            Check(perfect.Done && perfect.AllBlocked && perfect.Results.Count == 3, "R8 같은 순서로 3개 → 모두 막음(보여주기 중 클릭 무시)");
            Check(miss.Done && !miss.AllBlocked && miss.Results[0] && !miss.Results[1] && miss.Results[2], "R8 틀린 칸은 그 타만 맞음");
            var late = Play(new[] { true });
            late.Tick(late.InputSeconds + 0.01f);
            Check(late.Done && late.Results.Count == 3 && late.Results[0] && !late.Results[1] && !late.Results[2], "R8 입력 시간 초과 → 남은 타 맞음");

            var a = new EnemyAdaptation();
            for (int i = 0; i < 7; i++) a.Add(t.adaptPerHit, t);
            bool capped = Near(a.Value, 0.25f);
            a.Tick(1.5f, t);
            bool recovered = Mathf.Abs(a.Value - 0.20f) < 0.001f;
            var b = new EnemyAdaptation(); b.Add(t.adaptCounterHit, t);
            Check(capped && recovered && Near(b.Value, 0.025f), "R8 적 적응: 성공 1타 +5% · 최대 25% · 1.5초에 5% 회복 · 방어 보상 반격은 +2.5%");
            var s = new CharacterStats(2, 2, 2, 2, 2);
            float plain = CombatResolver.PreviewChance(new List<AttackDirection>(), AttackDirection.Left, s, t, true, new SideBiasTracker(), 0f);
            float adapted = CombatResolver.PreviewChance(new List<AttackDirection>(), AttackDirection.Left, s, t, true, new SideBiasTracker(), 0f, false, null, 0.1f);
            Check(Mathf.Abs(plain - adapted - 0.1f) < 0.001f, "R8 적 적응이 성공률에서 빠짐(표시 = 판정)");
            Check(Near(t.attackReentrySeconds, 1.2f), "R8 공격 재진입 대기 1.2초");
        }

        /// <summary>R6: 겨눔 자세 표(팔이 닿는 위치·묶음), 하체 방향·뒷걸음, 적 베임 단계.</summary>
        private static void CheckKamaeAndWounds()
        {
            var shoulder = new Vector3(0.18f, 1.45f, 0f);   // 키 1.8m 마네킹 오른 어깨 근처
            bool reach = true, families = true;
            var P = SwordPrototype.Presentation.Kamae.Family.Low;
            for (int i = 0; i < 8; i++)
            {
                var pose = SwordPrototype.Presentation.Kamae.Of((AttackDirection)i);
                float d = Vector3.Distance(pose.hand, shoulder);
                reach &= d >= 0.15f && d <= 0.62f && Mathf.Approximately(pose.blade.magnitude, 1f);
                var expected = i == 0 || i == 1 || i == 7 ? SwordPrototype.Presentation.Kamae.Family.High
                    : i == 2 || i == 6 ? SwordPrototype.Presentation.Kamae.Family.Middle : P;
                families &= pose.family == expected
                    && (expected != P || pose.hand.y < 1.05f)
                    && (expected != SwordPrototype.Presentation.Kamae.Family.High || pose.hand.y > 1.4f);
            }
            Check(reach, "R6 겨눔 자세 8개 모두 팔이 닿는 손 위치(어깨에서 0.15~0.62m)");
            Check(families, "R6 묶음: ↓↙↘ 하단·와키 / ↑↖↗ 상단·팔상 / ←→ 중단");


            StrikeOutcome O(bool second = false, float dmg = 10f, bool crit = false, bool counter = false, bool dash = false)
                => new StrikeOutcome { success = true, sameDirectionSecond = second, damageToEnemy = dmg, critical = crit, counter = counter, dashSlash = dash };
            Check(SwordPrototype.Presentation.Kamae.WoundTier(O()) == 0 && SwordPrototype.Presentation.Kamae.WoundTier(O(second: true)) == 1
                && SwordPrototype.Presentation.Kamae.WoundTier(O(dmg: 16f)) == 1 && SwordPrototype.Presentation.Kamae.WoundTier(O(crit: true)) == 2
                && SwordPrototype.Presentation.Kamae.WoundTier(O(counter: true)) == 2 && SwordPrototype.Presentation.Kamae.WoundTier(O(dash: true)) == 2,
                "R6 베임 단계: 일반 = 스침 · 둘째 타/큰 피해 = 베임 · 치명타/반격/지나가며 베기 = 깊게");
        }

        /// <summary>R5 검 자세: 각도 단계·보정값, 표시 = 판정, 확정 성공 유지, 상성표 교체, 움직임·점프·대쉬 → 자세.</summary>
        private static void CheckStance()
        {
            var t = new StatTuning();
            const AttackDirection U = AttackDirection.Up, UR = AttackDirection.UpRight, R = AttackDirection.Right, DR = AttackDirection.DownRight,
                D = AttackDirection.Down, DL = AttackDirection.DownLeft, L = AttackDirection.Left, UL = AttackDirection.UpLeft;
            Check(StatEffects.SwingSteps(UR, DL) == 4 && StatEffects.SwingSteps(UR, D) == 3 && StatEffects.SwingSteps(UR, DR) == 2
                && StatEffects.SwingSteps(UR, U) == 1 && StatEffects.SwingSteps(UR, R) == 1 && StatEffects.SwingSteps(UR, UR) == 0 && StatEffects.SwingSteps(U, UL) == 1, "R5 칸 수(0~4, 한 바퀴 이어짐)");
            Check(Mathf.Approximately(StatEffects.StanceBonus(UR, DL, t), 0.10f) && Mathf.Approximately(StatEffects.StanceBonus(UR, D, t), 0.05f)
                && Mathf.Approximately(StatEffects.StanceBonus(UR, DR, t), 0f) && Mathf.Approximately(StatEffects.StanceBonus(UR, U, t), -0.10f)
                && Mathf.Approximately(StatEffects.StanceBonus(UR, UR, t), 0f), "R5 보정 크게 +10 · 넓게 +5 · 보통 0 · 짧게 -10 · 회전 0");

            var s = new CharacterStats(2, 2, 2, 2, 2);
            var none = new List<AttackDirection>();
            float plain = CombatResolver.PreviewChance(none, DL, s, t, true, new SideBiasTracker(), 0f);
            float big = CombatResolver.PreviewChance(none, DL, s, t, true, new SideBiasTracker(), 0f, false, UR);
            float second = CombatResolver.PreviewChance(new List<AttackDirection> { L }, R, s, t, true, new SideBiasTracker(), 0f, false, UR);
            float secondPlain = CombatResolver.PreviewChance(new List<AttackDirection> { L }, R, s, t, true, new SideBiasTracker(), 0f);
            Check(Mathf.Approximately(big - plain, 0.10f) && Mathf.Approximately(second - secondPlain, 0.10f), "R5 첫 타는 선택 시작 자세, 다음 타는 앞 예약 방향 기준");

            var plan = new List<AttackDirection> { D, U, UL };
            var outs = CombatResolver.Resolve(plan, s, t, true, new SideBiasTracker(), 0f, () => 0.5f, false, false, UR);
            bool same = true;
            for (int i = 0; i < plan.Count; i++)
                same &= Mathf.Approximately(outs[i].chance, CombatResolver.PreviewChance(plan.Take(i).ToList(), plan[i], s, t, true, new SideBiasTracker(), 0f, false, UR));
            Check(same, "R5 표시 성공률 = 판정 성공률(3타)");
            var fin = CombatResolver.Resolve(new List<AttackDirection> { U, U, U }, new CharacterStats(1, 1, 3, 2, 3), t, true, new SideBiasTracker(), 0f, () => 1f, false, false, UR);
            var counter = CombatResolver.Resolve(new List<AttackDirection> { UR }, s, t, true, new SideBiasTracker(), 0f, () => 1f, true, false, UR);
            Check(fin[2].success && fin[2].chance >= 1f && counter[0].success, "R5 마무리·반격은 자세와 무관하게 확정 성공");

            var matrix = new StatTuning { stanceMatrix = new float[64] };
            matrix.stanceMatrix[(int)UR * 8 + (int)U] = 0.2f;
            Check(Mathf.Approximately(StatEffects.StanceBonus(UR, U, matrix), 0.2f) && Mathf.Approximately(StatEffects.StanceBonus(UR, DL, matrix), 0f), "R5 상성표 64칸을 채우면 칸 수 표 대신 사용");

            bool opposite = true;
            for (int i = 0; i < 8; i++) opposite &= SwordStance.MoveStance((AttackDirection)i, t) == (AttackDirection)((i + 4) % 8);
            Check(opposite, "R5 기본 매핑: 검은 이동 반대쪽(8방향)");
            var st = new SwordStance();
            bool early = !st.TickRealtime(0.1f, new Vector2(0, 1), false, t) && st.Current == DR;
            bool forward = st.TickRealtime(0.2f, new Vector2(0, 1), false, t) && st.Current == D;
            bool left = st.TickRealtime(0.3f, new Vector2(-1, 0), false, t) && st.Current == R;
            bool back = st.TickRealtime(0.3f, new Vector2(0, -1), false, t) && st.Current == U;
            bool diag = st.TickRealtime(0.3f, new Vector2(-1, 1), false, t) && st.Current == DR;
            Check(early && forward && left && back && diag, "R5 0.25초 유지 후 자세: 앞→↓ · 왼→→ · 뒤→↑ · 앞왼→↘");
            bool air = st.TickRealtime(0.01f, Vector2.zero, true, t) && st.Current == U;
            bool dash = st.ApplyDash(new Vector2(1, 0), t) && st.Current == L;
            st.Commit(UR);
            bool rest = !st.TickRealtime(1.9f, Vector2.zero, false, t) && st.TickRealtime(0.2f, Vector2.zero, false, t) && st.Current == DR;
            Check(air && dash && rest, "R5 점프 → ↑ 즉시 · 오른쪽 대쉬 → ← 즉시 · 멈춤 2초 → ↘");
            Check(SwordPrototype.UI.SelectionWheelView.SwingLabel(UR, DL, t).Contains("크게 +10%") && SwordPrototype.UI.SelectionWheelView.SwingLabel(UR, U, t).Contains("짧게 -10%")
                && SwordPrototype.UI.SelectionWheelView.SwingLabel(UR, DR, t) == "", "R5 선택 휠 크게/짧게 표시(보통은 표시 없음)");
        }

        /// <summary>review R1(메뉴 중 전투 시계 정지) · R2(투사체 경로·높이 판정).</summary>
        private static void CheckReviewFixes()
        {
            // R1: 10초에 메뉴 열고 3초 기다림 → 전투 시각은 그대로, 닫은 뒤 1초 → 1초만 흐름
            BattleClock.Reset();
            float before = BattleClock.NowAt(10f);
            BattleClock.SetPaused(true, 10f);
            bool frozen = Mathf.Approximately(BattleClock.NowAt(13f), before);
            BattleClock.SetPaused(false, 13f);
            bool resumed = Mathf.Approximately(BattleClock.NowAt(14f) - before, 1f);
            BattleClock.SetPaused(true, 20f); BattleClock.Reset();
            Check(frozen && resumed && !BattleClock.Paused, "R1 메뉴 동안 전투 시계 정지 · 재개 후 이어짐 · 초기화 시 정지 해제");

            // R2: 몸 캡슐(발 0, 키 2.1, 반지름 0.35) — 아래/위 구 중심 0.35 / 1.75
            Vector3 a = new Vector3(0, 0.35f, 0), b = new Vector3(0, 1.75f, 0);
            const float r = 0.35f, pr = 0.15f;
            var S = (Func<Vector3, Vector3, Func<Vector3, bool>, Enemy.EnemyProjectile.SweepResult>)((f, to, cover) => Enemy.EnemyProjectile.Sweep(f, to, pr, a, b, r, cover));
            Check(S(new Vector3(0, 1.3f, -0.6f), new Vector3(0, 1.3f, -0.5f), null) == Enemy.EnemyProjectile.SweepResult.Body, "R2 지상 몸통 정면 → 피격");
            Vector3 up = Vector3.up * 1.6f;   // 점프로 발이 1.6m 위
            Check(Enemy.EnemyProjectile.Sweep(new Vector3(0, 1.0f, -3f), new Vector3(0, 1.0f, 3f), pr, a + up, b + up, r, null) == Enemy.EnemyProjectile.SweepResult.None, "R2 점프한 몸 아래로 통과 → 피격 없음(높이 반영)");
            Check(Enemy.EnemyProjectile.Sweep(new Vector3(0, 2.2f, -3f), new Vector3(0, 2.2f, 3f), pr, a + up, b + up, r, null) == Enemy.EnemyProjectile.SweepResult.Body, "R2 점프 중 몸에 닿음 → 피격");
            Check(S(new Vector3(0, 1.3f, -5f), new Vector3(0, 1.3f, 5f), null) == Enemy.EnemyProjectile.SweepResult.Body, "R2 한 프레임에 10m 이동해도 몸 통과 검출");
            Check(S(new Vector3(0, 1.3f, -5f), new Vector3(0, 1.3f, 5f), p => p.z > -2f && p.z < -1.5f) == Enemy.EnemyProjectile.SweepResult.Blocked, "R2 몸 앞 엄폐가 먼저 막음");
            Check(S(new Vector3(0, 1.3f, -5f), new Vector3(0, 1.3f, 5f), p => p.z > 1.5f && p.z < 2f) == Enemy.EnemyProjectile.SweepResult.Body, "R2 몸 뒤 엄폐는 피격을 취소하지 않음");
            Check(S(new Vector3(1.2f, 1.3f, -5f), new Vector3(1.2f, 1.3f, 5f), null) == Enemy.EnemyProjectile.SweepResult.None, "R2 옆으로 빗나감");
        }

        private static void CheckFlow(StatTuning t)
        {
            var a = new SwordPrototype.Flow.StatAllocation();
            Check(a.Hardcore && a.Remaining == 10, "배분 시작: 0포인트 하드코어");
            for (int i = 0; i < 5; i++) a.Increase(SwordPrototype.Flow.StatKind.Health);
            Check(a[SwordPrototype.Flow.StatKind.Health] == 3 && !a.CanIncrease(SwordPrototype.Flow.StatKind.Health), "레벨 상한 3");
            for (int i = 0; i < 3; i++) { a.Increase(SwordPrototype.Flow.StatKind.Speed); a.Increase(SwordPrototype.Flow.StatKind.Strength); a.Increase(SwordPrototype.Flow.StatKind.Special); }
            Check(a.Used == 10 && a.Remaining == 0 && !a.CanIncrease(SwordPrototype.Flow.StatKind.Stamina), "합계 10 상한");
            a.Clear(); a.Decrease(SwordPrototype.Flow.StatKind.Speed);
            Check(a.Used == 0, "0 아래로 내려가지 않음");
            bool presetsOk = true;
            for (int i = 0; i < SwordPrototype.Flow.StatAllocation.Presets.Length; i++)
            {
                a.ApplyPreset(i);
                presetsOk &= a.Used == 10 && a.ToStats().IsValid(out _);
            }
            Check(presetsOk && SwordPrototype.Flow.StatAllocation.Presets.Length == 3, "프리셋 3종 유효(합계 10)");

            var s = new CharacterStats(1, 3, 2, 3, 1);
            Check(SwordPrototype.Flow.RunSetup.TryParse(SwordPrototype.Flow.RunSetup.Format(s), out CharacterStats back) && back.Health == 1 && back.Strength == 3,
                "마지막 스탯 저장 형식 왕복");
            Check(!SwordPrototype.Flow.RunSetup.TryParse("3,3,3,3,3", out _) && !SwordPrototype.Flow.RunSetup.TryParse("x", out _), "잘못된 저장값 거부");

            string hp = SwordPrototype.Flow.StatDescriber.Describe(SwordPrototype.Flow.StatKind.Health, 2, t);
            string sp = SwordPrototype.Flow.StatDescriber.Describe(SwordPrototype.Flow.StatKind.Speed, 1, t);
            string st = SwordPrototype.Flow.StatDescriber.Describe(SwordPrototype.Flow.StatKind.Strength, 3, t);
            string sc = SwordPrototype.Flow.StatDescriber.Describe(SwordPrototype.Flow.StatKind.Special, 0, t);
            Check(hp.Contains("HP 100") && hp.Contains("정면 +5%"), "설명: 체력 2 = " + hp);
            Check(sp.Contains("1타 1.3초") && sp.Contains("대쉬 1.8초"), "설명: 속도 1 = " + sp);
            Check(st.Contains("치명타 15%"), "설명: 힘 3 = " + st);
            Check(sc.Contains("무적 없음"), "설명: 특수기 0 = " + sc);
            var tweaked = new StatTuning(); tweaked.maxHealth = new[] { 50f, 70f, 90f, 130f };
            Check(SwordPrototype.Flow.StatDescriber.Describe(SwordPrototype.Flow.StatKind.Health, 2, tweaked).Contains("HP 90"), "설명이 수치 변경을 따라감");

            var r = new SwordPrototype.Flow.RunRecord();
            r.Begin(10f);
            foreach (bool hit in new[] { true, true, false, true, true, true }) r.RecordStrike(hit);
            r.RecordDamageTaken(15f); r.RecordDamageTaken(8f); r.RecordDamageTaken(0f); r.RecordParry();
            r.Finish(70f);
            Check(r.Strikes == 6 && r.Successes == 5 && r.MaxStreak == 3 && Near(r.SuccessRate, 5f / 6f), "결과 기록: 성공률·최대 연속");
            Check(Near(r.DamageTaken, 23f) && r.Parries == 1 && Near(r.Elapsed(999f), 60f), "결과 기록: 피해·패링·시간");
        }

        private static void CheckFront()
        {
            Check(StatEffects.IsInFront(Vector3.zero, Vector3.forward, new Vector3(1, 0, 2)), "정면 판정");
            Check(!StatEffects.IsInFront(Vector3.zero, Vector3.forward, new Vector3(0, 0, -1)), "후면 판정");
        }
    }
}
