using System.Collections.Generic;
using UnityEngine;
using CityBuilderToolkit.Core;

namespace CityBuilderToolkit.Buildings
{
    /// <summary>
    /// Generates detailed procedural building facades, windows, doors, balconies,
    /// and architectural ornaments onto a MeshBuilder.
    /// </summary>
    public static class FacadeGenerator
    {
        public static void GenerateWallFacade(
            MeshBuilder mb,
            Vector3 start,
            Vector3 end,
            float floorBottomY,
            float floorTopY,
            int floorIndex,
            int totalFloors,
            BuildingStyleConfig style,
            System.Random rng)
        {
            float floorHeight = floorTopY - floorBottomY;
            Vector3 dir = (end - start);
            float wallLength = dir.magnitude;
            if (wallLength < 1.0f) return;

            Vector3 unitDir = dir / wallLength;
            // Outward normal (assuming CCW polygon winding around Y-up)
            Vector3 outwardNormal = new Vector3(-unitDir.z, 0f, unitDir.x).normalized;

            bool isGroundFloor = (floorIndex == 0);
            bool isTopFloor = (floorIndex == totalFloors - 1);

            float cornerMargin = Mathf.Min(0.8f, wallLength * 0.15f);
            float usableLength = wallLength - (cornerMargin * 2f);

            int windowCount = Mathf.Max(1, Mathf.FloorToInt(usableLength / style.windowSpacing));
            float bayWidth = usableLength / windowCount;

            // 1. Generate Left and Right Corner Pillars
            Vector3 leftCornerEnd = start + unitDir * cornerMargin;
            Vector3 rightCornerStart = end - unitDir * cornerMargin;

            mb.AddExtrudedWall(BuildingSubmeshSlots.Wall, start, leftCornerEnd, floorBottomY, floorTopY);
            mb.AddExtrudedWall(BuildingSubmeshSlots.Wall, rightCornerStart, end, floorBottomY, floorTopY);

            // 2. Floor Separator Trim / Cornice Band
            float trimThickness = 0.15f;
            float trimProjection = 0.08f;
            Vector3 trimStart = start + outwardNormal * trimProjection;
            Vector3 trimEnd = end + outwardNormal * trimProjection;
            mb.AddExtrudedWall(BuildingSubmeshSlots.Trim, trimStart, trimEnd, floorBottomY, floorBottomY + trimThickness);

            // 3. Generate Window / Door Bays
            for (int b = 0; b < windowCount; b++)
            {
                float bayStartX = cornerMargin + b * bayWidth;
                float bayEndX = bayStartX + bayWidth;

                Vector3 pBayStart = start + unitDir * bayStartX;
                Vector3 pBayEnd = start + unitDir * bayEndX;

                float winW = Mathf.Min(style.windowWidth, bayWidth * 0.75f);
                float pierW = (bayWidth - winW) * 0.5f;

                Vector3 pPierLeftEnd = pBayStart + unitDir * pierW;
                Vector3 pPierRightStart = pBayEnd - unitDir * pierW;

                // Solid side piers
                mb.AddExtrudedWall(BuildingSubmeshSlots.Wall, pBayStart, pPierLeftEnd, floorBottomY, floorTopY);
                mb.AddExtrudedWall(BuildingSubmeshSlots.Wall, pPierRightStart, pBayEnd, floorBottomY, floorTopY);

                if (isGroundFloor && style.hasGroundFloorCommercial)
                {
                    // Commercial Shopfront / Entrance
                    GenerateShopfrontBay(mb, pPierLeftEnd, pPierRightStart, floorBottomY, floorTopY, outwardNormal, b == 0, style);
                }
                else
                {
                    // Standard Upper Floor Window Bay
                    GenerateWindowBay(mb, pPierLeftEnd, pPierRightStart, floorBottomY, floorTopY, outwardNormal, floorIndex, style, rng);
                }
            }
        }

