using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SwordPrototype.Editor
{
    public static class FoundationVerification
    {
        private static double started;
        private static bool failed;
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Game/Scenes/Foundation.unity");
            if (UnityEngine.Object.FindFirstObjectByType<PlayerMovement>() == null ||
                UnityEngine.Object.FindFirstObjectByType<EncounterTrigger>() == null || Camera.main == null)
                throw new Exception("Missing foundation scene components");
            Capture();
            // SessionState survives the domain reload when entering play mode.
            SessionState.SetBool("Sword.VerifyPending", true);
            EditorApplication.isPlaying = true;
        }
        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (!SessionState.GetBool("Sword.VerifyPending", false)) return;
            started = EditorApplication.timeSinceStartup;
            Application.logMessageReceived += OnLog;
            EditorApplication.update += Tick;
        }
        private static void OnLog(string message, string stack, LogType type)
        { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) failed = true; }
        private static void Tick()
        {
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup - started < 5) return;
            SessionState.SetBool("Sword.VerifyPending", false);
            Debug.Log(failed ? "FOUNDATION_RUNTIME_FAILED" : "FOUNDATION_RUNTIME_OK_5_SECONDS");
            EditorApplication.update -= Tick;
            EditorApplication.Exit(failed ? 1 : 0);
        }
        private static void Capture()
        {
            var camera = Camera.main;
            Vector3 previousPosition = camera.transform.position;
            Quaternion previousRotation = camera.transform.rotation;
            camera.transform.position = new Vector3(37, 42, -48);
            camera.transform.LookAt(new Vector3(0, 0, -5));
            var rt = new RenderTexture(1280, 800, 24);
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var texture = new Texture2D(1280,800,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,1280,800),0,0);
            texture.Apply();
            File.WriteAllBytes(Path.GetFullPath("../html/foundation-preview.png"),texture.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(texture);
            camera.transform.SetPositionAndRotation(previousPosition, previousRotation);
        }
    }
}
