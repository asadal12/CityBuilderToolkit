using System;
using System.Collections.Generic;
using UnityEngine;
using CityBuilderToolkit.Core;
using CityBuilderToolkit.Materials;
using CityBuilderToolkit.Roads;

namespace CityBuilderToolkit.OSM
{
    /// <summary>
    /// Builds real environmental features from OpenStreetMap: parks, grass areas, water bodies,
    /// and street furniture along highways.
    /// </summary>
    public static class OsmEnvironmentBuilder
    {
        public static GameObject BuildEnvironment(OsmDataSet dataSet, Transform parent, bool spawnScatter = true)
        {
            var container = new GameObject("OSM_Environment");
            if (parent != null) container.transform.SetParent(parent, false);

            var grassMat = CityMaterialPalette.GetGrassMaterial();
            var waterMat = CityMaterialPalette.GetWaterMaterial();

            var mbParks = new MeshBuilder();
            var mbWater = new MeshBuilder();

            var parkPositions = new List<Vector3>();

            foreach (var way in dataSet.Ways)
            {
                if (way.Points.Count < 3) continue;

                // 1. Parks and Green Spaces
                if (way.IsPark)
                {
                    var poly = CleanPolygon(way.Points);
                    if (poly.Count >= 3)
                    {
                        mbParks.AddHorizontalPolygon(0, poly, 0.01f, true, Color.white);
                        parkPositions.Add(new Vector3(poly[0].x, 0f, poly[0].y));
                    }
                }
                // 2. Water Bodies & Waterways
                else if (way.IsWater)
                {
                    var poly = CleanPolygon(way.Points);
                    if (poly.Count >= 3)
                    {
                        mbWater.AddHorizontalPolygon(0, poly, -0.05f, true, Color.white);
                    }
                    else if (way.Points.Count >= 2)
                    {
                        // Linear waterway (river, canal, stream)
                        BuildWaterRibbon(mbWater, way.Points, 6.0f);
                    }
                }
            }

            if (mbParks.VertexCount > 0)
            {
                CreateMeshObject(container, mbParks, grassMat, "OSM_Parks_Greenery");
            }

            if (mbWater.VertexCount > 0)
            {
                CreateMeshObject(container, mbWater, waterMat, "OSM_Water_Features");
            }

            // 3. Street Furniture & Tree Scatter
            if (spawnScatter)
            {
                var scatterContainer = new GameObject("OSM_StreetFurniture");
                scatterContainer.transform.SetParent(container.transform, false);

                // Spawn streetlights along major road ways
                int lightCount = 0;
                foreach (var way in dataSet.Ways)
                {
                    if (!way.IsHighway) continue;
                    if (way.Points.Count < 2) continue;

                    string hw = way.GetTag("highway").ToLowerInvariant();
                    if (hw == "primary" || hw == "secondary" || hw == "residential")
                    {
                        for (int i = 0; i < way.Points.Count - 1; i += 2)
                        {
                            Vector2 pt = way.Points[i];
                            Vector2 next = way.Points[i + 1];
                            Vector2 dir = (next - pt).normalized;
                            Vector2 normal = new Vector2(-dir.y, dir.x);

                            Vector3 pos = new Vector3(pt.x + normal.x * 4.5f, 0f, pt.y + normal.y * 4.5f);
                            Quaternion rot = Quaternion.LookRotation(new Vector3(-normal.x, 0f, -normal.y), Vector3.up);

                            StreetFurnitureSpawner.SpawnStreetLight(scatterContainer.transform, pos, rot);
                            lightCount++;
                            if (lightCount > 120) break; // Keep scene performant
                        }
                    }
                    if (lightCount > 120) break;
                }

                // Spawn benches and trees in park areas
                int treeCount = 0;
                foreach (var parkPos in parkPositions)
                {
                    StreetFurnitureSpawner.SpawnTree(scatterContainer.transform, parkPos + new Vector3(2f, 0, 2f), 1.0f);
                    StreetFurnitureSpawner.SpawnBench(scatterContainer.transform, parkPos + new Vector3(-2f, 0, 1f), Quaternion.identity);
                    treeCount++;
                    if (treeCount > 60) break;
                }
            }

            return container;
        }

        private static List<Vector2> CleanPolygon(List<Vector2> raw)
        {
            var result = new List<Vector2>(raw.Count);
            for (int i = 0; i < raw.Count; i++)
            {
                Vector2 pt = raw[i];
                if (result.Count > 0 && Vector2.Distance(result[result.Count - 1], pt) < 0.2f)
                    continue;
                result.Add(pt);
            }
            if (result.Count > 3 && Vector2.Distance(result[0], result[result.Count - 1]) < 0.25f)
            {
                result.RemoveAt(result.Count - 1);
            }
            return Polygon2D.EnsureWinding(result, true);
        }

        private static void BuildWaterRibbon(MeshBuilder mb, List<Vector2> points, float width)
        {
            float halfW = width * 0.5f;
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 p1 = points[i];
                Vector2 p2 = points[i + 1];
                Vector2 dir = (p2 - p1).normalized;
                Vector2 norm = new Vector2(-dir.y, dir.x);

                Vector3 v1_l = new Vector3(p1.x + norm.x * halfW, -0.05f, p1.y + norm.y * halfW);
                Vector3 v1_r = new Vector3(p1.x - norm.x * halfW, -0.05f, p1.y - norm.y * halfW);
                Vector3 v2_l = new Vector3(p2.x + norm.x * halfW, -0.05f, p2.y + norm.y * halfW);
                Vector3 v2_r = new Vector3(p2.x - norm.x * halfW, -0.05f, p2.y - norm.y * halfW);

                mb.AddQuad(0, v1_l, v1_r, v2_r, v2_l,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                    Color.white, Vector3.up);
            }
        }

        private static void CreateMeshObject(GameObject parent, MeshBuilder mb, Material material, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            var mc = go.AddComponent<MeshCollider>();

            Mesh mesh = mb.BuildMesh(name);
            mf.sharedMesh = mesh;
            mr.sharedMaterial = material;
            mc.sharedMesh = mesh;
        }
    }
}
