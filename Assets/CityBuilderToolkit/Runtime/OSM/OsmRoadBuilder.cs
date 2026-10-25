using System;
using System.Collections.Generic;
using UnityEngine;
using CityBuilderToolkit.Core;
using CityBuilderToolkit.Materials;

namespace CityBuilderToolkit.OSM
{
    /// <summary>
    /// Procedurally constructs continuous 3D road ribbons, raised concrete curbs,
    /// and pedestrian sidewalks from OpenStreetMap highway vectors.
    /// </summary>
    public static class OsmRoadBuilder
    {
        public static GameObject BuildRoads(OsmDataSet dataSet, Transform parent)
        {
            var container = new GameObject("OSM_Roads");
            if (parent != null) container.transform.SetParent(parent, false);

            Material[] roadMats = new Material[]
            {
                CityMaterialPalette.GetAsphaltMaterial(),     // Submesh 0: Asphalt
                CityMaterialPalette.GetSidewalkMaterial(),    // Submesh 1: Sidewalk
                CityMaterialPalette.GetCurbMaterial(),        // Submesh 2: Curbs
                CityMaterialPalette.GetRoadMarkingMaterial()  // Submesh 3: Markings
            };

            var mb = new MeshBuilder();
            int roadsBuilt = 0;
            int chunkIndex = 0;

            foreach (var way in dataSet.Ways)
            {
                if (!way.IsHighway) continue;
                if (way.Points.Count < 2) continue;

                // Determine road geometry parameters
                GetRoadDimensions(way, out float roadWidth, out float sidewalkWidth, out bool hasSidewalks);

                BuildRoadRibbon(mb, way.Points, roadWidth, sidewalkWidth, hasSidewalks);
                roadsBuilt++;

                if (mb.VertexCount > 35000)
                {
                    CreateChunk(container, mb, roadMats, $"RoadChunk_{chunkIndex++}");
                    mb.Clear();
                }
            }

            if (mb.VertexCount > 0)
            {
                CreateChunk(container, mb, roadMats, $"RoadChunk_{chunkIndex++}");
            }

            Debug.Log($"[OsmRoadBuilder] Successfully built {roadsBuilt} real OSM road segments.");
            return container;
        }

        private static void GetRoadDimensions(OsmWay way, out float roadWidth, out float sidewalkWidth, out bool hasSidewalks)
        {
            string hw = way.GetTag("highway").ToLowerInvariant();
            switch (hw)
            {
                case "motorway":
                case "trunk":
                    roadWidth = 14f;
                    sidewalkWidth = 0f;
                    hasSidewalks = false;
                    break;
                case "primary":
                    roadWidth = 10.5f;
                    sidewalkWidth = 2.5f;
                    hasSidewalks = true;
                    break;
                case "secondary":
                case "tertiary":
                    roadWidth = 8.5f;
                    sidewalkWidth = 2.2f;
                    hasSidewalks = true;
                    break;
                case "pedestrian":
                    roadWidth = 6.0f;
                    sidewalkWidth = 0f;
                    hasSidewalks = false;
                    break;
                case "footway":
                case "cycleway":
                case "path":
                case "steps":
                    roadWidth = 2.5f;
                    sidewalkWidth = 0f;
                    hasSidewalks = false;
                    break;
                case "service":
                    roadWidth = 4.5f;
                    sidewalkWidth = 1.2f;
                    hasSidewalks = true;
                    break;
                case "residential":
                case "living_street":
                default:
                    roadWidth = 6.8f;
                    sidewalkWidth = 1.8f;
                    hasSidewalks = true;
                    break;
            }
        }

