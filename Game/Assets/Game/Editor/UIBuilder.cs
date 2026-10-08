using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using SwordPrototype.UI;

namespace SwordPrototype.Editor
{
    /// <summary>
    /// S8-3: 수묵 UI 생성기. TMP 기본 자원 → 한글 동적 글꼴 에셋(OFL 글꼴) → 코드로 그린 스프라이트 → 스킨 → 화면 배치.
    /// </summary>
    public static class UIBuilder
    {
        public const string Folder = "Assets/Game/UI";
        public const string SkinPath = Folder + "/InkUISkin.asset";
        public const string BodyFontPath = Folder + "/NotoSansKR SDF.asset";
        public const string TitleFontPath = Folder + "/NanumBrushScript SDF.asset";
        public const string FontFolder = "Assets/Game/Art/Fonts/";
        /// <summary>화면에 쓰는 특수 기호(글꼴에 반드시 있어야 함).</summary>
        public const string Symbols = "↑↗→↘↓↙←↖●○·−%";

        public static bool HasTmpEssentials => AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset") != null;

        /// <summary>TMP 기본 자원(셰이더·설정)을 패키지에서 가져온다. 별도 배치 단계로 실행(가져오기가 끝나면 종료).</summary>
        public static void ImportTmpEssentials()
        {
            if (HasTmpEssentials) { Debug.Log("TMP_ESSENTIALS_OK"); if (Application.isBatchMode) EditorApplication.Exit(0); return; }
            AssetDatabase.importPackageCompleted += _ => { Debug.Log("TMP_ESSENTIALS_OK"); if (Application.isBatchMode) EditorApplication.Exit(0); };
            AssetDatabase.importPackageFailed += (_, error) => { Debug.LogError("TMP 기본 자원 가져오기 실패: " + error); if (Application.isBatchMode) EditorApplication.Exit(1); };
            TMP_PackageResourceImporter.ImportResources(true, false, false);
        }

        public static InkUISkin BuildSkin()
        {
            Directory.CreateDirectory(Folder);
            var skin = AssetDatabase.LoadAssetAtPath<InkUISkin>(SkinPath);
            if (skin == null) { skin = ScriptableObject.CreateInstance<InkUISkin>(); AssetDatabase.CreateAsset(skin, SkinPath); }
            skin.body = FontAsset(FontFolder + "NotoSansKR-Variable.ttf", BodyFontPath, "NotoSansKR");
            skin.title = FontAsset(FontFolder + "NanumBrushScript-Regular.ttf", TitleFontPath, "NanumBrushScript");
            // 가변 글꼴의 기본 굵기가 가늘어 글자 면을 조금 두껍게(SDF 확장)
            skin.body.material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.22f);
            skin.panel = Sprite("UIPanel", 128, 128, PanelPixel, new Vector4(36, 36, 36, 36));
            skin.brush = Sprite("UIBrush", 512, 64, BrushPixel, Vector4.zero);
            skin.circle = Sprite("UICircle", 128, 128, CirclePixel, Vector4.zero);
            skin.seal = Sprite("UISeal", 128, 128, SealPixel, Vector4.zero);
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            return skin;
        }

        // 한글은 실행 중 필요한 글자를 만드는 동적 글꼴(여러 장의 아틀라스 허용). 원본 글꼴 파일은 빌드에 함께 들어간다.
        private static TMP_FontAsset FontAsset(string fontPath, string assetPath, string name)
        {
            AssetDatabase.DeleteAsset(assetPath);
            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            asset.name = name + " SDF";
            AssetDatabase.CreateAsset(asset, assetPath);
            asset.atlasTextures[0].name = name + " Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static Sprite Sprite(string name, int width, int height, System.Func<float, float, float, float, Color> pixel, Vector4 border)
        {
            Directory.CreateDirectory(GeneratedArt.Folder);
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    tex.SetPixel(x, y, pixel(x / (float)(width - 1), y / (float)(height - 1), x, y));
            tex.Apply();
            string path = $"{GeneratedArt.Folder}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // 한지: 흰 바탕(이미지 색으로 한지색 입힘) + 가장자리 먹 붓 테두리(들쭉날쭉)
        private static Color PanelPixel(float u, float v, float x, float y)
        {
            float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)) * 128f;
            float wobble = Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 6f;
            if (edge < 3f + wobble * 0.3f) return new Color(0, 0, 0, Mathf.Clamp01(edge / 2f));
            if (edge < 12f + wobble) return new Color(0.12f, 0.11f, 0.10f, 1f);
            float fiber = 1f - Mathf.PerlinNoise(x * 0.6f, y * 0.6f) * 0.06f;
            return new Color(fiber, fiber, fiber, 1f);
        }

        // 붓 획: 가로로 길고 양 끝이 가늘어지며 가장자리가 거칠다.
        private static Color BrushPixel(float u, float v, float x, float y)
        {
            float taper = Mathf.Clamp01(Mathf.Min(u, 1f - u) * 12f);
            float half = 0.42f * Mathf.Lerp(0.55f, 1f, taper) + (Mathf.PerlinNoise(x * 0.05f, 3f) - 0.5f) * 0.12f;
            float d = Mathf.Abs(v - 0.5f);
            float alpha = Mathf.Clamp01((half - d) * 28f) * (0.85f + Mathf.PerlinNoise(x * 0.3f, y * 0.3f) * 0.15f);
            return new Color(1, 1, 1, alpha);
        }

        private static Color CirclePixel(float u, float v, float x, float y)
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
            float rough = (Mathf.PerlinNoise(x * 0.2f, y * 0.2f) - 0.5f) * 0.05f;
            return new Color(1, 1, 1, Mathf.Clamp01((1f - d + rough) * 30f));
        }

        // 낙관: 거친 사각 도장
        private static Color SealPixel(float u, float v, float x, float y)
        {
            float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
            float rough = Mathf.PerlinNoise(x * 0.25f, y * 0.25f) * 0.05f;
            float alpha = Mathf.Clamp01((edge - 0.02f - rough) * 40f) * (0.82f + Mathf.PerlinNoise(x * 0.5f, y * 0.5f) * 0.18f);
            return new Color(1, 1, 1, alpha);
        }

        private static Transform Canvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return go.transform;
        }

        private static void EventSystemObject()
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        /// <summary>전투장 장면: HUD · 선택 휠 · 조작 안내 · 결과 · 일시정지.</summary>
        public static void BuildBattleUI(SwordPrototype.Battle.BattleFlow battle, InkUISkin skin)
        {
            var hud = Canvas("HUDCanvas", 0);
            hud.gameObject.AddComponent<CombatHudView>().Build(battle, skin);
            hud.gameObject.AddComponent<ControlsHintView>().Build(skin);
            var wheel = Canvas("SelectionCanvas", 10);
            wheel.gameObject.AddComponent<SelectionWheelView>().Build(battle, skin);
            var menus = Canvas("MenuCanvas", 20);
            menus.gameObject.AddComponent<ResultView>().Build(battle, skin);
            menus.gameObject.AddComponent<PauseView>().Build(battle, skin);
            EventSystemObject();
        }

        public static void BuildTitleUI(InkUISkin skin)
        {
            var canvas = Canvas("TitleCanvas", 0);
            canvas.gameObject.AddComponent<TitleView>().Build(skin);
            EventSystemObject();
        }
    }
}
