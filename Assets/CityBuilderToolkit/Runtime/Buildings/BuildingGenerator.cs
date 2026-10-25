using System;
using System.Collections.Generic;
using UnityEngine;
using CityBuilderToolkit.Core;
using CityBuilderToolkit.Materials;

namespace CityBuilderToolkit.Buildings
{
    /// <summary>
    /// Procedural building generator component.
    /// Can be added to any GameObject in the scene to procedurally generate a custom building
    /// with customizable floors, facades, windows, balconies, and roofs.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [ExecuteInEditMode]
    public class BuildingGenerator : MonoBehaviour
    {
        [Header("Footprint Settings")]
        public FootprintShape shape = FootprintShape.Rectangle;
        [Range(4f, 60f)]
        public float width = 16f;
        [Range(4f, 60f)]
        public float depth = 14f;

        [Tooltip("Custom 2D polygon vertices in local X-Z space (when shape == CustomPolygon)")]
        public List<Vector2> customPolygon = new List<Vector2>();

        [Header("Floors & Dimensions")]
        [Range(1, 40)]
        public int floors = 4;
        [Range(0f, 0.5f)]
        public float setbackPerFloor = 0f;

        [Header("Architectural Style")]
        public BuildingStyleConfig style = new BuildingStyleConfig();

        [Header("Collision & Output")]
        public bool generateMeshCollider = true;
        public int randomSeed = 0;

        [Header("Optional Preset")]
        public BuildingPreset preset;

        public void ApplyPreset(BuildingPreset p)
        {
            if (p == null) return;
            preset = p;
            shape = p.footprintShape;
            width = p.width;
            depth = p.depth;
            floors = UnityEngine.Random.Range(p.minFloors, p.maxFloors + 1);
            style = p.style;
        }

        [ContextMenu("Generate Building")]
        public void Generate()
        {
            System.Random rng = (randomSeed == 0) ? new System.Random() : new System.Random(randomSeed);

            // 1. Resolve Footprint Polygon
            List<Vector2> footprint = GetFootprintPolygon();
            if (footprint == null || footprint.Count < 3)
            {
                Debug.LogWarning("[BuildingGenerator] Footprint has fewer than 3 vertices.", this);
                return;
            }

            footprint = Polygon2D.EnsureWinding(footprint, true);

            var mb = new MeshBuilder(6);

            float currentY = 0f;
            List<Vector2> currentFootprint = new List<Vector2>(footprint);

            // 2. Generate Storey by Storey
            for (int f = 0; f < floors; f++)
            {
                float floorH = (f == 0 && style.hasGroundFloorCommercial) ? style.groundFloorHeight : style.floorHeight;
                float bottomY = currentY;
                float topY = bottomY + floorH;

                int vertexCount = currentFootprint.Count;
                for (int i = 0; i < vertexCount; i++)
                {
                    Vector2 v0_2D = currentFootprint[i];
                    Vector2 v1_2D = currentFootprint[(i + 1) % vertexCount];

                    Vector3 p0 = new Vector3(v0_2D.x, 0f, v0_2D.y);
                    Vector3 p1 = new Vector3(v1_2D.x, 0f, v1_2D.y);

                    FacadeGenerator.GenerateWallFacade(mb, p0, p1, bottomY, topY, f, floors, style, rng);
                }

                currentY = topY;

                // Setback for upper floors
                if (setbackPerFloor > 0.01f && f < floors - 1)
                {
                    var nextFootprint = Polygon2D.OffsetPolygon(currentFootprint, setbackPerFloor);
                    if (nextFootprint.Count >= 3 && Polygon2D.GetSignedArea(nextFootprint) > 10f)
                    {
                        // Add setback ledge / terrace slab
                        mb.AddHorizontalPolygonFace(BuildingSubmeshSlots.Roof, currentFootprint, currentY, true);
                        currentFootprint = nextFootprint;
                    }
                }
            }

            // 3. Generate Roof Structure
            GenerateRoof(mb, currentFootprint, currentY, rng);

            // 4. Assign Mesh to Filter
            Mesh mesh = mb.ToMesh($"Building_{name}");
            var mf = GetComponent<MeshFilter>();
            if (mf != null) mf.sharedMesh = mesh;

            // 5. Assign Materials
            var mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterials = CityMaterialPalette.GetBuildingMaterials(style);
            }

            // 6. Update Mesh Collider
            if (generateMeshCollider)
            {
                var mc = GetComponent<MeshCollider>();
                if (mc == null) mc = gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = null;
                mc.sharedMesh = mesh;
            }
        }

        private List<Vector2> GetFootprintPolygon()
        {
            switch (shape)
            {
                case FootprintShape.Rectangle:
                    return Polygon2D.CreateRectangle(width, depth);
                case FootprintShape.LShape:
                    return Polygon2D.CreateLShape(width, depth, 0.5f, 0.5f);
                case FootprintShape.UShape:
                    return Polygon2D.CreateUShape(width, depth, 0.35f, 0.5f);
                case FootprintShape.TShape:
                    return Polygon2D.CreateTShape(width, depth, 0.4f, 0.45f);
                case FootprintShape.CustomPolygon:
                    return (customPolygon != null && customPolygon.Count >= 3)
                        ? customPolygon
                        : Polygon2D.CreateRectangle(width, depth);
                default:
                    return Polygon2D.CreateRectangle(width, depth);
            }
        }

