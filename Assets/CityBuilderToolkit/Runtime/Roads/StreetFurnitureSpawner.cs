using System.Collections.Generic;
using UnityEngine;
using CityBuilderToolkit.Core;
using CityBuilderToolkit.Materials;

namespace CityBuilderToolkit.Roads
{
    /// <summary>
    /// Procedurally spawns or generates street furniture:
    /// trees, modern street lamp posts with light components, benches, and fire hydrants.
    /// Operates stand-alone with built-in procedural models, or accepts user prefabs.
    /// </summary>
    public static class StreetFurnitureSpawner
    {
        public static GameObject SpawnTree(Transform parent, Vector3 position, float scale = 1.0f)
        {
            var go = new GameObject("StreetTree");
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var mb = new MeshBuilder(2);

            // Trunk (Brown Cylinder/Box)
            float trunkH = 2.4f * scale;
            float trunkR = 0.22f * scale;
            mb.AddBox(0, Vector3.up * (trunkH * 0.5f), new Vector3(trunkR * 2f, trunkH, trunkR * 2f));

            // Foliage Canopy (Green Hexagonal or Box Clustered Canopy)
            float canopyRadius = 1.8f * scale;
            Vector3 canopyCenter = Vector3.up * (trunkH + canopyRadius * 0.8f);

            mb.AddBox(1, canopyCenter, new Vector3(canopyRadius * 2f, canopyRadius * 1.8f, canopyRadius * 2f));
            mb.AddBox(1, canopyCenter + Vector3.up * (canopyRadius * 0.5f),
                new Vector3(canopyRadius * 1.4f, canopyRadius * 1.2f, canopyRadius * 1.4f));

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mb.ToMesh("TreeMesh");

            var mr = go.AddComponent<MeshRenderer>();
            Material trunkMat = CityMaterialPalette.CreateMaterial("Mat_Trunk", new Color(0.35f, 0.22f, 0.12f), 0.1f);
            Material foliageMat = CityMaterialPalette.CreateMaterial("Mat_Foliage", new Color(0.20f, 0.45f, 0.18f), 0.2f);
            mr.sharedMaterials = new Material[] { trunkMat, foliageMat };

            return go;
        }

        public static GameObject SpawnStreetLight(Transform parent, Vector3 position, Quaternion rotation)
        {
            var go = new GameObject("StreetLight");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = rotation;

            var mb = new MeshBuilder(2);

            // Steel Mast / Pole
            float poleH = 6.5f;
            mb.AddBox(0, Vector3.up * (poleH * 0.5f), new Vector3(0.18f, poleH, 0.18f));

            // Horizontal Overhanging Arm
            float armLen = 1.6f;
            Vector3 armCenter = new Vector3(0f, poleH - 0.1f, armLen * 0.5f);
            mb.AddBox(0, armCenter, new Vector3(0.12f, 0.12f, armLen));

            // Luminaire / Lamp Head
            Vector3 headCenter = new Vector3(0f, poleH - 0.25f, armLen);
            mb.AddBox(1, headCenter, new Vector3(0.35f, 0.15f, 0.6f));

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mb.ToMesh("StreetLightMesh");

            var mr = go.AddComponent<MeshRenderer>();
            Material poleMat = CityMaterialPalette.CreateMaterial("Mat_LampPole", new Color(0.22f, 0.25f, 0.28f), 0.5f, 0.5f);
            Material bulbMat = CityMaterialPalette.CreateMaterial("Mat_LampBulb", new Color(1.0f, 0.95f, 0.8f), 0.9f);
            mr.sharedMaterials = new Material[] { poleMat, bulbMat };

            // Optional Light Component
            var lightGo = new GameObject("LightSource");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, poleH - 0.4f, armLen);

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = new Color(1.0f, 0.92f, 0.78f);
            light.intensity = 2.5f;
            light.range = 14f;
            light.spotAngle = 75f;
            lightGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            return go;
        }

        public static GameObject SpawnBench(Transform parent, Vector3 position, Quaternion rotation)
        {
            var go = new GameObject("ParkBench");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = rotation;

            var mb = new MeshBuilder(2);

            // Wooden Slats
            float seatW = 1.8f;
            float seatD = 0.5f;
            float seatH = 0.45f;

            mb.AddBox(0, Vector3.up * seatH, new Vector3(seatW, 0.08f, seatD));
            mb.AddBox(0, new Vector3(0f, seatH + 0.35f, -seatD * 0.45f), new Vector3(seatW, 0.4f, 0.08f));

            // Steel Legs
            mb.AddBox(1, new Vector3(-seatW * 0.4f, seatH * 0.5f, 0f), new Vector3(0.08f, seatH, seatD));
            mb.AddBox(1, new Vector3( seatW * 0.4f, seatH * 0.5f, 0f), new Vector3(0.08f, seatH, seatD));

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mb.ToMesh("BenchMesh");

            var mr = go.AddComponent<MeshRenderer>();
            Material woodMat = CityMaterialPalette.CreateMaterial("Mat_BenchWood", new Color(0.48f, 0.28f, 0.15f), 0.3f);
            Material steelMat = CityMaterialPalette.CreateMaterial("Mat_BenchSteel", new Color(0.18f, 0.20f, 0.22f), 0.6f, 0.7f);
            mr.sharedMaterials = new Material[] { woodMat, steelMat };

            return go;
        }
    }
}
