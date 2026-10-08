using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SwordPrototype.Editor
{
    /// <summary>
    /// S8-2: Quaternius 마네킹(CC0)으로 플레이어·적을 만든다.
    /// 애니메이터 컨트롤러를 코드로 생성하고, 먹빛 재질 · 코드로 만든 삿갓·도포 자락·허리띠 · 직검/대도를 붙인다.
    /// 판정·이동은 캐릭터 루트(기존과 같음)가 하고, 모델은 자식 "Model"이다.
    /// </summary>
    public static class CharacterBuilder
    {
        public const string ControllerPath = "Assets/Game/Art/Generated/FighterAnimator.controller";
        private const float MannequinHeight = 1.94f;   // 가져온 마네킹 실제 높이(ual-clips.txt)
        private const float BaseCharacterHeight = 1.81f;   // R7 기본 모델 키(md/ubc-report.txt 몸 메시 경계)

        /// <summary>R7: 기본 모델(UBC)이 있으면 그것을, 없으면 예전 마네킹을 쓴다(되돌리기 쉽게 마네킹은 남겨 둠).</summary>
        public static bool UseBaseCharacter => BaseCharacterImport.Available;
        public static float ModelHeight => UseBaseCharacter ? BaseCharacterHeight : MannequinHeight;
        private static string ModelPath => UseBaseCharacter ? BaseCharacterImport.BodyPath : CharacterImport.LibraryPath;
        // 의상 메시는 장면 생성마다 한 번만 만들어 두 캐릭터가 공유한다(다시 만들면 앞 캐릭터의 참조가 끊김).
        private static Mesh hatMesh, robeMesh, beltMesh;

        public static bool Available => AssetDatabase.LoadAssetAtPath<GameObject>(CharacterImport.LibraryPath) != null;

        public const string UpperMaskPath = "Assets/Game/Art/Generated/ArmsMask.mask";

        // 첫 라이브러리에서 찾고, 없으면 R6 두 번째 라이브러리(UAL2, CC0)에서 찾는다.
        private static AnimationClip Clip(string name)
            => AssetDatabase.LoadAllAssetsAtPath(CharacterImport.LibraryPath).OfType<AnimationClip>().FirstOrDefault(c => c.name == "Armature|" + name)
            ?? AssetDatabase.LoadAllAssetsAtPath(CharacterImport.Library2Path).OfType<AnimationClip>().FirstOrDefault(c => c.name == "Armature|" + name)
            ?? KayKitImport.Clip(name);   // R8 KayKit(이름에 접두사 없음)

        /// <summary>R6: 두 팔·손가락만 덮는 마스크(몸통·머리·다리는 달리기 동작 그대로).</summary>
        private static AvatarMask ArmsMask()
        {
            AssetDatabase.DeleteAsset(UpperMaskPath);
            var mask = new AvatarMask();
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            {
                var part = (AvatarMaskBodyPart)i;
                bool arms = part == AvatarMaskBodyPart.LeftArm || part == AvatarMaskBodyPart.RightArm
                    || part == AvatarMaskBodyPart.LeftFingers || part == AvatarMaskBodyPart.RightFingers
                    || part == AvatarMaskBodyPart.LeftHandIK || part == AvatarMaskBodyPart.RightHandIK;
                mask.SetHumanoidBodyPartActive(part, arms);
            }
            AssetDatabase.CreateAsset(mask, UpperMaskPath);
            return mask;
        }

        /// <summary>상태 이름은 AnimatorRig.Cue와 맞춘다. 팩에 없는 동작은 가까운 동작으로 대신(md/roadmap.md 대응표).</summary>
        public static AnimatorController BuildController()
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            // R8: 캐릭터 기준 이동 속도(x 오른쪽, z 앞) — 2D 방향 섞기
            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveZ", AnimatorControllerParameterType.Float);
            var layers = controller.layers;
            layers[0].iKPass = true;   // 검 팔 IK
            controller.layers = layers;
            var machine = controller.layers[0].stateMachine;

            var locomotion = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.FreeformDirectional2D;
            tree.blendParameter = "MoveX";
            tree.blendParameterY = "MoveZ";
            tree.AddChild(Clip("Sword_Idle"), Vector2.zero);
            tree.AddChild(Clip("Walk_Loop"), new Vector2(0f, 1.6f));
            tree.AddChild(Clip("Jog_Fwd_Loop"), new Vector2(0f, 4.5f));
            tree.AddChild(Clip("Sprint_Loop"), new Vector2(0f, 7.5f));
            // R8 KayKit(CC0): 옆으로 달리기 · 뒷걸음
            tree.AddChild(Clip("Running_Strafe_Left"), new Vector2(-4.5f, 0f));
            tree.AddChild(Clip("Running_Strafe_Right"), new Vector2(4.5f, 0f));
            tree.AddChild(Clip("Walking_Backwards"), new Vector2(0f, -3f));
            machine.defaultState = locomotion;

            void State(string name, string clip, float speed = 1f)
            {
                var s = machine.AddState(name);
                s.motion = Clip(clip);
                s.speed = speed;
                s.writeDefaultValues = true;
            }
            State("Air", "Jump_Loop");
            State("Land", "Jump_Land", 1.6f);
            State("Swing", "Sword_Attack", 2.4f);
            State("Roll", "Roll", 1.5f);
            State("Crouch", "Crouch_Fwd_Loop");
            State("Guard", "Sword_Block");        // R6: UAL2 실제 검 막기(이전 대체: Sword_Idle)
            State("Knockback", "Hit_Knockback");  // R6: 깊게 베임
            State("Hit", "Hit_A", 1.2f);          // R8 KayKit 피격(이전 Hit_Chest)
            State("HitHead", "Hit_B", 1.2f);      // R8 KayKit 피격(이전 Hit_Head)
            State("Struggle", "Push_Loop");
            // R8 KayKit(CC0) 회피·막기·공격
            State("DodgeBack", "Dodge_Backward");
            State("DodgeLeft", "Dodge_Left");
            State("DodgeRight", "Dodge_Right");
            State("BlockHit", "Melee_Block_Hit", 1.6f);
            State("Spinning", "Melee_2H_Attack_Spinning", 1.4f);
            State("Slice", "Melee_2H_Attack_Slice", 1.6f);
            State("Chop", "Melee_2H_Attack_Chop", 2.2f);
            State("Stab", "Melee_2H_Attack_Stab", 2.4f);
            State("SpinAttack", "Melee_2H_Attack_Spin", 2.6f);
            State("Kick", "Melee_Unarmed_Attack_Kick", 1.6f);
            State("JumpChop", "Melee_1H_Attack_Jump_Chop", 1.6f);
            State("Throw", "Throw", 2f);          // R8 KayKit 던지기(이전 Spell_Simple_Shoot)
            State("Stagger", "Spell_Simple_Enter", 0.6f);
            State("Death", "Death01");
            State("Victory", "Idle_Loop");

            // R6: 두 팔만 검 겨눔 동작으로 덮는 위 층(팔 흔들기 제거). 무게는 AnimatorRig가 조절, 손 위치는 IK.
            controller.AddLayer("Arms");
            var withArms = controller.layers;
            withArms[1].avatarMask = ArmsMask();
            withArms[1].defaultWeight = 0f;
            withArms[1].blendingMode = AnimatorLayerBlendingMode.Override;
            withArms[1].iKPass = true;
            controller.layers = withArms;
            var armsMachine = controller.layers[1].stateMachine;
            var hold = armsMachine.AddState("Hold");
            hold.motion = Clip("Sword_Idle");
            armsMachine.defaultState = hold;
            if (UseBaseCharacter) BaseCharacterDress.Prepare();
            hatMesh = GeneratedArt.Frustum("HatMesh", 0.36f, 0.02f, 0.16f);
            robeMesh = GeneratedArt.Frustum("RobeMesh", 0.30f, 0.17f, 0.55f);
            beltMesh = GeneratedArt.Frustum("BeltMesh", 0.175f, 0.17f, 0.07f);            AssetDatabase.SaveAssets();
            return controller;
        }

        /// <summary>
        /// 캐릭터 루트에 마네킹 모델·의상·검을 붙인다. height는 목표 키(m).
        /// </summary>
        public static GameObject Build(string name, Vector3 location, float height, Material body, Material robe, Material accent, Material blade, bool hat, bool greatBlade, AnimatorController controller)
        {
            var root = new GameObject(name);
            root.transform.position = location;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var model = (GameObject)Object.Instantiate(source, root.transform);
            model.name = "Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            float scale = height / ModelHeight;
            model.transform.localScale = Vector3.one * scale;

            var animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().First();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (UseBaseCharacter)
            {
                // R7: 몸은 먹빛 무복 + 수묵 톤 피부, 플레이어는 짧은 머리(삿갓 아래), 적은 짧게 깎은 머리 + 수염
                BaseCharacterDress.Dress(model, animator, body, hat ? new[] { "Hair_SimpleParted" } : new[] { "Hair_Buzzed", "Hair_Beard" });
                BaseCharacterDress.Trim(animator, robe, scale);   // 옷·피부 경계를 깃·끝동으로 가림(도포 색, 실측 굵기)
            }
            else
                foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>()) r.sharedMaterials = Enumerable.Repeat(body, r.sharedMaterials.Length).ToArray();

            // 의상: 월드 기준으로 놓은 뒤 뼈에 붙인다(뼈 축 방향과 무관하게 바르게 섬).
            float hatLift = UseBaseCharacter ? 0.19f : 0.13f;   // 머리뼈에서 정수리까지(모델마다 다름)
            if (hat && head != null)
                Attach(Piece("Hat", hatMesh, accent), head, head.position + Vector3.up * hatLift * scale, scale);
            if (hips != null)
            {
                Attach(Piece("RobeSkirt", robeMesh, robe), hips, new Vector3(hips.position.x, hips.position.y - 0.5f * scale, hips.position.z), scale);
                Attach(Piece("Belt", beltMesh, accent), hips, hips.position + Vector3.up * 0.02f * scale, scale);
            }

            var socket = Sword(root.transform, greatBlade, blade, accent).transform;
            var rig = model.AddComponent<SwordPrototype.Presentation.AnimatorRig>();
            rig.Configure(root.transform, socket, height);
            return root;
        }

        private static GameObject Piece(string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        private static void Attach(GameObject piece, Transform bone, Vector3 worldPosition, float scale)
        {
            piece.transform.SetPositionAndRotation(worldPosition, Quaternion.identity);
            piece.transform.localScale = Vector3.one * scale;
            piece.transform.SetParent(bone, true);
        }

        /// <summary>직검(가는 날·검수·술) 또는 대도(넓은 날). 검날은 소켓의 +y 방향, 손잡이가 소켓 원점.</summary>
        private static GameObject Sword(Transform parent, bool great, Material bladeMat, Material dark)
        {
            var socket = new GameObject("SwordSocket");
            socket.transform.SetParent(parent, false);
            GameObject Box(string n, Vector3 pos, Vector3 size, Material m)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                b.name = n;
                Object.DestroyImmediate(b.GetComponent<Collider>());
                b.transform.SetParent(socket.transform, false);
                b.transform.localPosition = pos;
                b.transform.localScale = size;
                b.GetComponent<Renderer>().sharedMaterial = m;
                return b;
            }
            if (great)
            {
                Box("Blade", new Vector3(0, .82f, 0), new Vector3(.15f, 1.3f, .025f), bladeMat);
                Box("Guard", new Vector3(0, .14f, 0), new Vector3(.26f, .05f, .09f), dark);
                Box("Grip", new Vector3(0, -.05f, 0), new Vector3(.045f, .34f, .045f), dark);
            }
            else
            {
                Box("Blade", new Vector3(0, .66f, 0), new Vector3(.045f, 1.0f, .012f), bladeMat);
                Box("Guard", new Vector3(0, .13f, 0), new Vector3(.17f, .035f, .05f), dark);
                Box("Grip", new Vector3(0, 0, 0), new Vector3(.032f, .22f, .032f), dark);
                Box("Tassel", new Vector3(0, -.17f, 0), new Vector3(.02f, .12f, .02f), dark);
            }
            return socket;
        }
    }
}
