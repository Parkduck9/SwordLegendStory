using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SwordPrototype.Editor
{
    public static class PrototypeBuilder
    {
        private static Material stone, teal, red, steel, dark, wood, wall, floor;
        private static SwordPrototype.Audio.SoundBank bank;

        // S8-1: URP Lit 재질. 질감이 있으면 바둑판식으로 깐다.
        private static Material Mat(string name, Color color, Texture2D texture = null, float tiling = 1f)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            material.SetFloat("_Smoothness", 0.08f);
            if (texture != null)
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", Vector2.one * tiling);
            }
            AssetDatabase.CreateAsset(material, "Assets/Game/Art/" + name + ".mat");
            return material;
        }

        private static Material UnlitMat(string name, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = color };
            material.SetFloat("_Cull", 0f);   // 양면(먼 산 고리 안쪽에서 보임)
            AssetDatabase.CreateAsset(material, "Assets/Game/Art/" + name + ".mat");
            return material;
        }

        private static GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material, bool solid = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid) Object.DestroyImmediate(go.GetComponent<Collider>());
            else go.layer = 8;
            return go;
        }

        private static GameObject Fighter(string name, Vector3 location, Material cloth)
        {
            var root = new GameObject(name);
            root.transform.position = location;
            var visual = new GameObject("Visual_Replaceable").transform;
            visual.SetParent(root.transform, false);
            Shape("Torso", PrimitiveType.Cube, visual, new Vector3(0, 1.22f, 0), new Vector3(.65f,.7f,.35f), cloth);
            Shape("Head", PrimitiveType.Cube, visual, new Vector3(0, 1.83f, 0), Vector3.one * .38f, stone);
            Shape("Hair", PrimitiveType.Cube, visual, new Vector3(0, 2.04f, -.03f), new Vector3(.42f,.14f,.42f), dark);
            foreach (float x in new[] { -.2f, .2f })
                Shape("Leg", PrimitiveType.Cube, visual, new Vector3(x,.43f,0), new Vector3(.24f,.8f,.28f), dark);
            Shape("LeftArm", PrimitiveType.Cube, visual, new Vector3(-.45f,1.2f,0), new Vector3(.22f,.65f,.25f), cloth);
            Shape("RightArm", PrimitiveType.Cube, visual, new Vector3(.45f,1.2f,0), new Vector3(.22f,.65f,.25f), cloth);
            var sword = new GameObject("SwordSocket").transform;
            sword.SetParent(visual, false);
            sword.localPosition = new Vector3(.45f,.95f,.15f);
            sword.localRotation = Quaternion.Euler(65,0,0);
            Shape("Blade", PrimitiveType.Cube, sword, new Vector3(0,.75f,0), new Vector3(.1f,1.2f,.035f), steel);
            Shape("Guard", PrimitiveType.Cube, sword, new Vector3(0,.15f,0), new Vector3(.36f,.07f,.1f), dark);
            Shape("Grip", PrimitiveType.Cube, sword, Vector3.zero, new Vector3(.08f,.28f,.08f), dark);
            return root;
        }

        // S6 설계 1.0: 파괴 가능한 돌기둥 8개(반경 12m) + 바위 더미 6개(반경 18m). 입구 직선 경로(x=0)는 비운다.
        // S8-1: 충돌체 크기는 그대로, 재질만 수묵풍. CC0 모델 교체는 다운로드 승인 후.
        public const string NatureKit = "Assets/Game/Art/Kenney/NatureKit/";

        private static void BuildProps()
        {
            var rock = Mat("Rock", new Color(.50f,.49f,.46f));
            var pillarStone = Mat("PillarStone", new Color(.42f,.41f,.39f));   // 한지색 바닥과 구분되는 먹빛 돌
            var props = new GameObject("DestructibleProps").transform;
            string letters = "ABCDEFGHIJ";
            for (int i = 0; i < 8; i++)
            {
                float a = (i * 45f + 22.5f) * Mathf.Deg2Rad;
                Prop("StonePillar", props, new Vector3(Mathf.Sin(a) * 12f, 2f, Mathf.Cos(a) * 12f), new Vector3(1.2f, 4f, 1.2f), i * 45f + 22.5f,
                    NatureKit + "stone_tall" + letters[i] + ".fbx", pillarStone).Configure(0.8f, 4f, 5);
            }
            int r = 0;
            foreach (float deg in new[] { 30f, 90f, 150f, 210f, 270f, 330f })
            {
                float a = deg * Mathf.Deg2Rad;
                Prop("RockPile", props, new Vector3(Mathf.Sin(a) * 18f, .7f, Mathf.Cos(a) * 18f), new Vector3(2f, 1.4f, 2f), deg + 20f,
                    NatureKit + "rock_large" + letters[r++] + ".fbx", rock).Configure(1.2f, 1.4f, 6);
            }
        }

        /// <summary>
        /// S8-1: 판정용 상자 충돌체(크기 고정)와 보이는 모델을 분리한다. Kenney CC0 모델을 상자 높이에 맞춰 넣고 수묵 재질로 덮는다.
        /// 모델이 없으면 같은 크기의 상자를 보이게 둔다.
        /// </summary>
        private static SwordPrototype.World.DestructibleProp Prop(string name, Transform parent, Vector3 center, Vector3 size, float yaw, string modelPath, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(center, Quaternion.Euler(0, yaw, 0));
            go.layer = 8;
            go.AddComponent<BoxCollider>().size = size;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                Shape(name + "Visual", PrimitiveType.Cube, go.transform, Vector3.zero, size, material);
                return go.AddComponent<SwordPrototype.World.DestructibleProp>();
            }
            var visual = (GameObject)Object.Instantiate(model, go.transform);
            visual.name = name + "Model";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            Bounds b = Combined(visual);
            // 높이·너비를 각각 상자에 맞춘다(보이는 모습과 충돌이 어긋나 '보이지 않는 벽'이 생기지 않게).
            float horizontal = size.x * 1.1f / Mathf.Max(0.01f, Mathf.Max(b.size.x, b.size.z));
            float vertical = size.y / Mathf.Max(0.01f, b.size.y);
            visual.transform.localScale = new Vector3(horizontal, vertical, horizontal);
            b = Combined(visual);
            // 모델 바닥을 상자 바닥에, 수평 중심을 상자 중심에 맞춘다.
            Vector3 offset = new Vector3(center.x - b.center.x, (center.y - size.y * 0.5f) - b.min.y, center.z - b.center.z);
            visual.transform.position += offset;
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
            {
                var mats = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = material;
                renderer.sharedMaterials = mats;
            }
            return go.AddComponent<SwordPrototype.World.DestructibleProp>();
        }

        private static Bounds Combined(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            Bounds b = renderers.Length > 0 ? renderers[0].bounds : new Bounds(root.transform.position, Vector3.zero);
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        // S8-1: 안개 낀 흐린 저녁 — 낮은 따뜻한 해, 3색 주변광, 한지색 안개.
        private static void BuildLighting()
        {
            var sun = new GameObject("Sun");
            sun.transform.rotation = Quaternion.Euler(22, -40, 0);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, .87f, .72f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = .7f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.62f, .65f, .69f);
            RenderSettings.ambientEquatorColor = new Color(.74f, .71f, .64f);
            RenderSettings.ambientGroundColor = new Color(.36f, .34f, .31f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = RenderSetup.Paper;
            RenderSettings.fogStartDistance = 28f;
            RenderSettings.fogEndDistance = 165f;
            RenderSettings.skybox = null;
        }

        // S8-1: 먼 산 실루엣 3겹(가까울수록 진한 먹색) + 전투장 밖 바닥. 충돌 없음.
        private static void BuildBackdrop()
        {
            var root = new GameObject("Backdrop").transform;
            (string name, float radius, float min, float max, Color color, float seed)[] rings =
            {
                ("MountainsNear", 70f, 8f, 22f, new Color(.23f, .22f, .21f), 3.1f),
                ("MountainsMid", 105f, 16f, 36f, new Color(.42f, .41f, .39f), 7.7f),
                ("MountainsFar", 150f, 26f, 55f, new Color(.62f, .61f, .58f), 12.4f),
            };
            foreach (var r in rings)
            {
                var go = new GameObject(r.name);
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>().sharedMesh = GeneratedArt.MountainRing(r.name, r.radius, r.min, r.max, r.seed);
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = UnlitMat(r.name, r.color);
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            var ground = Shape("OuterGround", PrimitiveType.Cylinder, root, new Vector3(0, -1.2f, 0), new Vector3(320, .1f, 320), Mat("OuterGround", new Color(.66f, .64f, .59f)));
            ground.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // S8-1: 입구 — 나무 기둥 + 들보 + 지붕판, 진입 후 닫히는 목조 문 두 짝(S7 입구 닫힘과 연동).
        private static void BuildGate(EncounterTrigger trigger)
        {
            foreach (float x in new[] { -2.2f, 2.2f })
                Shape("EntrancePillar", PrimitiveType.Cube, null, new Vector3(x, 2, -25), new Vector3(.6f, 4, .6f), wood, true);
            Shape("GateBeam", PrimitiveType.Cube, null, new Vector3(0, 4.15f, -25), new Vector3(5.6f, .35f, .55f), wood);
            Shape("GateRoof", PrimitiveType.Cube, null, new Vector3(0, 4.45f, -25), new Vector3(6.4f, .14f, 1.1f), dark);

            var gate = new GameObject("ArenaGateWall");
            gate.transform.position = new Vector3(0, 1.5f, -26.5f);
            gate.layer = 8;
            var barrier = gate.AddComponent<BoxCollider>();
            barrier.size = new Vector3(4.2f, 3f, .3f);
            Transform Door(string name, float hingeX, float panelOffset)
            {
                var hinge = new GameObject(name).transform;
                hinge.position = new Vector3(hingeX, 0, -26.5f);
                Shape(name + "Panel", PrimitiveType.Cube, hinge, new Vector3(panelOffset, 1.5f, 0), new Vector3(2.05f, 3f, .12f), wood);
                return hinge;
            }
            var left = Door("GateDoorLeft", -2.1f, 1.04f);
            var right = Door("GateDoorRight", 2.1f, -1.04f);
            gate.AddComponent<SwordPrototype.World.ArenaGate>().Configure(trigger, barrier, left, right);
        }

        private static void ConfigureCamera(Camera cam)
        {
            cam.backgroundColor = RenderSetup.Paper;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        }

        private static void BuildPostVolume()
        {
            var volume = new GameObject("InkWashVolume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(RenderSetup.ProfilePath);
        }

        [MenuItem("Sword Prototype/Create Foundation Scene")]
        public static void Build()
        {
            RenderSetup.Setup();
            var skin = UIBuilder.BuildSkin();   // S8-3 수묵 UI 자원(글꼴·스프라이트)
            bank = SoundBankBuilder.Build();    // S8-4 소리(합성 + Kenney CC0)
            BuildTitle(skin);
            Directory.CreateDirectory("Assets/Game/Art");
            Directory.CreateDirectory("Assets/Game/Scenes");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // S8-1 수묵 팔레트: 한지·먹색, 붉은 포인트(적·예고)
            stone = Mat("Stone", new Color(.66f,.64f,.60f));
            teal = Mat("PlayerTeal", new Color(.24f,.40f,.44f));
            red = Mat("EnemyRed", new Color(.56f,.14f,.12f));
            steel = Mat("Steel", new Color(.82f,.84f,.86f));
            dark = Mat("Dark", new Color(.16f,.15f,.14f));
            wood = Mat("Wood", Color.white, GeneratedArt.Wood(), 1f);
            wall = Mat("StoneWall", Color.white, GeneratedArt.StoneWall(), 1f);
            floor = Mat("ArenaFloor", Color.white, GeneratedArt.FloorSlabs(), 6f);
            var arena=Shape("CircularArena", PrimitiveType.Cylinder, null, new Vector3(0,-.5f,0), new Vector3(50,.5f,50), floor, true);
            // 기본 원기둥의 캡슐 충돌체는 납작하게 늘리면 반지름 25m 구가 되어 입구를 막는다 → 원기둥 메시 충돌체로 교체.
            Object.DestroyImmediate(arena.GetComponent<Collider>());
            arena.AddComponent<MeshCollider>().sharedMesh=arena.GetComponent<MeshFilter>().sharedMesh;
            Shape("ApproachPath", PrimitiveType.Cube, null, new Vector3(0,-.5f,-27.5f), new Vector3(4,1,5.5f), stone, true);
            for (int i=0;i<36;i++)
            {
                float angle=i*Mathf.PI*2/36;
                if (Mathf.Cos(angle)<-.98f) continue;
                Vector3 pos=new Vector3(Mathf.Sin(angle)*25.5f,.65f,Mathf.Cos(angle)*25.5f);
                var boundary=Shape("ArenaBoundary",PrimitiveType.Cube,null,pos,new Vector3(4.5f,1.3f,1),wall,true);
                boundary.transform.rotation=Quaternion.Euler(0,angle*Mathf.Rad2Deg,0);
            }
            BuildProps();
            foreach(float x in new[]{-2.3f,2.3f})
                Shape("PathRail",PrimitiveType.Cube,null,new Vector3(x,.6f,-27.5f),new Vector3(.5f,1.2f,5.5f),wall,true);
            Shape("PathEnd",PrimitiveType.Cube,null,new Vector3(0,.6f,-30.4f),new Vector3(5,1.2f,.5f),wall,true);
            // S8-2: Quaternius 마네킹이 있으면 애니메이션 캐릭터, 없으면 블록 인형.
            bool animated = CharacterBuilder.Available;
            var controller = animated ? CharacterBuilder.BuildController() : null;
            var ink = Mat("InkBody", new Color(.17f,.16f,.15f));
            var strawHat = Mat("StrawHat", new Color(.36f,.32f,.25f));
            var player = animated
                ? CharacterBuilder.Build("Player", new Vector3(0,.1f,-30), 1.8f, ink, teal, strawHat, steel, true, false, controller)
                : Fighter("Player",new Vector3(0,.1f,-30),teal);
            var body=player.AddComponent<CharacterController>();
            body.height=2.1f; body.radius=.35f; body.center=new Vector3(0,1.05f,0);
            player.AddComponent<PlayerMovement>();
            player.AddComponent<SwordPrototype.Battle.Health>();
            player.AddComponent<SwordPrototype.Battle.PlayerStats>();
            player.AddComponent<SwordPrototype.Battle.PlayerDash>();
            player.AddComponent<SwordPrototype.Battle.PlayerJump>();
            if (!animated) player.AddComponent<SwordPrototype.Presentation.FighterRig>();
            var enemy = animated
                ? CharacterBuilder.Build("Enemy_StationaryPlaceholder", Vector3.zero, 2.25f, ink, red, dark, steel, false, true, controller)
                : Fighter("Enemy_StationaryPlaceholder",Vector3.zero,red);
            enemy.transform.rotation=Quaternion.Euler(0,180,0);
            enemy.AddComponent<SwordPrototype.Battle.Health>().SetMax(220f);   // S4 설계 1.0: 적 체력 220
            enemy.AddComponent<SwordPrototype.Enemy.EnemyPose>();
            if (!animated) enemy.AddComponent<SwordPrototype.Presentation.FighterRig>();
            var gate=new GameObject("ArenaEntranceTrigger");
            gate.transform.position=new Vector3(0,1.5f,-24.8f);
            var box=gate.AddComponent<BoxCollider>();box.size=new Vector3(4,3,.5f);box.isTrigger=true;
            var rb=gate.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
            var trigger=gate.AddComponent<EncounterTrigger>();
            var cameraGo=new GameObject("Main Camera");cameraGo.tag="MainCamera";
            var cam=cameraGo.AddComponent<Camera>();cam.fieldOfView=60;cam.farClipPlane=260;
            ConfigureCamera(cam);
            cameraGo.AddComponent<AudioListener>();
            var follow=cameraGo.AddComponent<ThirdPersonCamera>();follow.Configure(player.transform,enemy.transform);
            cameraGo.transform.position=player.transform.position+new Vector3(0,4,-6);cameraGo.transform.LookAt(player.transform.position+Vector3.up);
            BuildLighting();
            BuildBackdrop();
            BuildPostVolume();
            var battle=new GameObject("BattleFlow").AddComponent<SwordPrototype.Battle.BattleFlow>();
            battle.Configure(player.transform,enemy.transform,follow,trigger);
            enemy.AddComponent<SwordPrototype.Enemy.EnemyBrain>().Configure(battle,player.transform,trigger);
            // S7: 입구 닫힘(진입 트리거보다 뒤쪽 진입로) · 결과 화면 · 일시정지 / S8-1: 목조 문
            BuildGate(trigger);
            // S8-3: HUD · 선택 휠 · 조작 안내 · 결과 · 일시정지(uGUI + TextMeshPro)
            UIBuilder.BuildBattleUI(battle, skin);
            SoundBankBuilder.AddService(bank, SwordPrototype.Audio.Sfx.BgmBattle);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/Game/Scenes/Foundation.unity");
            EditorBuildSettings.scenes=new[]{
                new EditorBuildSettingsScene("Assets/Game/Scenes/Title.unity",true),
                new EditorBuildSettingsScene("Assets/Game/Scenes/Foundation.unity",true)};
            AssetDatabase.SaveAssets();
            Debug.Log("FOUNDATION_BUILD_OK");
        }

        /// <summary>S7 타이틀 장면: 카메라·조명·TitleMenu. 전투장 장면보다 먼저 만든다(빌드 목록 0번).</summary>
        [MenuItem("Sword Prototype/Create Title Scene")]
        public static void BuildTitle() => BuildTitle(UIBuilder.BuildSkin());

        public static void BuildTitle(SwordPrototype.UI.InkUISkin skin)
        {
            Directory.CreateDirectory("Assets/Game/Scenes");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraGo=new GameObject("Main Camera");cameraGo.tag="MainCamera";
            var cam=cameraGo.AddComponent<Camera>();
            ConfigureCamera(cam);
            cameraGo.AddComponent<AudioListener>();
            UIBuilder.BuildTitleUI(skin);
            SoundBankBuilder.AddService(bank != null ? bank : SoundBankBuilder.Build(), SwordPrototype.Audio.Sfx.BgmTitle);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/Game/Scenes/Title.unity");
            Debug.Log("TITLE_BUILD_OK");
        }
    }
}
