using System;
using UnityEngine;
using CityBuilderToolkit.Core;

namespace CityBuilderToolkit.Town
{
    public enum ZoneType
    {
        DowntownSkyscrapers,
        CommercialMedium,
        ResidentialApartments,
        SuburbanHouses,
        PublicPark
    }

    public static class TownZoning
    {
        public static BuildingStyleConfig GetStyleForZone(ZoneType zone, System.Random rng)
        {
            var config = new BuildingStyleConfig();

            switch (zone)
            {
                case ZoneType.DowntownSkyscrapers:
                    config.styleName = "Downtown Skyscraper";
                    config.facadeStyle = (rng.NextDouble() > 0.4) ? FacadeStyle.ModernGlass : FacadeStyle.ContemporaryPanels;
                    config.windowPattern = (rng.NextDouble() > 0.5) ? WindowGridPattern.RibbonWindows : WindowGridPattern.StandardGrid;
                    config.roofStyle = (rng.NextDouble() > 0.6) ? RoofStyle.SkyscraperCrown : RoofStyle.FlatWithParapet;
                    config.floorHeight = 3.6f;
                    config.groundFloorHeight = 5.0f;
                    config.hasGroundFloorCommercial = true;
                    config.hasBalconies = false;
                    config.hasRooftopUnits = true;
                    config.wallBaseColor = (rng.NextDouble() > 0.5)
                        ? new Color(0.22f, 0.25f, 0.28f) // Dark Anthracite
                        : new Color(0.85f, 0.86f, 0.88f); // High-tech Silver
                    config.trimColor = new Color(0.15f, 0.16f, 0.18f);
                    break;

                case ZoneType.CommercialMedium:
                    config.styleName = "Commercial Office";
                    config.facadeStyle = FacadeStyle.CommercialShopfront;
                    config.windowPattern = WindowGridPattern.StandardGrid;
                    config.roofStyle = RoofStyle.FlatWithParapet;
                    config.floorHeight = 3.3f;
                    config.groundFloorHeight = 4.4f;
                    config.hasGroundFloorCommercial = true;
                    config.hasBalconies = false;
                    config.hasRooftopUnits = true;
                    config.wallBaseColor = new Color(0.88f, 0.85f, 0.80f);
                    config.trimColor = new Color(0.25f, 0.26f, 0.28f);
                    break;

                case ZoneType.ResidentialApartments:
                    config.styleName = "Residential Apartment";
                    config.facadeStyle = FacadeStyle.EuropeanPlaster;
                    config.windowPattern = WindowGridPattern.StandardGrid;
                    config.roofStyle = (rng.NextDouble() > 0.5) ? RoofStyle.Gabled : RoofStyle.FlatWithParapet;
                    config.floorHeight = 3.0f;
                    config.groundFloorHeight = 3.2f;
                    config.hasGroundFloorCommercial = rng.NextDouble() < 0.35f;
                    config.hasBalconies = true;
                    config.balconyProbability = 0.65f;
                    config.hasRooftopUnits = false;

                    // Pastel European Plaster Palette
                    Color[] residentialColors = new Color[]
                    {
                        new Color(0.92f, 0.88f, 0.82f), // Cream
                        new Color(0.85f, 0.82f, 0.78f), // Sandstone
                        new Color(0.88f, 0.76f, 0.70f), // Warm Terracotta Plaster
                        new Color(0.78f, 0.84f, 0.86f), // Light Sky Grey
                        new Color(0.82f, 0.86f, 0.80f)  // Sage Tint
                    };
                    config.wallBaseColor = residentialColors[rng.Next(residentialColors.Length)];
                    config.trimColor = new Color(0.28f, 0.28f, 0.30f);
                    config.roofColor = new Color(0.65f, 0.28f, 0.22f); // Terracotta tiles
                    break;

                case ZoneType.SuburbanHouses:
                    config.styleName = "Suburban House";
                    config.facadeStyle = FacadeStyle.ClassicBrick;
                    config.windowPattern = WindowGridPattern.StandardGrid;
                    config.roofStyle = RoofStyle.Gabled;
                    config.floorHeight = 2.9f;
                    config.hasGroundFloorCommercial = false;
                    config.hasBalconies = false;
                    config.wallBaseColor = new Color(0.75f, 0.40f, 0.30f); // Red Brick
                    config.roofColor = new Color(0.35f, 0.25f, 0.22f);
                    break;

                default:
                    config.wallBaseColor = Color.white;
                    break;
            }

            return config;
        }

        public static int GetFloorCountForZone(ZoneType zone, System.Random rng)
        {
            switch (zone)
            {
                case ZoneType.DowntownSkyscrapers:
                    return rng.Next(10, 26);
                case ZoneType.CommercialMedium:
                    return rng.Next(4, 9);
                case ZoneType.ResidentialApartments:
                    return rng.Next(3, 6);
                case ZoneType.SuburbanHouses:
                    return rng.Next(1, 3);
                default:
                    return 3;
            }
        }
    }
}
