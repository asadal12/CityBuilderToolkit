using System.IO;
using UnityEditor;
using UnityEngine;
using CityBuilderToolkit.Town;
using CityBuilderToolkit.Buildings;
using CityBuilderToolkit.Core;

namespace CityBuilderToolkit.Editor
{
    public static class CityBuilderDemoRunner
    {
        [MenuItem("Tools/City Builder/Generate & Capture Demo Town Preview")]
        public static void GenerateAndCapturePreview()
        {
            string artDir = @"C:\Users\asada\.gemini\antigravity-ide\brain\86876f52-2199-4d2d-b7cd-6b2a7eeb7006";
            if (!Directory.Exists(artDir)) Directory.CreateDirectory(artDir);

            // 1. Create Town Generator
            var townGo = new GameObject("Demo_Procedural_Town");
            var tg = townGo.AddComponent<TownGenerator>();
            tg.blocksX = 3;
            tg.blocksZ = 3;
            tg.blockSizeX = 48f;
            tg.blockSizeZ = 48f;
            tg.roadWidth = 10f;
            tg.sidewalkWidth = 2.5f;
            tg.spawnStreetTrees = true;
            tg.spawnStreetLights = true;
            tg.spawnParkBenches = true;
            tg.randomSeed = 2026;

            tg.GenerateTown();

            // 2. Setup Camera
            var camGo = new GameObject("DemoPreviewCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 2000f;

            // Position elevated overlooking the whole town
            camGo.transform.position = new Vector3(-110f, 75f, -110f);
            camGo.transform.LookAt(new Vector3(0f, 15f, 0f));

            int width = 1280;
            int height = 720;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            try
            {
                cam.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                RenderTexture.active = null;

                byte[] png = tex.EncodeToPNG();
                Object.DestroyImmediate(tex);

                string outPath = Path.Combine(artDir, "city_builder_toolkit_demo_town.png");
                File.WriteAllBytes(outPath, png);
                Debug.Log($"[CityBuilderDemoRunner] Saved demo preview to: {outPath}");

                // Close-up street-level shot
                camGo.transform.position = new Vector3(-35f, 5.5f, -35f);
                camGo.transform.LookAt(new Vector3(0f, 12f, 10f));
                cam.fieldOfView = 55f;

                cam.Render();
                RenderTexture.active = rt;
                var texStreet = new Texture2D(width, height, TextureFormat.RGB24, false);
                texStreet.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texStreet.Apply();
                RenderTexture.active = null;

                byte[] pngStreet = texStreet.EncodeToPNG();
                Object.DestroyImmediate(texStreet);

                string outPathStreet = Path.Combine(artDir, "city_builder_toolkit_street_view.png");
                File.WriteAllBytes(outPathStreet, pngStreet);
                Debug.Log($"[CityBuilderDemoRunner] Saved street view preview to: {outPathStreet}");
            }
            finally
            {
                cam.targetTexture = null;
                RenderTexture.active = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(camGo);
                Object.DestroyImmediate(townGo);
            }
        }

        [MenuItem("Tools/City Builder/Create Standalone Demo Scene")]
        public static void CreateDemoScene()
        {
            string folder = "Assets/CityBuilderToolkit/Scenes";
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string scenePath = $"{folder}/TownGeneratorDemo.unity";

            // Create new scene
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single
            );

            // 1. Directional Sun
            var sunGo = new GameObject("Directional Light");
            var light = sunGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1.0f, 0.96f, 0.90f);
            light.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 2. Procedural Town
            var townGo = new GameObject("Procedural_Town");
            var tg = townGo.AddComponent<TownGenerator>();
            tg.blocksX = 3;
            tg.blocksZ = 3;
            tg.blockSizeX = 50f;
            tg.blockSizeZ = 50f;
            tg.roadWidth = 10f;
            tg.sidewalkWidth = 2.5f;
            tg.spawnStreetTrees = true;
            tg.spawnStreetLights = true;
            tg.spawnParkBenches = true;
            tg.randomSeed = 2026;
            tg.GenerateTown();

            // 3. Free Fly Camera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 2500f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<Demo.FreeFlyCamera>();

            camGo.transform.position = new Vector3(-85f, 45f, -85f);
            camGo.transform.LookAt(new Vector3(0f, 10f, 0f));

            // Save Scene
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[CityBuilderDemoRunner] Saved demo scene to: {scenePath}");
        }
    }
}