        private static void GenerateWindowBay(
            MeshBuilder mb,
            Vector3 pLeft,
            Vector3 pRight,
            float bottomY,
            float topY,
            Vector3 normal,
            int floorIndex,
            BuildingStyleConfig style,
            System.Random rng)
        {
            float floorHeight = topY - bottomY;
            float sillHeight = Mathf.Min(0.85f, floorHeight * 0.28f);
            float winH = Mathf.Min(style.windowHeight, floorHeight - sillHeight - 0.5f);
            float lintelHeight = floorHeight - (sillHeight + winH);

            float winBottomY = bottomY + sillHeight;
            float winTopY = winBottomY + winH;

            // Lower Spandrel / Sill Wall
            mb.AddExtrudedWall(BuildingSubmeshSlots.Wall, pLeft, pRight, bottomY, winBottomY);

            // Upper Lintel Wall
            mb.AddExtrudedWall(BuildingSubmeshSlots.Wall, pLeft, pRight, winTopY, topY);

            // Window Sill Projection Ledge
            float sillProjection = 0.08f;
            Vector3 sillLeft = pLeft + normal * sillProjection;
            Vector3 sillRight = pRight + normal * sillProjection;
            mb.AddExtrudedWall(BuildingSubmeshSlots.Trim, sillLeft, sillRight, winBottomY - 0.08f, winBottomY);

            // Recessed Window Glass
            Vector3 glassLeft = pLeft - normal * style.windowRecess;
            Vector3 glassRight = pRight - normal * style.windowRecess;
            mb.AddExtrudedWall(BuildingSubmeshSlots.Glass, glassLeft, glassRight, winBottomY, winTopY);

            // Window Reveal Walls (sides and soffit of recess)
            mb.AddExtrudedWall(BuildingSubmeshSlots.Trim, pLeft, glassLeft, winBottomY, winTopY);
            mb.AddExtrudedWall(BuildingSubmeshSlots.Trim, glassRight, pRight, winBottomY, winTopY);

            // Optional Balcony
            if (style.hasBalconies && floorIndex > 0 && rng.NextDouble() < style.balconyProbability)
            {
                GenerateBalcony(mb, pLeft, pRight, winBottomY, normal);
            }
        }

        private static void GenerateShopfrontBay(
            MeshBuilder mb,
            Vector3 pLeft,
            Vector3 pRight,
            float bottomY,
            float topY,
            Vector3 normal,
            bool isMainEntrance,
            BuildingStyleConfig style)
        {
            float transomY = topY - 0.7f;
            float lintelY = topY;

            // Upper fascia / signage board
            mb.AddExtrudedWall(BuildingSubmeshSlots.Trim, pLeft + normal * 0.05f, pRight + normal * 0.05f, transomY, lintelY);

            // Glass door or panoramic shop window
            Vector3 glassLeft = pLeft - normal * (style.windowRecess * 0.5f);
            Vector3 glassRight = pRight - normal * (style.windowRecess * 0.5f);

            int slot = isMainEntrance ? BuildingSubmeshSlots.Door : BuildingSubmeshSlots.Glass;
            mb.AddExtrudedWall(slot, glassLeft, glassRight, bottomY, transomY);

            // Entrance canopy over main door
            if (isMainEntrance)
            {
                Vector3 center = (pLeft + pRight) * 0.5f + normal * 1.0f + Vector3.up * transomY;
                float width = Vector3.Distance(pLeft, pRight) + 0.6f;
                mb.AddBox(BuildingSubmeshSlots.Trim, center, new Vector3(width, 0.15f, 1.8f));
            }
        }

        private static void GenerateBalcony(
            MeshBuilder mb,
            Vector3 pLeft,
            Vector3 pRight,
            float floorY,
            Vector3 normal)
        {
            float projection = 1.1f;
            float slabThickness = 0.18f;
            float railingHeight = 0.95f;

            Vector3 center = (pLeft + pRight) * 0.5f + normal * (projection * 0.5f);
            float width = Vector3.Distance(pLeft, pRight) + 0.3f;

            // Balcony Concrete Floor Slab
            mb.AddBox(BuildingSubmeshSlots.Trim, center + Vector3.up * (floorY - slabThickness * 0.5f),
                new Vector3(width, slabThickness, projection));

            // Balcony Railing Walls
            Vector3 frontCenter = center + normal * (projection * 0.5f);
            mb.AddBox(BuildingSubmeshSlots.Balcony, frontCenter + Vector3.up * (floorY + railingHeight * 0.5f),
                new Vector3(width, railingHeight, 0.05f));

            Vector3 sideDir = Vector3.Cross(normal, Vector3.up).normalized;
            Vector3 leftCenter = center - sideDir * (width * 0.5f);
            Vector3 rightCenter = center + sideDir * (width * 0.5f);

            mb.AddBox(BuildingSubmeshSlots.Balcony, leftCenter + Vector3.up * (floorY + railingHeight * 0.5f),
                new Vector3(0.05f, railingHeight, projection));
            mb.AddBox(BuildingSubmeshSlots.Balcony, rightCenter + Vector3.up * (floorY + railingHeight * 0.5f),
                new Vector3(0.05f, railingHeight, projection));
        }
    }
}