        private void GenerateRoof(MeshBuilder mb, List<Vector2> roofPoly, float roofBaseY, System.Random rng)
        {
            switch (style.roofStyle)
            {
                case RoofStyle.FlatWithParapet:
                case RoofStyle.RooftopTerrace:
                    GenerateFlatParapetRoof(mb, roofPoly, roofBaseY, rng);
                    break;
                case RoofStyle.Gabled:
                    GenerateGabledRoof(mb, roofPoly, roofBaseY);
                    break;
                case RoofStyle.Hipped:
                    GenerateHippedRoof(mb, roofPoly, roofBaseY);
                    break;
                case RoofStyle.SkyscraperCrown:
                    GenerateSkyscraperCrown(mb, roofPoly, roofBaseY);
                    break;
                default:
                    GenerateFlatParapetRoof(mb, roofPoly, roofBaseY, rng);
                    break;
            }
        }

        private void GenerateFlatParapetRoof(MeshBuilder mb, List<Vector2> poly, float baseY, System.Random rng)
        {
            // Roof deck slab
            mb.AddHorizontalPolygonFace(BuildingSubmeshSlots.Roof, poly, baseY, true);

            // Parapet wall around edge
            float parapetH = Mathf.Max(0.6f, style.parapetHeight);
            int count = poly.Count;
            for (int i = 0; i < count; i++)
            {
                Vector2 v0 = poly[i];
                Vector2 v1 = poly[(i + 1) % count];
                mb.AddExtrudedWall(BuildingSubmeshSlots.Trim,
                    new Vector3(v0.x, 0, v0.y),
                    new Vector3(v1.x, 0, v1.y),
                    baseY, baseY + parapetH);
            }

            // Rooftop Units (Elevator shaft, AC units)
            if (style.hasRooftopUnits)
            {
                Vector2 centroid = Polygon2D.GetCentroid(poly);
                Vector3 center3D = new Vector3(centroid.x, baseY, centroid.y);

                // Elevator Bulkhead Box
                mb.AddBox(BuildingSubmeshSlots.Wall,
                    center3D + Vector3.up * 1.5f,
                    new Vector3(4f, 3.0f, 3.5f));

                // AC Chillers
                mb.AddBox(BuildingSubmeshSlots.Trim,
                    center3D + new Vector3(3.5f, 0.7f, 0.5f),
                    new Vector3(1.8f, 1.4f, 1.2f));

                mb.AddBox(BuildingSubmeshSlots.Trim,
                    center3D + new Vector3(-3.5f, 0.7f, -0.5f),
                    new Vector3(1.8f, 1.4f, 1.2f));
            }
        }

        private void GenerateGabledRoof(MeshBuilder mb, List<Vector2> poly, float baseY)
        {
            Rect bounds = Polygon2D.GetBounds(poly);
            float pitchH = Mathf.Min(bounds.width, bounds.height) * 0.35f;
            float ridgeY = baseY + pitchH;

            // Determine long vs short axis for ridge line
            bool longX = bounds.width >= bounds.height;
            float hw = bounds.width * 0.5f;
            float hd = bounds.height * 0.5f;
            Vector2 c = bounds.center;

            if (longX)
            {
                Vector3 pLeftRidge = new Vector3(c.x - hw, ridgeY, c.y);
                Vector3 pRightRidge = new Vector3(c.x + hw, ridgeY, c.y);

                Vector3 pSouthLeft = new Vector3(c.x - hw, baseY, c.y - hd);
                Vector3 pSouthRight = new Vector3(c.x + hw, baseY, c.y - hd);
                Vector3 pNorthLeft = new Vector3(c.x - hw, baseY, c.y + hd);
                Vector3 pNorthRight = new Vector3(c.x + hw, baseY, c.y + hd);

                // South Sloping Roof Plane
                mb.AddQuad(BuildingSubmeshSlots.Roof, pSouthLeft, pSouthRight, pRightRidge, pLeftRidge,
                    new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));

                // North Sloping Roof Plane
                mb.AddQuad(BuildingSubmeshSlots.Roof, pNorthRight, pNorthLeft, pLeftRidge, pRightRidge,
                    new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));

                // West Triangular Gable Wall
                int iw0 = mb.AddVertex(pSouthLeft, Vector3.left, new Vector2(0, 0));
                int iw1 = mb.AddVertex(pLeftRidge, Vector3.left, new Vector2(0.5f, 1));
                int iw2 = mb.AddVertex(pNorthLeft, Vector3.left, new Vector2(1, 0));
                mb.AddTriangle(BuildingSubmeshSlots.Wall, iw0, iw1, iw2);

