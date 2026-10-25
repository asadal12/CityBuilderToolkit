using System.IO;
using UnityEditor;
using UnityEngine;
using CityBuilderToolkit.OSM;

namespace CityBuilderToolkit.Editor
{
    [CustomEditor(typeof(RealTownGenerator))]
    public class RealTownGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var gen = (RealTownGenerator)target;

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("🗺️ Real Town Generator (OpenStreetMap)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Generates an authentic real town from OpenStreetMap GIS data. Downloads live via Overpass API or loads local .osm / .json files.", MessageType.Info);

            EditorGUILayout.Space(4);

            EditorGUI.BeginChangeCheck();
            var newPreset = (RealTownPreset)EditorGUILayout.EnumPopup("Location Preset", gen.preset);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(gen, "Change Location Preset");
                gen.ApplyPreset(newPreset);
            }

            if (gen.preset == RealTownPreset.Custom)
            {
                gen.latitude = EditorGUILayout.DoubleField("Latitude", gen.latitude);
                gen.longitude = EditorGUILayout.DoubleField("Longitude", gen.longitude);
            }
            else
            {
                EditorGUILayout.LabelField("GPS Coordinates", $"{gen.latitude:F5}° N, {gen.longitude:F5}° E");
            }

            gen.radiusMeters = EditorGUILayout.Slider("Radius (Meters)", gen.radiusMeters, 150f, 2000f);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("🏗️ Features & Layer Toggles", EditorStyles.boldLabel);
            gen.generateBuildings = EditorGUILayout.Toggle("Generate Buildings", gen.generateBuildings);
            gen.generateRoads = EditorGUILayout.Toggle("Generate Roads & Sidewalks", gen.generateRoads);
            gen.generateParksAndWater = EditorGUILayout.Toggle("Generate Parks & Water", gen.generateParksAndWater);
            gen.spawnStreetFurniture = EditorGUILayout.Toggle("Spawn Street Furniture", gen.spawnStreetFurniture);
            gen.maxBuildings = EditorGUILayout.IntSlider("Max Buildings Limit", gen.maxBuildings, 50, 3000);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🚀 Generation Actions", EditorStyles.boldLabel);

            GUI.backgroundColor = new Color(0.3f, 0.75f, 0.45f);
            if (GUILayout.Button("🌍 Download & Generate from OpenStreetMap", GUILayout.Height(34)))
            {
                Undo.RecordObject(gen, "Generate Real Town");
                gen.GenerateFromOverpass();
            }

            GUI.backgroundColor = new Color(0.35f, 0.65f, 0.95f);
            if (GUILayout.Button("📂 Load Bundled Offline Sample (Kaiserslautern)", GUILayout.Height(28)))
            {
                string samplePath = Path.GetFullPath(Path.Combine(Application.dataPath, "CityBuilderToolkit", "Samples", "Kaiserslautern_Sample.osm"));
                if (File.Exists(samplePath))
                {
                    Undo.RecordObject(gen, "Load Sample Real Town");
                    gen.GenerateFromLocalFile(samplePath);
                }
                else
                {
                    EditorUtility.DisplayDialog("Sample Missing", $"Sample file not found at: {samplePath}", "OK");
                }
            }

            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("📁 Choose Local .osm / .json File...", GUILayout.Height(24)))
            {
                string path = EditorUtility.OpenFilePanel("Select OpenStreetMap File", Application.dataPath, "osm,json");
                if (!string.IsNullOrEmpty(path))
                {
                    Undo.RecordObject(gen, "Load Local OSM File");
                    gen.localOsmFilePath = path;
                    gen.GenerateFromLocalFile(path);
                }
            }

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
            if (GUILayout.Button("🗑️ Clear Town Geometry", GUILayout.Height(24)))
            {
                Undo.RecordObject(gen, "Clear Town");
                gen.ClearTown();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Status:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(gen.statusMessage, MessageType.None);

            if (GUI.changed)
            {
                EditorUtility.SetDirty(gen);
            }
        }
    }
}
