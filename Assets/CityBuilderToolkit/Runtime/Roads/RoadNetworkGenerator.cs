using System.Collections.Generic;
using UnityEngine;
using CityBuilderToolkit.Core;
using CityBuilderToolkit.Materials;

namespace CityBuilderToolkit.Roads
{
    public static class RoadSubmeshSlots
    {
        public const int Asphalt = 0;
        public const int Sidewalk = 1;
        public const int Curb = 2;
        public const int Markings = 3;
    }

    /// <summary>
    /// Generates modular road meshes with asphalt lanes, raised curbs,
    /// paved pedestrian sidewalks, and white road markings.
    /// </summary>
    public static class RoadNetworkGenerator
    {
        public static void GenerateRoadSegment(
            MeshBuilder mb,
            Vector3 start,
            Vector3 end,
            float roadWidth = 10f,
            float sidewalkWidth = 2.5f,
            float curbHeight = 0.15f)
        {
            Vector3 dir = end - start;
            float length = dir.magnitude;
            if (length < 0.5f) return;

            Vector3 unitDir = dir / length;
            Vector3 side = new Vector3(-unitDir.z, 0f, unitDir.x).normalized;

            float halfRoad = roadWidth * 0.5f;

            // 1. Asphalt Road Surface
            Vector3 pRoadLeftStart = start - side * halfRoad;
            Vector3 pRoadRightStart = start + side * halfRoad;
            Vector3 pRoadRightEnd = end + side * halfRoad;
            Vector3 pRoadLeftEnd = end - side * halfRoad;

            mb.AddQuad(RoadSubmeshSlots.Asphalt,
                pRoadLeftStart, pRoadRightStart, pRoadRightEnd, pRoadLeftEnd,
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, length * 0.2f),
                new Vector2(0, length * 0.2f),
                Vector3.up);

            // 2. White Centerline Road Markings
            float markingWidth = 0.2f;
            Vector3 pMarkLeftStart = start - side * (markingWidth * 0.5f) + Vector3.up * 0.01f;
            Vector3 pMarkRightStart = start + side * (markingWidth * 0.5f) + Vector3.up * 0.01f;
            Vector3 pMarkRightEnd = end + side * (markingWidth * 0.5f) + Vector3.up * 0.01f;
            Vector3 pMarkLeftEnd = end - side * (markingWidth * 0.5f) + Vector3.up * 0.01f;

            mb.AddQuad(RoadSubmeshSlots.Markings,
                pMarkLeftStart, pMarkRightStart, pMarkRightEnd, pMarkLeftEnd,
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, length * 0.35f),
                new Vector2(0, length * 0.35f),
                Vector3.up);

            // 3. Left Curb (vertical riser) & Sidewalk
            Vector3 pLeftCurbBottomStart = pRoadLeftStart;
            Vector3 pLeftCurbBottomEnd = pRoadLeftEnd;
            Vector3 pLeftCurbTopStart = pRoadLeftStart + Vector3.up * curbHeight;
            Vector3 pLeftCurbTopEnd = pRoadLeftEnd + Vector3.up * curbHeight;

            mb.AddQuad(RoadSubmeshSlots.Curb,
                pLeftCurbBottomStart, pLeftCurbTopStart, pLeftCurbTopEnd, pLeftCurbBottomEnd,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(length * 0.5f, 1), new Vector2(length * 0.5f, 0));

            Vector3 pLeftSidewalkOuterStart = pLeftCurbTopStart - side * sidewalkWidth;
            Vector3 pLeftSidewalkOuterEnd = pLeftCurbTopEnd - side * sidewalkWidth;

            mb.AddQuad(RoadSubmeshSlots.Sidewalk,
                pLeftSidewalkOuterStart, pLeftCurbTopStart, pLeftCurbTopEnd, pLeftSidewalkOuterEnd,
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, length * 0.25f),
                new Vector2(0, length * 0.25f),
                Vector3.up);

            // 4. Right Curb (vertical riser) & Sidewalk
            Vector3 pRightCurbBottomStart = pRoadRightStart;
            Vector3 pRightCurbBottomEnd = pRoadRightEnd;
            Vector3 pRightCurbTopStart = pRoadRightStart + Vector3.up * curbHeight;
            Vector3 pRightCurbTopEnd = pRoadRightEnd + Vector3.up * curbHeight;

            mb.AddQuad(RoadSubmeshSlots.Curb,
                pRightCurbTopStart, pRightCurbBottomStart, pRightCurbBottomEnd, pRightCurbTopEnd,
                new Vector2(0, 1), new Vector2(0, 0), new Vector2(length * 0.5f, 0), new Vector2(length * 0.5f, 1));

            Vector3 pRightSidewalkOuterStart = pRightCurbTopStart + side * sidewalkWidth;
            Vector3 pRightSidewalkOuterEnd = pRightCurbTopEnd + side * sidewalkWidth;

            mb.AddQuad(RoadSubmeshSlots.Sidewalk,
                pRightCurbTopStart, pRightSidewalkOuterStart, pRightSidewalkOuterEnd, pRightCurbTopEnd,
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, length * 0.25f),
                new Vector2(0, length * 0.25f),
                Vector3.up);
        }

        public static Material[] GetRoadMaterials()
        {
            return new Material[]
            {
                CityMaterialPalette.GetAsphaltMaterial(),
                CityMaterialPalette.GetSidewalkMaterial(),
                CityMaterialPalette.GetCurbMaterial(),
                CityMaterialPalette.GetRoadMarkingMaterial()
            };
        }
    }
}
