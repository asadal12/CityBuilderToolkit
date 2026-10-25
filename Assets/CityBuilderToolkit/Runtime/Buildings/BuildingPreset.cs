using UnityEngine;
using CityBuilderToolkit.Core;

namespace CityBuilderToolkit.Buildings
{
    [CreateAssetMenu(fileName = "NewBuildingPreset", menuName = "City Builder/Building Preset", order = 10)]
    public class BuildingPreset : ScriptableObject
    {
        [Header("Footprint & Dimensions")]
        public FootprintShape footprintShape = FootprintShape.Rectangle;
        public float width = 16f;
        public float depth = 14f;

        [Header("Height & Storeys")]
        [Range(1, 40)]
        public int minFloors = 3;
        [Range(1, 40)]
        public int maxFloors = 5;

        [Header("Facade & Architecture")]
        public BuildingStyleConfig style = new BuildingStyleConfig();

        [Header("Materials (Optional Overrides)")]
        public Material customWallMaterial;
        public Material customGlassMaterial;
        public Material customRoofMaterial;
        public Material customTrimMaterial;
    }
}
