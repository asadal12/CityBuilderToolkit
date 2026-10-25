using System;
using System.Collections.Generic;
using UnityEngine;
using CityBuilderToolkit.Core;
using CityBuilderToolkit.Materials;

namespace CityBuilderToolkit.OSM
{
    /// <summary>
    /// Generates accurate 3D building meshes from OpenStreetMap polygon footprints,
    /// complete with multi-storey facade subdivisions, windows, entrances, and architectural roofs.
    /// </summary>
    public static class OsmBuildingBuilder
    {
        public static GameObject BuildBuildings(OsmDataSet dataSet, Transform parent, int maxBuildings = 1500)
        {
            var container = new GameObject("OSM_Buildings");
            if (parent != null) container.transform.SetParent(parent, false);

            var materials = CityMaterialPalette.GetDefaultBuildingMaterials();

            var mb = new MeshBuilder();
            int buildingsBuilt = 0;
            int chunkIndex = 0;

            // Palette of authentic European & modern town colors
            Color[] facadeColors = new Color[]
            {
                new Color(0.90f, 0.88f, 0.84f), // Warm plaster
                new Color(0.85f, 0.82f, 0.77f), // Sandstone
                new Color(0.78f, 0.76f, 0.74f), // Contemporary grey
                new Color(0.92f, 0.86f, 0.78f), // Soft cream
                new Color(0.72f, 0.65f, 0.58f), // Muted ochre
                new Color(0.82f, 0.84f, 0.85f), // Modern cool concrete
                new Color(0.68f, 0.42f, 0.38f), // European brick red
            };

            foreach (var way in dataSet.Ways)
            {
                if (!way.IsBuilding) continue;
                if (way.Points.Count < 3) continue;

                var poly = CleanPolygon(way.Points);
                if (poly.Count < 3) continue;

                float area = Mathf.Abs(Polygon2D.GetSignedArea(poly));
                if (area < 8f) continue; // Skip tiny sheds or artifacts

                // Build building into mesh builder
                BuildSingleBuilding(mb, poly, way, facadeColors);
                buildingsBuilt++;

                // Chunk meshes if vertex count approaches 16-bit / 32-bit threshold or per batch
                if (mb.VertexCount > 35000 || buildingsBuilt >= maxBuildings)
                {
                    CreateChunk(container, mb, materials, $"BuildingChunk_{chunkIndex++}");
                    mb.Clear();

                    if (buildingsBuilt >= maxBuildings) break;
                }
            }

            if (mb.VertexCount > 0)
            {
                CreateChunk(container, mb, materials, $"BuildingChunk_{chunkIndex++}");
            }

            Debug.Log($"[OsmBuildingBuilder] Successfully constructed {buildingsBuilt} real OSM buildings.");
            return container;
        }

        private static List<Vector2> CleanPolygon(List<Vector2> raw)
        {
            var result = new List<Vector2>(raw.Count);
            for (int i = 0; i < raw.Count; i++)
            {
                Vector2 pt = raw[i];
                if (result.Count > 0 && Vector2.Distance(result[result.Count - 1], pt) < 0.15f)
                    continue;
                result.Add(pt);
            }

            // Remove closing vertex if it's identical to the start vertex
            if (result.Count > 3 && Vector2.Distance(result[0], result[result.Count - 1]) < 0.2f)
            {
                result.RemoveAt(result.Count - 1);
            }

            return Polygon2D.EnsureWinding(result, true);
        }

