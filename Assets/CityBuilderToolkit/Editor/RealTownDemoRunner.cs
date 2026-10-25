using System.IO;
using UnityEditor;
using UnityEngine;
using CityBuilderToolkit.OSM;

namespace CityBuilderToolkit.Editor
{
    public static class RealTownDemoRunner
    {
        [MenuItem("Tools/City Builder/Generate & Capture Real OSM Town Preview")]
        public static void GenerateAndCapturePreview()
        {
            string artDir = @"C:\Users\asada\.gemini\antigravity-ide\brain\86876f52-2199-4d2d-b7cd-6b2a7eeb7006";
            if (!Directory.Exists(artDir)) Directory.CreateDirectory(artDir);

            string samplePath = Path.GetFullPath(Path.Combine(Application.dataPath, "CityBuilderToolkit", "Samples", "Kaiserslautern_Sample.osm"));
            if (!File.Exists(samplePath))
            {
                Debug.LogError($"[RealTownDemoRunner] Sample file not found at: {samplePath}");
                return;
            }

            // 1. Create Real Town Generator
            var townGo = new GameObject("Real_Town_Kaiserslautern_OSM");
            var gen = townGo.AddComponent<RealTownGenerator>();
            gen.preset = RealTownPreset.KaiserslauternCenter;
            gen.latitude = 49.4215;
            gen.longitude = 7.7420;
            gen.radiusMeters = 600f;
            gen.generateBuildings = true;
            gen.generateRoads = true;
            gen.generateParksAndWater = true;
            gen.spawnStreetFurniture = true;
            gen.maxBuildings = 1000;

            gen.GenerateFromLocalFile(samplePath);

            // 2. Setup Camera for Aerial Overview
            var camGo = new GameObject("RealOsmPreviewCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 3000f;

            // Overview position
            camGo.transform.position = new Vector3(-160f, 110f, -140f);
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

                string outPath = Path.Combine(artDir, "real_osm_town_aerial.png");
                File.WriteAllBytes(outPath, png);
                Debug.Log($"[RealTownDemoRunner] Saved aerial preview to: {outPath}");

                // Close-up perspective inside the real street network
                camGo.transform.position = new Vector3(-35f, 8f, -25f);
                camGo.transform.LookAt(new Vector3(15f, 10f, 30f));
                cam.fieldOfView = 60f;

                cam.Render();
                RenderTexture.active = rt;
                var texStreet = new Texture2D(width, height, TextureFormat.RGB24, false);
                texStreet.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texStreet.Apply();
                RenderTexture.active = null;

                byte[] pngStreet = texStreet.EncodeToPNG();
                Object.DestroyImmediate(texStreet);

                string outPathStreet = Path.Combine(artDir, "real_osm_town_street.png");
                File.WriteAllBytes(outPathStreet, pngStreet);
                Debug.Log($"[RealTownDemoRunner] Saved street perspective to: {outPathStreet}");
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
    }
}
