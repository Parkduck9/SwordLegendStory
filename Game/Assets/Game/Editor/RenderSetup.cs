using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SwordPrototype.Editor
{
    /// <summary>
    /// S8-1: URP 파이프라인 에셋·렌더러·먹선 기능·후처리 프로필을 만들고 프로젝트에 지정한다.
    /// 여러 번 실행해도 같은 경로의 에셋을 다시 만든다.
    /// </summary>
    public static class RenderSetup
    {
        public const string Folder = "Assets/Game/Rendering";
        public const string PipelinePath = Folder + "/GeomHyeop_URP.asset";
        public const string RendererPath = Folder + "/GeomHyeop_URP_Renderer.asset";
        public const string OutlineMaterialPath = Folder + "/InkOutline.mat";
        public const string ProfilePath = Folder + "/InkWashVolume.asset";

        // 수묵 팔레트(시안과 같은 값)
        public static readonly Color Paper = new Color(0.86f, 0.83f, 0.76f);
        public static readonly Color Ink = new Color(0.12f, 0.11f, 0.10f);

        [MenuItem("Sword Prototype/Setup URP (S8-1)")]
        public static UniversalRenderPipelineAsset Setup()
        {
            System.IO.Directory.CreateDirectory(Folder);
            AssetDatabase.DeleteAsset(PipelinePath);
            AssetDatabase.DeleteAsset(RendererPath);

            // 기본 후처리 자원이 채워진 렌더러를 URP 내부 생성 함수로 만든다(메뉴 "URP Asset (with Universal Renderer)"와 같은 경로).
            MethodInfo createRenderer = typeof(UniversalRenderPipelineAsset).GetMethod("CreateRendererAsset", BindingFlags.NonPublic | BindingFlags.Static);
            var renderer = (UniversalRendererData)createRenderer.Invoke(null, new object[] { RendererPath, RendererType.UniversalRenderer, false, "Renderer" });

            AddOutline(renderer);

            var pipeline = UniversalRenderPipelineAsset.Create(renderer);
            pipeline.supportsCameraDepthTexture = true;
            pipeline.shadowDistance = 60f;
            pipeline.msaaSampleCount = 4;
            AssetDatabase.CreateAsset(pipeline, PipelinePath);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);
            CreateVolumeProfile();
            AssetDatabase.SaveAssets();
            Debug.Log("URP_SETUP_OK");
            return pipeline;
        }

        private static void AddOutline(UniversalRendererData renderer)
        {
            var shader = Shader.Find("Hidden/GeomHyeop/InkOutline");
            AssetDatabase.DeleteAsset(OutlineMaterialPath);
            var material = new Material(shader) { name = "InkOutline" };
            material.SetColor("_InkColor", Ink);
            AssetDatabase.CreateAsset(material, OutlineMaterialPath);

            var feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            feature.name = "InkOutline";
            feature.passMaterial = material;
            feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
            feature.requirements = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
            feature.fetchColorBuffer = true;
            AssetDatabase.AddObjectToAsset(feature, renderer);
            renderer.rendererFeatures.Add(feature);

            // 기능 목록과 하위 에셋 연결표(m_RendererFeatureMap)를 맞춘다.
            var so = new SerializedObject(renderer);
            var map = so.FindProperty("m_RendererFeatureMap");
            map.arraySize = renderer.rendererFeatures.Count;
            for (int i = 0; i < renderer.rendererFeatures.Count; i++)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(renderer.rendererFeatures[i], out _, out long id);
                map.GetArrayElementAtIndex(i).longValue = id;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            renderer.SetDirty();
            EditorUtility.SetDirty(renderer);
        }

        /// <summary>색보정(채도↓·따뜻한 색조) · 약한 블룸 · 비네트 · 톤매핑.</summary>
        private static void CreateVolumeProfile()
        {
            AssetDatabase.DeleteAsset(ProfilePath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);

            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(-28f);
            color.contrast.Override(8f);
            color.colorFilter.Override(new Color(1f, 0.96f, 0.9f));
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.35f);
            bloom.threshold.Override(1.0f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.5f);
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);

            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(profile);
        }
    }
}