        private static void BuildRoadRibbon(MeshBuilder mb, List<Vector2> rawPoints, float roadWidth, float sidewalkWidth, bool hasSidewalks)
        {
            // Deduplicate points
            var points = new List<Vector2>();
            for (int i = 0; i < rawPoints.Count; i++)
            {
                if (points.Count > 0 && Vector2.Distance(points[points.Count - 1], rawPoints[i]) < 0.2f)
                    continue;
                points.Add(rawPoints[i]);
            }

            if (points.Count < 2) return;

            float halfRoad = roadWidth * 0.5f;
            float curbHeight = 0.15f;

            float accumulatedDist = 0f;

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 p0_2d = (i > 0) ? points[i - 1] : points[i];
                Vector2 p1_2d = points[i];
                Vector2 p2_2d = points[i + 1];
                Vector2 p3_2d = (i + 2 < points.Count) ? points[i + 2] : points[i + 1];

                Vector2 dir1 = (p2_2d - p0_2d).normalized;
                Vector2 dir2 = (p3_2d - p1_2d).normalized;

                Vector2 norm1_2d = new Vector2(-dir1.y, dir1.x);
                Vector2 norm2_2d = new Vector2(-dir2.y, dir2.x);

                float segLen = Vector2.Distance(p1_2d, p2_2d);

                Vector3 v1_left = new Vector3(p1_2d.x + norm1_2d.x * halfRoad, 0.02f, p1_2d.y + norm1_2d.y * halfRoad);
                Vector3 v1_right = new Vector3(p1_2d.x - norm1_2d.x * halfRoad, 0.02f, p1_2d.y - norm1_2d.y * halfRoad);
                Vector3 v2_left = new Vector3(p2_2d.x + norm2_2d.x * halfRoad, 0.02f, p2_2d.y + norm2_2d.y * halfRoad);
                Vector3 v2_right = new Vector3(p2_2d.x - norm2_2d.x * halfRoad, 0.02f, p2_2d.y - norm2_2d.y * halfRoad);

                // 1. Asphalt Driving Surface
                float u0 = accumulatedDist / 10f;
                float u1 = (accumulatedDist + segLen) / 10f;

                mb.AddQuad(0, v1_left, v1_right, v2_right, v2_left,
                    new Vector2(0f, u0), new Vector2(1f, u0), new Vector2(1f, u1), new Vector2(0f, u1),
                    Color.white, Vector3.up);

                // 2. White Centerline Marking
                if (roadWidth >= 6.0f)
                {
                    float markHalfW = 0.10f;
                    Vector3 c1_l = new Vector3(p1_2d.x + norm1_2d.x * markHalfW, 0.035f, p1_2d.y + norm1_2d.y * markHalfW);
                    Vector3 c1_r = new Vector3(p1_2d.x - norm1_2d.x * markHalfW, 0.035f, p1_2d.y - norm1_2d.y * markHalfW);
                    Vector3 c2_l = new Vector3(p2_2d.x + norm2_2d.x * markHalfW, 0.035f, p2_2d.y + norm2_2d.y * markHalfW);
                    Vector3 c2_r = new Vector3(p2_2d.x - norm2_2d.x * markHalfW, 0.035f, p2_2d.y - norm2_2d.y * markHalfW);

                    mb.AddQuad(3, c1_l, c1_r, c2_r, c2_l,
                        new Vector2(0f, u0 * 3f), new Vector2(1f, u0 * 3f), new Vector2(1f, u1 * 3f), new Vector2(0f, u1 * 3f),
                        Color.white, Vector3.up);
                }

                // 3. Raised Curbs and Sidewalks
                if (hasSidewalks && sidewalkWidth > 0.5f)
                {
                    // Left Curb + Sidewalk
                    Vector3 curbTop_l1 = v1_left + Vector3.up * curbHeight;
                    Vector3 curbTop_l2 = v2_left + Vector3.up * curbHeight;

                    // Curb vertical riser
                    mb.AddQuad(2, v1_left, curbTop_l1, curbTop_l2, v2_left,
                        Vector2.zero, Vector2.up, Vector2.one, Vector2.right,
                        Color.white * 0.8f, -new Vector3(norm1_2d.x, 0, norm1_2d.y));

                    // Sidewalk surface
                    Vector3 sw_out_l1 = curbTop_l1 + new Vector3(norm1_2d.x, 0, norm1_2d.y) * sidewalkWidth;
                    Vector3 sw_out_l2 = curbTop_l2 + new Vector3(norm2_2d.x, 0, norm2_2d.y) * sidewalkWidth;

                    mb.AddQuad(1, curbTop_l1, sw_out_l1, sw_out_l2, curbTop_l2,
                        new Vector2(0f, u0), new Vector2(sidewalkWidth, u0), new Vector2(sidewalkWidth, u1), new Vector2(0f, u1),
                        Color.white, Vector3.up);

                    // Right Curb + Sidewalk
                    Vector3 curbTop_r1 = v1_right + Vector3.up * curbHeight;
                    Vector3 curbTop_r2 = v2_right + Vector3.up * curbHeight;

                    // Curb vertical riser
                    mb.AddQuad(2, curbTop_r1, v1_right, v2_right, curbTop_r2,
                        Vector2.zero, Vector2.up, Vector2.one, Vector2.right,
                        Color.white * 0.8f, new Vector3(norm1_2d.x, 0, norm1_2d.y));

                    // Sidewalk surface
                    Vector3 sw_out_r1 = curbTop_r1 - new Vector3(norm1_2d.x, 0, norm1_2d.y) * sidewalkWidth;
                    Vector3 sw_out_r2 = curbTop_r2 - new Vector3(norm2_2d.x, 0, norm2_2d.y) * sidewalkWidth;

                    mb.AddQuad(1, sw_out_r1, curbTop_r1, curbTop_r2, sw_out_r2,
                        new Vector2(0f, u0), new Vector2(sidewalkWidth, u0), new Vector2(sidewalkWidth, u1), new Vector2(0f, u1),
                        Color.white, Vector3.up);
                }

                accumulatedDist += segLen;
            }
        }

        private static void CreateChunk(GameObject parent, MeshBuilder mb, Material[] materials, string name)
        {
            var chunkGo = new GameObject(name);
            chunkGo.transform.SetParent(parent.transform, false);

            var mf = chunkGo.AddComponent<MeshFilter>();
            var mr = chunkGo.AddComponent<MeshRenderer>();
            var mc = chunkGo.AddComponent<MeshCollider>();

            Mesh mesh = mb.BuildMesh(name);
            mf.sharedMesh = mesh;
            mr.sharedMaterials = materials;
            mc.sharedMesh = mesh;
        }
    }
}