        private static void BuildSingleBuilding(MeshBuilder mb, List<Vector2> poly, OsmWay way, Color[] colorPalette)
        {
            float height = way.GetHeight(10f);
            int floors = Mathf.Max(1, Mathf.RoundToInt(height / 3.2f));
            float floorHeight = height / floors;

            // Pick color seed based on way ID
            int colorIdx = (int)(Math.Abs(way.Id) % colorPalette.Length);
            Color wallCol = colorPalette[colorIdx];

            // 1. Extrude Walls with Floor Subdivisions & Windows
            int n = poly.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 p1_2d = poly[i];
                Vector2 p2_2d = poly[(i + 1) % n];

                Vector3 base1 = new Vector3(p1_2d.x, 0f, p1_2d.y);
                Vector3 base2 = new Vector3(p2_2d.x, 0f, p2_2d.y);

                float wallLen = Vector3.Distance(base1, base2);
                Vector3 wallDir = (base2 - base1).normalized;
                Vector3 outNormal = Vector3.Cross(Vector3.up, wallDir).normalized;

                // Subdivide by floors
                for (int f = 0; f < floors; f++)
                {
                    float yBottom = f * floorHeight;
                    float yTop = (f + 1) * floorHeight;

                    // Ground floor commercial / entrance or standard floor
                    bool isGround = (f == 0);
                    bool hasShopfront = isGround && (way.HasTag("shop") || way.HasTag("commercial") || (wallLen > 8f && wallCol.r > 0.8f));

                    if (hasShopfront && wallLen > 4f)
                    {
                        // Commercial glass shopfront on ground floor
                        BuildShopfrontSegment(mb, base1, base2, yBottom, yTop, outNormal, wallCol);
                    }
                    else if (wallLen > 3.0f)
                    {
                        // Residential / office window bays
                        BuildWindowedWallSegment(mb, base1, base2, yBottom, yTop, outNormal, wallCol, isGround);
                    }
                    else
                    {
                        // Narrow wall segment: plain solid wall
                        Vector3 b1 = base1 + Vector3.up * yBottom;
                        Vector3 b2 = base2 + Vector3.up * yBottom;
                        Vector3 t1 = base1 + Vector3.up * yTop;
                        Vector3 t2 = base2 + Vector3.up * yTop;

                        mb.AddQuad(BuildingSubmeshSlots.Wall, b1, b2, t2, t1,
                            new Vector2(0f, yBottom), new Vector2(wallLen, yBottom),
                            new Vector2(wallLen, yTop), new Vector2(0f, yTop),
                            wallCol, outNormal);
                    }

                    // Floor separation ledge band
                    if (f > 0)
                    {
                        Vector3 bandStart = base1 + Vector3.up * yBottom + outNormal * 0.08f;
                        Vector3 bandEnd = base2 + Vector3.up * yBottom + outNormal * 0.08f;
                        mb.AddBox(BuildingSubmeshSlots.Trim, (bandStart + bandEnd) * 0.5f,
                            new Vector3(wallLen, 0.15f, 0.16f),
                            Quaternion.LookRotation(wallDir, Vector3.up),
                            Color.white * 0.85f);
                    }
                }
            }

            // 2. Roof Construction
            string roofShape = way.GetTag("roof:shape", "flat").ToLowerInvariant();
            if (roofShape == "gabled" || roofShape == "pitched")
            {
                BuildGabledRoof(mb, poly, height, wallCol);
            }
            else if (roofShape == "hipped")
            {
                BuildHippedRoof(mb, poly, height, wallCol);
            }
            else
            {
                // Default: Modern Flat Roof with Raised Parapet
                BuildFlatParapetRoof(mb, poly, height);

                // Add rooftop elevator bulkheads / HVAC chillers on larger roofs
                if (Polygon2D.GetSignedArea(poly) > 120f)
                {
                    Vector2 centroid = Polygon2D.GetCentroid(poly);
                    Vector3 hvacPos = new Vector3(centroid.x, height + 0.8f, centroid.y);
                    mb.AddBox(BuildingSubmeshSlots.Details, hvacPos, new Vector3(3.2f, 1.6f, 2.4f), Quaternion.identity, new Color(0.45f, 0.48f, 0.50f));
                }
            }
        }

