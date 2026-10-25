using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilderToolkit.Core
{
    public enum FootprintShape
    {
        Rectangle,
        LShape,
        UShape,
        TShape,
        CustomPolygon
    }

    public enum RoofStyle
    {
        FlatWithParapet,
        Gabled,
        Hipped,
        Mansard,
        RooftopTerrace,
        SkyscraperCrown
    }

    public enum FacadeStyle
    {
        ModernGlass,
        ClassicBrick,
        EuropeanPlaster,
        ContemporaryPanels,
        CommercialShopfront
    }

    public enum WindowGridPattern
    {
        StandardGrid,
        Alternating,
        RibbonWindows,
        FloorToCeiling,
        Minimal
    }

    /// <summary>
    /// Material slot indices for procedural buildings.
    /// </summary>
    public static class BuildingSubmeshSlots
    {
        public const int Wall = 0;
        public const int Glass = 1;
        public const int Roof = 2;
        public const int Trim = 3;
        public const int Door = 4;
        public const int Balcony = 5;

        // Aliases
        public const int Windows = 1;
        public const int Details = 5;
    }

    [System.Serializable]
    public class BuildingStyleConfig
    {
        public string styleName = "Default Style";
        public FacadeStyle facadeStyle = FacadeStyle.EuropeanPlaster;
        public WindowGridPattern windowPattern = WindowGridPattern.StandardGrid;
        public RoofStyle roofStyle = RoofStyle.FlatWithParapet;

        [Range(2.5f, 5.0f)]
        public float floorHeight = 3.2f;

        [Range(1.0f, 4.0f)]
        public float windowSpacing = 2.4f;

        [Range(0.6f, 2.5f)]
        public float windowWidth = 1.3f;

        [Range(0.8f, 2.5f)]
        public float windowHeight = 1.6f;

        [Range(0.02f, 0.35f)]
        public float windowRecess = 0.12f;

        public bool hasBalconies = true;
        [Range(0.0f, 1.0f)]
        public float balconyProbability = 0.45f;

        public bool hasGroundFloorCommercial = true;
        public float groundFloorHeight = 4.2f;

        public bool hasRooftopUnits = true;
        public float parapetHeight = 0.9f;

        public Color wallBaseColor = new Color(0.92f, 0.90f, 0.85f);
        public Color trimColor = new Color(0.25f, 0.28f, 0.32f);
        public Color roofColor = new Color(0.35f, 0.35f, 0.38f);
    }
}
