using System.IO;
using UnityEditor;
using UnityEngine;
using CityBuilderToolkit.Town;
using CityBuilderToolkit.Buildings;
using CityBuilderToolkit.Core;
using CityBuilderToolkit.OSM;

namespace CityBuilderToolkit.Editor
{
    /// <summary>
    /// Interactive Editor Window for both Real-World (OpenStreetMap) and Procedural Grid Town generation.
    /// Provides one-click city creation, presets, and customized building spawner.
    /// </summary>
    public class TownGeneratorWindow : EditorWindow
    {
        private int _selectedTab = 0;
        private readonly string[] _tabNames = new string[] { "🗺️ Real Town (OSM)", "🏙️ Grid Town", "🏢 Single Building", "📖 Guide" };

        // OSM Real Town Settings
        private RealTownPreset _osmPreset = RealTownPreset.KaiserslauternCenter;
        private double _osmLat = 49.4447;
        private double _osmLon = 7.7690;
        private float _osmRadius = 500f;
        private bool _osmBuildings = true;
        private bool _osmRoads = true;
        private bool _osmEnvironment = true;
        private bool _osmFurniture = true;
        private int _osmMaxBuildings = 1500;
        private string _osmStatus = "Ready to generate real town from OpenStreetMap.";

        // Procedural Grid Town Settings
        private int _blocksX = 3;
        private int _blocksZ = 3;
        private float _blockSizeX = 50f;
        private float _blockSizeZ = 50f;
        private float _roadWidth = 10f;
        private float _sidewalkWidth = 2.5f;
        private bool _spawnTrees = true;
        private bool _spawnLights = true;
        private bool _spawnBenches = true;
        private int _townSeed = 1337;

        // Single Building Settings
        private FootprintShape _buildingShape = FootprintShape.Rectangle;
        private float _buildingWidth = 16f;
        private float _buildingDepth = 14f;
        private int _buildingFloors = 4;
        private RoofStyle _buildingRoof = RoofStyle.FlatWithParapet;
        private FacadeStyle _buildingFacade = FacadeStyle.EuropeanPlaster;
        private bool _buildingBalconies = true;
        private bool _buildingCommercial = true;

        private Vector2 _scrollPos;