        private static void BuildWindowedWallSegment(MeshBuilder mb, Vector3 p1, Vector3 p2, float yBottom, float yTop, Vector3 normal, Color wallCol, bool isGround)
        {
            float wallLen = Vector3.Distance(p1, p2);
            Vector3 dir = (p2 - p1).normalized;

            int bays = Mathf.Max(1, Mathf.FloorToInt(wallLen / 3.0f));
            float bayWidth = wallLen / bays;
            float floorH = yTop - yBottom;

            float windowW = bayWidth * 0.52f;
            float windowH = floorH * 0.55f;
            float sillH = floorH * 0.28f;

            for (int b = 0; b < bays; b++)
            {
                Vector3 bStart = p1 + dir * (b * bayWidth);
                Vector3 bEnd = p1 + dir * ((b + 1) * bayWidth);

                float xPad = (bayWidth - windowW) * 0.5f;
                Vector3 w1 = bStart + dir * xPad;
                Vector3 w2 = w1 + dir * windowW;

                float ySill = yBottom + sillH;
                float yHead = ySill + windowH;

                // 1. Wall below window
                mb.AddQuad(BuildingSubmeshSlots.Wall,
                    bStart + Vector3.up * yBottom, bEnd + Vector3.up * yBottom,
                    bEnd + Vector3.up * ySill, bStart + Vector3.up * ySill,
                    new Vector2(0f, 0f), new Vector2(bayWidth, 0f), new Vector2(bayWidth, sillH), new Vector2(0f, sillH),
                    wallCol, normal);

                // 2. Wall above window
                mb.AddQuad(BuildingSubmeshSlots.Wall,
                    bStart + Vector3.up * yHead, bEnd + Vector3.up * yHead,
                    bEnd + Vector3.up * yTop, bStart + Vector3.up * yTop,
                    new Vector2(0f, 0f), new Vector2(bayWidth, 0f), new Vector2(bayWidth, floorH - (sillH + windowH)), new Vector2(0f, floorH - (sillH + windowH)),
                    wallCol, normal);

                // 3. Wall piers on sides
                mb.AddQuad(BuildingSubmeshSlots.Wall,
                    bStart + Vector3.up * ySill, w1 + Vector3.up * ySill,
                    w1 + Vector3.up * yHead, bStart + Vector3.up * yHead,
                    Vector2.zero, new Vector2(xPad, 0), new Vector2(xPad, windowH), new Vector2(0, windowH),
                    wallCol, normal);

                mb.AddQuad(BuildingSubmeshSlots.Wall,
                    w2 + Vector3.up * ySill, bEnd + Vector3.up * ySill,
                    bEnd + Vector3.up * yHead, w2 + Vector3.up * yHead,
                    Vector2.zero, new Vector2(xPad, 0), new Vector2(xPad, windowH), new Vector2(0, windowH),
                    wallCol, normal);

                // 4. Recessed Window Glass
                Vector3 recess = -normal * 0.12f;
                Vector3 gw1 = w1 + recess + Vector3.up * ySill;
                Vector3 gw2 = w2 + recess + Vector3.up * ySill;
                Vector3 gw3 = w2 + recess + Vector3.up * yHead;
                Vector3 gw4 = w1 + recess + Vector3.up * yHead;

                mb.AddQuad(BuildingSubmeshSlots.Windows, gw1, gw2, gw3, gw4,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                    new Color(0.25f, 0.45f, 0.65f), normal);

                // 5. Projecting Sill
                Vector3 sillCenter = (w1 + w2) * 0.5f + Vector3.up * ySill + normal * 0.05f;
                mb.AddBox(BuildingSubmeshSlots.Trim, sillCenter,
                    new Vector3(windowW + 0.12f, 0.08f, 0.22f),
                    Quaternion.LookRotation(dir, Vector3.up),
                    Color.white * 0.9f);
            }
        }

        private static void BuildShopfrontSegment(MeshBuilder mb, Vector3 p1, Vector3 p2, float yBottom, float yTop, Vector3 normal, Color wallCol)
        {
            float wallLen = Vector3.Distance(p1, p2);
            Vector3 dir = (p2 - p1).normalized;

            // Transom beam height
            float beamH = 0.5f;
            float glassTop = yTop - beamH;

            // Recessed glass storefront
            Vector3 recess = -normal * 0.18f;
            Vector3 g1 = p1 + dir * 0.4f + recess + Vector3.up * yBottom;
            Vector3 g2 = p2 - dir * 0.4f + recess + Vector3.up * yBottom;
            Vector3 g3 = g2 + Vector3.up * (glassTop - yBottom);
            Vector3 g4 = g1 + Vector3.up * (glassTop - yBottom);

            mb.AddQuad(BuildingSubmeshSlots.Windows, g1, g2, g3, g4,
                Vector2.zero, new Vector2(wallLen, 0f), new Vector2(wallLen, 1f), Vector2.up,
                new Color(0.2f, 0.4f, 0.55f), normal);

            // Upper fascia signboard / transom wall
            mb.AddQuad(BuildingSubmeshSlots.Wall,
                p1 + Vector3.up * glassTop, p2 + Vector3.up * glassTop,
                p2 + Vector3.up * yTop, p1 + Vector3.up * yTop,
                Vector2.zero, new Vector2(wallLen, 0f), new Vector2(wallLen, beamH), new Vector2(0f, beamH),
                wallCol * 0.9f, normal);

            // Overhead store canopy
            Vector3 canopyPos = (p1 + p2) * 0.5f + Vector3.up * glassTop + normal * 0.6f;
            mb.AddBox(BuildingSubmeshSlots.Details, canopyPos,
                new Vector3(wallLen - 0.5f, 0.1f, 1.2f),
                Quaternion.LookRotation(dir, Vector3.up),
                new Color(0.2f, 0.22f, 0.25f));
        }

