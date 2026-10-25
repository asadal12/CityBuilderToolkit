using System.Collections.Generic;
using UnityEngine;
using CityBuilderToolkit.Core;

namespace CityBuilderToolkit.Materials
{
    /// <summary>
    /// Automatic procedural material and shader manager.
    /// Automatically detects Universal Render Pipeline (URP) or Built-in pipeline,
    /// and generates curated architectural materials.
    /// </summary>
    public static class CityMaterialPalette
    {
        private static Shader _litShader;
        private static readonly Dictionary<string, Material> _materialCache = new Dictionary<string, Material>();

        public static Shader GetActiveLitShader()
        {
            if (_litShader != null) return _litShader;

            _litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (_litShader == null) _litShader = Shader.Find("Standard");
            if (_litShader == null) _litShader = Shader.Find("Mobile/Diffuse");
            if (_litShader == null) _litShader = Shader.Find("Diffuse");

            return _litShader;
        }

        public static Material CreateMaterial(string name, Color color, float smoothness = 0.2f, float metallic = 0.0f)
        {
            string key = $"{name}_{color.r:F2}_{color.g:F2}_{color.b:F2}_{smoothness:F2}_{metallic:F2}";
            if (_materialCache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var mat = new Material(GetActiveLitShader())
            {
                name = name,
                color = color
            };

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            _materialCache[key] = mat;
            return mat;
        }

        // =========================================================================
        // PRESET ARCHITECTURAL PALETTES
        // =========================================================================

        public static Material GetWallMaterial(Color tint)
        {
            return CreateMaterial("Mat_City_Wall", tint, 0.25f, 0.0f);
        }

        public static Material GetGlassMaterial()
        {
            Color glassColor = new Color(0.18f, 0.24f, 0.32f, 0.95f);
            return CreateMaterial("Mat_City_Glass", glassColor, 0.92f, 0.85f);
        }

        public static Material GetRoofMaterial(Color tint)
        {
            return CreateMaterial("Mat_City_Roof", tint, 0.15f, 0.05f);
        }

        public static Material GetTrimMaterial(Color tint)
        {
            return CreateMaterial("Mat_City_Trim", tint, 0.4f, 0.2f);
        }

        public static Material GetDoorMaterial()
        {
            Color doorColor = new Color(0.20f, 0.22f, 0.25f);
            return CreateMaterial("Mat_City_Door", doorColor, 0.35f, 0.3f);
        }

        public static Material GetBalconyMaterial()
        {
            Color balconyColor = new Color(0.30f, 0.32f, 0.35f);
            return CreateMaterial("Mat_City_Balcony", balconyColor, 0.5f, 0.6f);
        }

        public static Material GetAsphaltMaterial()
        {
            Color asphalt = new Color(0.22f, 0.23f, 0.25f);
            return CreateMaterial("Mat_City_Asphalt", asphalt, 0.15f, 0.0f);
        }

        public static Material GetSidewalkMaterial()
        {
            Color sidewalk = new Color(0.72f, 0.70f, 0.68f);
            return CreateMaterial("Mat_City_Sidewalk", sidewalk, 0.20f, 0.0f);
        }

        public static Material GetCurbMaterial()
        {
            Color curb = new Color(0.58f, 0.57f, 0.55f);
            return CreateMaterial("Mat_City_Curb", curb, 0.25f, 0.0f);
        }

        public static Material GetRoadMarkingMaterial()
        {
            Color marking = new Color(0.96f, 0.96f, 0.94f);
            return CreateMaterial("Mat_City_Marking", marking, 0.3f, 0.0f);
        }

        public static Material GetGrassMaterial()
        {
            Color grass = new Color(0.32f, 0.52f, 0.26f);
            return CreateMaterial("Mat_City_Grass", grass, 0.1f, 0.0f);
        }

        public static Material GetWaterMaterial()
        {
            Color water = new Color(0.20f, 0.40f, 0.55f);
            return CreateMaterial("Mat_City_Water", water, 0.9f, 0.1f);
        }

        // Aliases for compatibility
        public static Material CreateAsphaltMaterial() => GetAsphaltMaterial();
        public static Material CreateSidewalkMaterial() => GetSidewalkMaterial();
        public static Material CreateCurbMaterial() => GetCurbMaterial();
        public static Material CreateRoadMarkingMaterial() => GetRoadMarkingMaterial();
        public static Material CreateGrassMaterial() => GetGrassMaterial();
        public static Material CreateWaterMaterial() => GetWaterMaterial();

        public static Material[] GetBuildingMaterials(BuildingStyleConfig style)
        {
            return new Material[]
            {
                GetWallMaterial(style.wallBaseColor),
                GetGlassMaterial(),
                GetRoofMaterial(style.roofColor),
                GetTrimMaterial(style.trimColor),
                GetDoorMaterial(),
                GetBalconyMaterial()
            };
        }

        public static Material[] GetDefaultBuildingMaterials()
        {
            return new Material[]
            {
                GetWallMaterial(new Color(0.85f, 0.82f, 0.77f)),
                GetGlassMaterial(),
                GetRoofMaterial(new Color(0.35f, 0.35f, 0.37f)),
                GetTrimMaterial(new Color(0.9f, 0.9f, 0.9f)),
                GetDoorMaterial(),
                GetBalconyMaterial()
            };
        }
    }
}
