using System;
using System.Collections.Generic;
using UnityEngine;
using CityBuilderToolkit.Core;
using CityBuilderToolkit.Buildings;
using CityBuilderToolkit.Roads;
using CityBuilderToolkit.Materials;

namespace CityBuilderToolkit.Town
{
    /// <summary>
    /// Represents a city block parcel, responsible for building layout,
    /// courtyards, alleys, and park generation.
    /// </summary>
    public static class TownBlock
    {
        public static void GenerateBlock(
            Transform parent,
            Rect blockRect,
            ZoneType zone,
            System.Random rng,
            bool spawnTrees = true,
            bool spawnBenches = true)
        {
            var blockGo = new GameObject($"Block_{zone}_{blockRect.x:F0}_{blockRect.y:F0}");
            blockGo.transform.SetParent(parent, false);

            if (zone == ZoneType.PublicPark)
            {
                GeneratePublicPark(blockGo.transform, blockRect, rng);
                return;
            }

            // Inset block slightly from sidewalk for building setback
            float setback = 1.0f;
            Rect parcelArea = new Rect(
                blockRect.x + setback,
                blockRect.y + setback,
                blockRect.width - setback * 2f,
                blockRect.height - setback * 2f
            );

            // Subdivide block into parcels
            List<Rect> parcels = SubdivideBlock(parcelArea, zone, rng);

            for (int p = 0; p < parcels.Count; p++)
            {
                Rect r = parcels[p];
                if (r.width < 10f || r.height < 10f) continue;

                var bldgGo = new GameObject($"Building_{p + 1}");
                bldgGo.transform.SetParent(blockGo.transform, false);
                bldgGo.transform.position = new Vector3(r.center.x, 0f, r.center.y);

                var bg = bldgGo.AddComponent<BuildingGenerator>();
                bg.width = r.width - 2.0f;
                bg.depth = r.height - 2.0f;
                bg.floors = TownZoning.GetFloorCountForZone(zone, rng);
                bg.style = TownZoning.GetStyleForZone(zone, rng);
                bg.randomSeed = rng.Next(1, 99999);

                // Varied footprint shapes for large parcels
                if (zone == ZoneType.ResidentialApartments && r.width > 24f && r.height > 20f)
                {
                    bg.shape = (rng.NextDouble() > 0.5) ? FootprintShape.LShape : FootprintShape.UShape;
                }
                else if (zone == ZoneType.DowntownSkyscrapers && bg.floors > 14)
                {
                    bg.shape = FootprintShape.Rectangle;
                    bg.setbackPerFloor = 0.08f;
                }
                else
                {
                    bg.shape = FootprintShape.Rectangle;
                }

                bg.Generate();
            }

            // Center Courtyard Greenery
            if (spawnTrees && parcels.Count >= 2)
            {
                Vector3 courtyardCenter = new Vector3(parcelArea.center.x, 0f, parcelArea.center.y);
                StreetFurnitureSpawner.SpawnTree(blockGo.transform, courtyardCenter, 1.2f);
            }
        }

        private static List<Rect> SubdivideBlock(Rect area, ZoneType zone, System.Random rng)
        {
            var list = new List<Rect>();

            if (zone == ZoneType.DowntownSkyscrapers)
            {
                // Skyscrapers: 1 massive tower or 2 large towers
                if (area.width > 35f && area.height > 35f && rng.NextDouble() > 0.5)
                {
                    float halfW = area.width * 0.5f;
                    list.Add(new Rect(area.x, area.y, halfW, area.height));
                    list.Add(new Rect(area.x + halfW, area.y, halfW, area.height));
                }
                else
                {
                    list.Add(area);
                }
            }
            else
            {
                // Subdivide into 2x2 or 2x1 lots
                bool splitX = area.width >= 30f;
                bool splitY = area.height >= 30f;

                if (splitX && splitY)
                {
                    float hw = area.width * 0.5f;
                    float hh = area.height * 0.5f;
                    list.Add(new Rect(area.x, area.y, hw, hh));
                    list.Add(new Rect(area.x + hw, area.y, hw, hh));
                    list.Add(new Rect(area.x, area.y + hh, hw, hh));
                    list.Add(new Rect(area.x + hw, area.y + hh, hw, hh));
                }
                else if (splitX)
                {
                    float hw = area.width * 0.5f;
                    list.Add(new Rect(area.x, area.y, hw, area.height));
                    list.Add(new Rect(area.x + hw, area.y, hw, area.height));
                }
                else
                {
                    list.Add(area);
                }
            }

            return list;
        }

        private static void GeneratePublicPark(Transform parent, Rect area, System.Random rng)
        {
            var parkGo = new GameObject("ParkPlaza");
            parkGo.transform.SetParent(parent, false);
            parkGo.transform.position = new Vector3(area.center.x, 0f, area.center.y);

            var mb = new MeshBuilder(2);

            // 1. Grass Lawn Base
            List<Vector2> lawnPoly = Polygon2D.CreateRectangle(area.width - 2f, area.height - 2f);
            mb.AddHorizontalPolygonFace(0, lawnPoly, 0.05f, true, 0.2f);

            // 2. Paved Cross Pathways
            float pathWidth = 3.5f;
            mb.AddBox(1, new Vector3(0f, 0.08f, 0f), new Vector3(area.width - 2f, 0.05f, pathWidth));
            mb.AddBox(1, new Vector3(0f, 0.08f, 0f), new Vector3(pathWidth, 0.05f, area.height - 2f));

            var mf = parkGo.AddComponent<MeshFilter>();
            mf.sharedMesh = mb.ToMesh("ParkMesh");

            var mr = parkGo.AddComponent<MeshRenderer>();
            Material grassMat = CityMaterialPalette.GetGrassMaterial();
            Material pathMat = CityMaterialPalette.GetSidewalkMaterial();
            mr.sharedMaterials = new Material[] { grassMat, pathMat };

            // 3. Scatter Park Trees & Benches
            float spreadX = (area.width - 8f) * 0.5f;
            float spreadZ = (area.height - 8f) * 0.5f;

            Vector3[] treeOffsets = new Vector3[]
            {
                new Vector3(-spreadX * 0.6f, 0f, -spreadZ * 0.6f),
                new Vector3( spreadX * 0.6f, 0f, -spreadZ * 0.6f),
                new Vector3(-spreadX * 0.6f, 0f,  spreadZ * 0.6f),
                new Vector3( spreadX * 0.6f, 0f,  spreadZ * 0.6f)
            };

            for (int i = 0; i < treeOffsets.Length; i++)
            {
                StreetFurnitureSpawner.SpawnTree(parkGo.transform, parkGo.transform.position + treeOffsets[i], 1.25f);
            }

            // Benches along paths
            StreetFurnitureSpawner.SpawnBench(parkGo.transform,
                parkGo.transform.position + new Vector3(3.0f, 0.08f, 2.0f), Quaternion.Euler(0f, 180f, 0f));
            StreetFurnitureSpawner.SpawnBench(parkGo.transform,
                parkGo.transform.position + new Vector3(-3.0f, 0.08f, -2.0f), Quaternion.Euler(0f, 0f, 0f));
        }
    }
}