        private static void BuildFlatParapetRoof(MeshBuilder mb, List<Vector2> poly, float height)
        {
            // Flat roof surface (inset slightly for parapet)
            var inset = Polygon2D.OffsetPolygon(poly, 0.35f);
            if (inset.Count >= 3)
            {
                mb.AddHorizontalPolygon(BuildingSubmeshSlots.Roof, inset, height + 0.05f, true);
            }
            else
            {
                mb.AddHorizontalPolygon(BuildingSubmeshSlots.Roof, poly, height, true);
            }

            // Parapet Wall (raised 0.45m around perimeter)
            float parapetH = 0.45f;
            int n = poly.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 p1 = poly[i];
                Vector2 p2 = poly[(i + 1) % n];

                Vector3 b1 = new Vector3(p1.x, height, p1.y);
                Vector3 b2 = new Vector3(p2.x, height, p2.y);
                Vector3 t1 = b1 + Vector3.up * parapetH;
                Vector3 t2 = b2 + Vector3.up * parapetH;

                Vector3 dir = (b2 - b1).normalized;
                Vector3 norm = Vector3.Cross(Vector3.up, dir).normalized;

                mb.AddQuad(BuildingSubmeshSlots.Wall, b1, b2, t2, t1,
                    Vector2.zero, new Vector2(Vector3.Distance(b1, b2), 0),
                    new Vector2(Vector3.Distance(b1, b2), parapetH), new Vector2(0, parapetH),
                    Color.white * 0.85f, norm);
            }
        }

        private static void BuildGabledRoof(MeshBuilder mb, List<Vector2> poly, float height, Color wallCol)
        {
            Rect bounds = Polygon2D.GetBounds(poly);
            bool alignX = bounds.width > bounds.height;
            float ridgeH = height + Mathf.Clamp(Mathf.Min(bounds.width, bounds.height) * 0.35f, 2.5f, 6.0f);

            // Roof slope faces
            mb.AddHorizontalPolygon(BuildingSubmeshSlots.Roof, poly, height, true);

            // Triangular gable peak at center
            Vector2 centroid = bounds.center;
            Vector3 peak = new Vector3(centroid.x, ridgeH, centroid.y);

            int n = poly.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 p1 = poly[i];
                Vector2 p2 = poly[(i + 1) % n];

                Vector3 v1 = new Vector3(p1.x, height, p1.y);
                Vector3 v2 = new Vector3(p2.x, height, p2.y);

                Vector3 faceNorm = Vector3.Cross(v2 - v1, peak - v1).normalized;
                mb.AddTriangle(BuildingSubmeshSlots.Roof, v1, v2, peak,
                    Vector2.zero, Vector2.right, Vector2.one * 0.5f,
                    new Color(0.65f, 0.28f, 0.22f), faceNorm);
            }
        }

        private static void BuildHippedRoof(MeshBuilder mb, List<Vector2> poly, float height, Color wallCol)
        {
            var inset = Polygon2D.OffsetPolygon(poly, 2.5f);
            float ridgeH = height + 3.0f;

            if (inset.Count >= 3)
            {
                mb.AddHorizontalPolygon(BuildingSubmeshSlots.Roof, inset, ridgeH, true);

                int n = poly.Count;
                for (int i = 0; i < n; i++)
                {
                    int next = (i + 1) % n;
                    Vector3 b1 = new Vector3(poly[i].x, height, poly[i].y);
                    Vector3 b2 = new Vector3(poly[next].x, height, poly[next].y);
                    Vector3 t1 = new Vector3(inset[i % inset.Count].x, ridgeH, inset[i % inset.Count].y);
                    Vector3 t2 = new Vector3(inset[next % inset.Count].x, ridgeH, inset[next % inset.Count].y);

                    Vector3 norm = Vector3.Cross(b2 - b1, t1 - b1).normalized;
                    mb.AddQuad(BuildingSubmeshSlots.Roof, b1, b2, t2, t1,
                        Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                        new Color(0.62f, 0.25f, 0.20f), norm);
                }
            }
            else
            {
                BuildFlatParapetRoof(mb, poly, height);
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