        [MenuItem("Tools/City & Building Generator", priority = 100)]
        [MenuItem("Window/City & Building Generator", priority = 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<TownGeneratorWindow>("City Generator");
            window.minSize = new Vector2(420, 580);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("City & Building Generator Toolkit", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Real-World OpenStreetMap GIS & Procedural Architecture", EditorStyles.miniLabel);
            EditorGUILayout.Space(6);

            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames, GUILayout.Height(28));
            EditorGUILayout.Space(10);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            switch (_selectedTab)
            {
                case 0:
                    DrawRealTownTab();
                    break;
                case 1:
                    DrawGridTownTab();
                    break;
                case 2:
                    DrawSingleBuildingTab();
                    break;
                case 3:
                    DrawGuideTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        #region Real Town (OSM) Tab
        private void DrawRealTownTab()
        {
            EditorGUILayout.LabelField("🌍 Real-World Town from OpenStreetMap", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Generate an accurate 1:1 3D city from real-world OpenStreetMap GIS data. Downloads live via Overpass API or loads local .osm / .json files.", MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUI.BeginChangeCheck();
            _osmPreset = (RealTownPreset)EditorGUILayout.EnumPopup("Location Preset", _osmPreset);
            if (EditorGUI.EndChangeCheck())
            {
                ApplyOsmPreset(_osmPreset);
            }

            if (_osmPreset == RealTownPreset.Custom)
            {
                _osmLat = EditorGUILayout.DoubleField("Latitude (°N)", _osmLat);
                _osmLon = EditorGUILayout.DoubleField("Longitude (°E)", _osmLon);
            }
            else
            {
                EditorGUILayout.LabelField("GPS Coordinates", $"{_osmLat:F5}° N, {_osmLon:F5}° E");
            }

            _osmRadius = EditorGUILayout.Slider("Radius (Meters)", _osmRadius, 150f, 2000f);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Layers & Options", EditorStyles.boldLabel);
            _osmBuildings = EditorGUILayout.Toggle("Generate Real Buildings", _osmBuildings);
            _osmRoads = EditorGUILayout.Toggle("Generate Real Roads & Sidewalks", _osmRoads);
            _osmEnvironment = EditorGUILayout.Toggle("Generate Parks & Waterways", _osmEnvironment);
            _osmFurniture = EditorGUILayout.Toggle("Spawn Streetlights & Benches", _osmFurniture);
            _osmMaxBuildings = EditorGUILayout.IntSlider("Max Buildings Limit", _osmMaxBuildings, 50, 3000);

            EditorGUILayout.Space(12);

            GUI.backgroundColor = new Color(0.28f, 0.75f, 0.42f);
            if (GUILayout.Button("🌍 Download & Generate Real Town (Overpass API)", GUILayout.Height(36)))
            {
                GenerateRealTownLive();
            }

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(0.35f, 0.65f, 0.95f);
            if (GUILayout.Button("📂 Load Bundled Offline Sample (Kaiserslautern)", GUILayout.Height(28)))
            {
                LoadBundledSample();
            }

            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("📁 Select Local .osm / .json File...", GUILayout.Height(26)))
            {
                string path = EditorUtility.OpenFilePanel("Select OpenStreetMap File", Application.dataPath, "osm,json");
                if (!string.IsNullOrEmpty(path))
                {
                    LoadLocalFile(path);
                }
            }

            EditorGUILayout.Space(6);
            GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
            if (GUILayout.Button("🗑️ Clear Real Town", GUILayout.Height(24)))
            {
                ClearRealTown();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Status:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(_osmStatus, MessageType.None);
        }

        private void ApplyOsmPreset(RealTownPreset preset)
        {
            switch (preset)
            {
                case RealTownPreset.KaiserslauternCenter:
                    _osmLat = 49.4447; _osmLon = 7.7690; _osmRadius = 500f; break;
                case RealTownPreset.KaiserslauternRptuCampus:
                    _osmLat = 49.4245; _osmLon = 7.7535; _osmRadius = 550f; break;
                case RealTownPreset.BerlinAlexanderplatz:
                    _osmLat = 52.5219; _osmLon = 13.4132; _osmRadius = 600f; break;
                case RealTownPreset.ParisEiffelTower:
                    _osmLat = 48.8584; _osmLon = 2.2945; _osmRadius = 650f; break;
                case RealTownPreset.LondonWestminster:
                    _osmLat = 51.5007; _osmLon = -0.1246; _osmRadius = 600f; break;
                case RealTownPreset.NewYorkTimesSquare:
                    _osmLat = 40.7580; _osmLon = -73.9855; _osmRadius = 500f; break;
                case RealTownPreset.TokyoShibuya:
                    _osmLat = 35.6595; _osmLon = 139.7005; _osmRadius = 500f; break;
            }
        }

        private RealTownGenerator GetOrCreateRealTownGenerator()
        {
            var gen = FindAnyObjectByType<RealTownGenerator>();
            if (gen == null)
            {
                var go = new GameObject("Real_Town_OSM");
                gen = go.AddComponent<RealTownGenerator>();
                Undo.RegisterCreatedObjectUndo(go, "Create Real Town Generator");
            }
            gen.preset = _osmPreset;
            gen.latitude = _osmLat;
            gen.longitude = _osmLon;
            gen.radiusMeters = _osmRadius;
            gen.generateBuildings = _osmBuildings;
            gen.generateRoads = _osmRoads;
            gen.generateParksAndWater = _osmEnvironment;
            gen.spawnStreetFurniture = _osmFurniture;
            gen.maxBuildings = _osmMaxBuildings;
            return gen;
        }

        private void GenerateRealTownLive()
        {
            var gen = GetOrCreateRealTownGenerator();
            _osmStatus = "Downloading OSM data from Overpass API...";
            Repaint();
            gen.GenerateFromOverpass(() =>
            {
                _osmStatus = gen.statusMessage;
                Repaint();
            });
        }

        private void LoadBundledSample()
        {
            string samplePath = Path.GetFullPath(Path.Combine(Application.dataPath, "CityBuilderToolkit", "Samples", "Kaiserslautern_Sample.osm"));
            if (File.Exists(samplePath))
            {
                var gen = GetOrCreateRealTownGenerator();
                gen.GenerateFromLocalFile(samplePath);
                _osmStatus = gen.statusMessage;
            }
            else
            {
                EditorUtility.DisplayDialog("Sample Missing", $"Sample file not found at: {samplePath}", "OK");
            }
        }

        private void LoadLocalFile(string path)
        {
            var gen = GetOrCreateRealTownGenerator();
            gen.GenerateFromLocalFile(path);
            _osmStatus = gen.statusMessage;
        }

        private void ClearRealTown()
        {
            var gen = FindAnyObjectByType<RealTownGenerator>();
            if (gen != null)
            {
                gen.ClearTown();
                _osmStatus = "Cleared real town.";
            }
        }
        #endregion

        #region Grid Town Tab
        private void DrawGridTownTab()
        {
            EditorGUILayout.LabelField("🏙️ Procedural Grid Layout", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _blocksX = EditorGUILayout.IntSlider("Blocks (X-Axis)", _blocksX, 1, 10);
            _blocksZ = EditorGUILayout.IntSlider("Blocks (Z-Axis)", _blocksZ, 1, 10);
            _blockSizeX = EditorGUILayout.Slider("Block Size X (m)", _blockSizeX, 30f, 100f);
            _blockSizeZ = EditorGUILayout.Slider("Block Size Z (m)", _blockSizeZ, 30f, 100f);
            _roadWidth = EditorGUILayout.Slider("Road Width (m)", _roadWidth, 6f, 20f);
            _sidewalkWidth = EditorGUILayout.Slider("Sidewalk Width (m)", _sidewalkWidth, 1.5f, 5f);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Street Furniture & Props", EditorStyles.boldLabel);
            _spawnTrees = EditorGUILayout.Toggle("Spawn Avenue Trees", _spawnTrees);
            _spawnLights = EditorGUILayout.Toggle("Spawn Street Lights", _spawnLights);
            _spawnBenches = EditorGUILayout.Toggle("Spawn Park Benches", _spawnBenches);
            _townSeed = EditorGUILayout.IntField("Random Seed", _townSeed);

            EditorGUILayout.Space(12);

            GUI.backgroundColor = new Color(0.3f, 0.75f, 0.45f);
            if (GUILayout.Button("🚀 Generate Procedural Grid Town", GUILayout.Height(36)))
            {
                GenerateGridTown();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(4);
            if (GUILayout.Button("🎲 Randomize Seed & Regenerate", GUILayout.Height(26)))
            {
                _townSeed = Random.Range(1000, 99999);
                GenerateGridTown();
            }

            GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
            if (GUILayout.Button("🗑️ Clear Town", GUILayout.Height(24)))
            {
                ClearGridTown();
            }
            GUI.backgroundColor = Color.white;
        }

        private void GenerateGridTown()
        {
            var townGen = FindAnyObjectByType<TownGenerator>();
            if (townGen == null)
            {
                var go = new GameObject("Procedural_Town");
                townGen = go.AddComponent<TownGenerator>();
                Undo.RegisterCreatedObjectUndo(go, "Create Town Generator");
            }

            townGen.blocksX = _blocksX;
            townGen.blocksZ = _blocksZ;
            townGen.blockSizeX = _blockSizeX;
            townGen.blockSizeZ = _blockSizeZ;
            townGen.roadWidth = _roadWidth;
            townGen.sidewalkWidth = _sidewalkWidth;
            townGen.spawnStreetTrees = _spawnTrees;
            townGen.spawnStreetLights = _spawnLights;
            townGen.spawnParkBenches = _spawnBenches;
            townGen.randomSeed = _townSeed;

            townGen.GenerateTown();
            Selection.activeGameObject = townGen.gameObject;
        }

        private void ClearGridTown()
        {
            var townGen = FindAnyObjectByType<TownGenerator>();
            if (townGen != null)
            {
                townGen.ClearTown();
            }
        }
        #endregion

        #region Single Building Tab
        private void DrawSingleBuildingTab()
        {
            EditorGUILayout.LabelField("🏢 Individual Building Spawner", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _buildingShape = (FootprintShape)EditorGUILayout.EnumPopup("Footprint Shape", _buildingShape);
            _buildingWidth = EditorGUILayout.Slider("Width (m)", _buildingWidth, 6f, 50f);
            _buildingDepth = EditorGUILayout.Slider("Depth (m)", _buildingDepth, 6f, 50f);
            _buildingFloors = EditorGUILayout.IntSlider("Floors", _buildingFloors, 1, 35);
            _buildingRoof = (RoofStyle)EditorGUILayout.EnumPopup("Roof Style", _buildingRoof);
            _buildingFacade = (FacadeStyle)EditorGUILayout.EnumPopup("Facade Style", _buildingFacade);
            _buildingBalconies = EditorGUILayout.Toggle("Add Balconies", _buildingBalconies);
            _buildingCommercial = EditorGUILayout.Toggle("Ground Shopfronts", _buildingCommercial);

            EditorGUILayout.Space(12);
            GUI.backgroundColor = new Color(0.35f, 0.65f, 0.95f);
            if (GUILayout.Button("🏗️ Spawn Building at Scene Center", GUILayout.Height(36)))
            {
                SpawnSingleBuilding();
            }
            GUI.backgroundColor = Color.white;
        }

        private void SpawnSingleBuilding()
        {
            var go = new GameObject($"Building_{_buildingShape}_{_buildingFloors}F");
            var bg = go.AddComponent<BuildingGenerator>();

            bg.shape = _buildingShape;
            bg.width = _buildingWidth;
            bg.depth = _buildingDepth;
            bg.floors = _buildingFloors;
            bg.style.roofStyle = _buildingRoof;
            bg.style.facadeStyle = _buildingFacade;
            bg.style.hasBalconies = _buildingBalconies;
            bg.style.hasGroundFloorCommercial = _buildingCommercial;

            bg.Generate();
            Undo.RegisterCreatedObjectUndo(go, "Spawn Single Building");
            Selection.activeGameObject = go;
        }
        #endregion

        #region Guide Tab
        private void DrawGuideTab()
        {
            EditorGUILayout.LabelField("📖 City & Building Generator Quick Guide", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "• 🗺️ Real Town (OSM): Fetch real-world GIS data from OpenStreetMap. Enter coordinates or pick presets (Kaiserslautern, Berlin, Paris, NYC, Tokyo).\n\n" +
                "• 🏙️ Grid Town: Generates an American/European grid city with zoned downtown skyscrapers, residential blocks, and parks.\n\n" +
                "• 🏢 Single Building: Spawn customizable buildings with custom footprints, balconies, glass shopfronts, and roofs.\n\n" +
                "• Scene View Vertex Handles: When a building uses CustomPolygon shape, drag the 3D handles directly in the Scene View!",
                MessageType.Info
            );
        }
        #endregion
    }
}