                // East Triangular Gable Wall
                int ie0 = mb.AddVertex(pSouthRight, Vector3.right, new Vector2(0, 0));
                int ie1 = mb.AddVertex(pNorthRight, Vector3.right, new Vector2(1, 0));
                int ie2 = mb.AddVertex(pRightRidge, Vector3.right, new Vector2(0.5f, 1));
                mb.AddTriangle(BuildingSubmeshSlots.Wall, ie0, ie1, ie2);
            }
            else
            {
                Vector3 pSouthRidge = new Vector3(c.x, ridgeY, c.y - hd);
                Vector3 pNorthRidge = new Vector3(c.x, ridgeY, c.y + hd);

                Vector3 pWestSouth = new Vector3(c.x - hw, baseY, c.y - hd);
                Vector3 pWestNorth = new Vector3(c.x - hw, baseY, c.y + hd);
                Vector3 pEastSouth = new Vector3(c.x + hw, baseY, c.y - hd);
                Vector3 pEastNorth = new Vector3(c.x + hw, baseY, c.y + hd);

                // West Sloping Plane
                mb.AddQuad(BuildingSubmeshSlots.Roof, pWestNorth, pWestSouth, pSouthRidge, pNorthRidge,
                    new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));

                // East Sloping Plane
                mb.AddQuad(BuildingSubmeshSlots.Roof, pEastSouth, pEastNorth, pNorthRidge, pSouthRidge,
                    new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));

                // South Gable Wall
                int is0 = mb.AddVertex(pWestSouth, Vector3.back, new Vector2(0, 0));
                int is1 = mb.AddVertex(pEastSouth, Vector3.back, new Vector2(1, 0));
                int is2 = mb.AddVertex(pSouthRidge, Vector3.back, new Vector2(0.5f, 1));
                mb.AddTriangle(BuildingSubmeshSlots.Wall, is0, is2, is1);

                // North Gable Wall
                int in0 = mb.AddVertex(pWestNorth, Vector3.forward, new Vector2(0, 0));
                int in1 = mb.AddVertex(pNorthRidge, Vector3.forward, new Vector2(0.5f, 1));
                int in2 = mb.AddVertex(pEastNorth, Vector3.forward, new Vector2(1, 0));
                mb.AddTriangle(BuildingSubmeshSlots.Wall, in0, in1, in2);
            }
        }

        private void GenerateHippedRoof(MeshBuilder mb, List<Vector2> poly, float baseY)
        {
            Vector2 centroid = Polygon2D.GetCentroid(poly);
            Rect bounds = Polygon2D.GetBounds(poly);
            float apexH = Mathf.Min(bounds.width, bounds.height) * 0.3f;
            Vector3 apex = new Vector3(centroid.x, baseY + apexH, centroid.y);

            int apexIdx = mb.AddVertex(apex, Vector3.up, new Vector2(0.5f, 0.5f));

            int count = poly.Count;
            for (int i = 0; i < count; i++)
            {
                Vector2 v0 = poly[i];
                Vector2 v1 = poly[(i + 1) % count];

                Vector3 p0 = new Vector3(v0.x, baseY, v0.y);
                Vector3 p1 = new Vector3(v1.x, baseY, v1.y);

                Vector3 normal = Vector3.Cross(p1 - p0, apex - p0).normalized;

                int i0 = mb.AddVertex(p0, normal, new Vector2(0, 0));
                int i1 = mb.AddVertex(p1, normal, new Vector2(1, 0));

                mb.AddTriangle(BuildingSubmeshSlots.Roof, i0, i1, apexIdx);
            }
        }

        private void GenerateSkyscraperCrown(MeshBuilder mb, List<Vector2> poly, float baseY)
        {
            // Tier 1 Crown Box
            Vector2 centroid = Polygon2D.GetCentroid(poly);
            Rect bounds = Polygon2D.GetBounds(poly);

            float tier1W = bounds.width * 0.7f;
            float tier1D = bounds.height * 0.7f;
            float tier1H = 4.5f;

            Vector3 center1 = new Vector3(centroid.x, baseY + tier1H * 0.5f, centroid.y);
            mb.AddBox(BuildingSubmeshSlots.Glass, center1, new Vector3(tier1W, tier1H, tier1D));

            // Tier 2 Spire Base
            float tier2W = tier1W * 0.5f;
            float tier2D = tier1D * 0.5f;
            float tier2H = 3.0f;
            Vector3 center2 = new Vector3(centroid.x, baseY + tier1H + tier2H * 0.5f, centroid.y);
            mb.AddBox(BuildingSubmeshSlots.Trim, center2, new Vector3(tier2W, tier2H, tier2D));

            // Spire / Antenna Mast
            float mastH = 8.0f;
            Vector3 mastCenter = new Vector3(centroid.x, baseY + tier1H + tier2H + mastH * 0.5f, centroid.y);
            mb.AddBox(BuildingSubmeshSlots.Trim, mastCenter, new Vector3(0.35f, mastH, 0.35f));
        }
    }
}
