using System;
using UnityEngine;

namespace CityBuilderToolkit.OSM
{
    public enum RealTownPreset
    {
        KaiserslauternCenter,
        KaiserslauternRptuCampus,
        BerlinAlexanderplatz,
        ParisEiffelTower,
        LondonWestminster,
        NewYorkTimesSquare,
        TokyoShibuya,
        Custom
    }

    /// <summary>
    /// Master component for generating real-world towns and cities based on OpenStreetMap GIS data.
    /// Supports querying the live Overpass API or loading local .osm / .json files.
    /// </summary>
    [ExecuteInEditMode]
    [SelectionBase]
    public class RealTownGenerator : MonoBehaviour
    {
        [Header("🌍 Location & Coordinates")]
        public RealTownPreset preset = RealTownPreset.KaiserslauternCenter;
        public double latitude = 49.4447;
        public double longitude = 7.7690;
        [Range(100f, 2500f)]
        public float radiusMeters = 500f;

        [Header("🏗️ Features to Generate")]
        public bool generateBuildings = true;
        public bool generateRoads = true;
        public bool generateParksAndWater = true;
        public bool spawnStreetFurniture = true;
        [Range(50, 3000)]
        public int maxBuildings = 1500;

        [Header("📂 Offline / Local File Import")]
        public string localOsmFilePath = "";

        [Header("Status")]
        [TextArea(2, 4)]
        public string statusMessage = "Ready to generate real town from OpenStreetMap.";

        public void ApplyPreset(RealTownPreset newPreset)
        {
            preset = newPreset;
            switch (preset)
            {
                case RealTownPreset.KaiserslauternCenter:
                    latitude = 49.4447;
                    longitude = 7.7690;
                    radiusMeters = 500f;
                    break;
                case RealTownPreset.KaiserslauternRptuCampus:
                    latitude = 49.4245;
                    longitude = 7.7535;
                    radiusMeters = 550f;
                    break;
                case RealTownPreset.BerlinAlexanderplatz:
                    latitude = 52.5219;
                    longitude = 13.4132;
                    radiusMeters = 600f;
                    break;
                case RealTownPreset.ParisEiffelTower:
                    latitude = 48.8584;
                    longitude = 2.2945;
                    radiusMeters = 650f;
                    break;
                case RealTownPreset.LondonWestminster:
                    latitude = 51.5007;
                    longitude = -0.1246;
                    radiusMeters = 600f;
                    break;
                case RealTownPreset.NewYorkTimesSquare:
                    latitude = 40.7580;
                    longitude = -73.9855;
                    radiusMeters = 500f;
                    break;
                case RealTownPreset.TokyoShibuya:
                    latitude = 35.6595;
                    longitude = 139.7005;
                    radiusMeters = 500f;
                    break;
                case RealTownPreset.Custom:
                    break;
            }
        }

        public void ClearTown()
        {
            var children = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < transform.childCount; i++)
            {
                children.Add(transform.GetChild(i).gameObject);
            }

            foreach (var child in children)
            {
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }

            statusMessage = "Cleared town.";
        }

        public void GenerateFromOverpass(Action onComplete = null)
        {
            statusMessage = $"Downloading OSM data for Lat: {latitude:F4}, Lon: {longitude:F4}, Radius: {radiusMeters:F0}m...";
            Debug.Log($"[RealTownGenerator] {statusMessage}");

            OsmOverpassClient.FetchData(latitude, longitude, radiusMeters,
                xmlData =>
                {
                    try
                    {
                        BuildFromOsmString(xmlData, latitude, longitude);
                        statusMessage = "Successfully generated real town from Overpass API!";
                        onComplete?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        statusMessage = $"Build Error: {ex.Message}";
                        Debug.LogException(ex);
                    }
                },
                error =>
                {
                    statusMessage = $"Overpass Error: {error}";
                    Debug.LogError($"[RealTownGenerator] {statusMessage}");
                },
                (progress, msg) =>
                {
                    statusMessage = $"[{progress * 100:F0}%] {msg}";
                });
        }

        public void GenerateFromLocalFile(string filePath, Action onComplete = null)
        {
            try
            {
                statusMessage = $"Loading local OSM file: {filePath}...";
                Debug.Log($"[RealTownGenerator] {statusMessage}");

                var dataSet = OsmParser.ParseFile(filePath, (preset != RealTownPreset.Custom) ? latitude : (double?)null, (preset != RealTownPreset.Custom) ? longitude : (double?)null);
                BuildFromDataSet(dataSet);
                statusMessage = $"Successfully generated real town from {System.IO.Path.GetFileName(filePath)}!";
                onComplete?.Invoke();
            }
            catch (Exception ex)
            {
                statusMessage = $"File Load Error: {ex.Message}";
                Debug.LogException(ex);
            }
        }

        public void BuildFromOsmString(string osmText, double? centerLat = null, double? centerLon = null)
        {
            var dataSet = OsmParser.Parse(osmText, centerLat, centerLon);
            BuildFromDataSet(dataSet);
        }

        public void BuildFromDataSet(OsmDataSet dataSet)
        {
            ClearTown();

            if (generateRoads)
            {
                OsmRoadBuilder.BuildRoads(dataSet, transform);
            }

            if (generateParksAndWater)
            {
                OsmEnvironmentBuilder.BuildEnvironment(dataSet, transform, spawnStreetFurniture);
            }

            if (generateBuildings)
            {
                OsmBuildingBuilder.BuildBuildings(dataSet, transform, maxBuildings);
            }

            Debug.Log($"[RealTownGenerator] Generation finished. Center: ({dataSet.CenterLat:F5}, {dataSet.CenterLon:F5}). Extents: {dataSet.WorldBounds.size}");
        }
    }
}
