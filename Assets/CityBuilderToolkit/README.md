# 🏙️ City & Building Generator Toolkit for Unity (with OpenStreetMap)

[![Unity 6 / 2022.3+](https://img.shields.io/badge/Unity-2022.3%20%7C%20Unity%206-blue.svg)](https://unity.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Zero Dependencies](https://img.shields.io/badge/Dependencies-0%20(Pure%20C%23)-brightgreen.svg)]()
[![OpenStreetMap](https://img.shields.io/badge/GIS-OpenStreetMap%20%2F%20Overpass-orange.svg)](https://www.openstreetmap.org)
[![Pipelines](https://img.shields.io/badge/Render%20Pipeline-URP%20%7C%20Built--in-purple.svg)]()

A modular, zero-dependency procedural 3D town, building, and GIS city generator for Unity. Designed for game developers, urban designers, and indie creators who want to **generate 1:1 real-world towns from OpenStreetMap (OSM)** or create **custom procedural grid cities and standalone buildings** with a single click.

---

## 📸 Showcase

### 🌍 Real-World Town (OpenStreetMap GIS Extrusion)
![Real OSM Town Aerial](Documentation/Images/real_osm_town_aerial.png)
*231 real-world building footprints, multi-storey towers, and 184 curving road segments extruded accurately from OpenStreetMap GIS data in Unity.*

![Real OSM Town Street Level](Documentation/Images/real_osm_town_street.png)
*Street perspective showing ground-floor commercial storefronts, recessed windows, concrete curbs, and pedestrian sidewalks.*

### 🏙️ Procedural Grid Cities
![Procedural City Overview](Documentation/Images/city_builder_toolkit_demo_town.png)
*Procedural grid city with zoned downtown skyscrapers, setbacks, pitched roofs, avenue trees, and streetlights.*

---

## ⚡ Highlights & Features

- **🗺️ Real-World OpenStreetMap Integration:**
  - **Live Overpass API Downloader:** Stream real-world GIS vector data directly inside the Unity Editor or in Play mode for any location globally.
  - **Offline / Local File Importer:** Open standard .osm (XML) or .json files exported directly from [OpenStreetMap.org](https://www.openstreetmap.org).
  - **Bundled Offline Sample:** Comes pre-packaged with a 1MB real-world dataset (Kaiserslautern, Germany) to test immediately without an internet connection.
  - **1:1 Geographic Projection:** Accurate Equirectangular/Mercator projection centered at your target coordinates (1 unit = 1 meter).
  - **Real Building Footprints:** Ear-clipping triangulation of arbitrary polygon footprints, multi-floor subdivisions, window bays, commercial shopfronts, and roofs (flat parapet, gabled, hipped).
  - **Real Road Networks:** Extruded 3D road ribbons along highway vectors with asphalt driving surface, raised concrete curbs (+0.15m), and pedestrian sidewalks.
  - **Real Parks & Water Features:** Triangulated green parks (leisure=park, landuse=grass), lakes, rivers, and streetlights.
- **🏢 Procedural Building Generator:**
  - Multiple footprint shapes: **Rectangle**, **L-Shape**, **U-Shape**, **T-Shape**, and **Custom Polygon**.
  - **Scene View 3D Vertex Handles:** Drag vertices directly in the Scene View to sculpt custom footprint polygons in real-time.
  - Multi-style facades: Modern Glass, Contemporary Panels, European Plaster, and Brick.
  - Architectural details: Recessed windows, sills, lintels, private balconies with metal railings, commercial glass shopfronts, and entrance canopies.
  - Diverse roof architectures: Flat with parapet & AC chillers/bulkheads, Gabled/Pitched, Hipped, and Skyscraper Crown.
  - **One-Click Prefab Export:** Save any generated building directly to your Assets folder as a reusable prefab.
- **🛣️ Procedural Grid Town Layout:**
  - Customizable grid size (locksX × locksZ), block dimensions, and road widths.
  - Zoning system: Downtown Skyscrapers, Commercial Mid-Rise, Residential Apartments, Suburban Houses, and Public Park Plazas.
- **🎨 Universal Pipeline Support & Zero Dependencies:**
  - Works out of the box with **Universal Render Pipeline (URP)** and **Built-in Render Pipeline**.
  - **100% pure C#** with standard Unity Mesh and GameObject APIs. No external DLLs, Python scripts, or third-party packages required.

---

## 📥 Installation

### Method 1: Open Standalone Project in Unity Hub (Recommended)
1. Clone this repository:
   `ash
   git clone https://github.com/asadal12/CityBuilderToolkit.git
   `
2. Open **Unity Hub** and click **Add > Add project from disk**.
3. Select the CityBuilderToolkit folder.
4. Open the project with **Unity 6** or **Unity 2022.3 LTS+**.
5. Load the demo scene: Assets/CityBuilderToolkit/Scenes/TownGeneratorDemo.unity.
6. Press **Play** and fly through the town using **WASD + Mouse Look**!

### Method 2: Import into an Existing Unity Project
1. Simply copy the Assets/CityBuilderToolkit/ folder from this repository into your existing project's Assets/ directory.
2. Unity will automatically compile the scripts.
3. Access all tools from the top menu: **Tools > City & Building Generator**.

---

## 🚀 Step-by-Step Instructions

### 1. Generating a Real Town from OpenStreetMap (OSM)

1. In the Unity top menu, open **Tools > City & Building Generator** (or **Window > City & Building Generator**).
2. Select the **"🗺️ Real Town (OSM)"** tab.
3. **Choose a Location:**
   - Pick an instant preset:
     - 🇩🇪 **Kaiserslautern City Center** *(Lat: 49.4447, Lon: 7.7690)*
     - 🎓 **Kaiserslautern RPTU Campus** *(Lat: 49.4245, Lon: 7.7535)*
     - 🇩🇪 **Berlin Alexanderplatz & Mitte** *(Lat: 52.5219, Lon: 13.4132)*
     - 🇫🇷 **Paris Eiffel Tower & Seine** *(Lat: 48.8584, Lon: 2.2945)*
     - 🇬🇧 **London Westminster & Big Ben** *(Lat: 51.5007, Lon: -0.1246)*
     - 🇺🇸 **New York Times Square** *(Lat: 40.7580, Lon: -73.9855)*
     - 🇯🇵 **Tokyo Shibuya Crossing** *(Lat: 35.6595, Lon: 139.7005)*
     - 🌐 **Custom Coordinates:** Enter your own Latitude, Longitude, and Radius (150m to 2000m+).
4. **Configure Layers:**
   - Toggle: Buildings, Roads & Sidewalks, Parks & Waterways, Streetlights & Benches.
5. **Generate:**
   - Click **"🌍 Download & Generate Real Town (Overpass API)"** to stream live data.
   - Or click **"📂 Load Bundled Offline Sample (Kaiserslautern)"** to generate instantly offline!
   - Or click **"📁 Select Local .osm / .json File..."** to import an OSM file you exported from [openstreetmap.org](https://www.openstreetmap.org).

---

### 2. Generating a Procedural Grid City

1. Open **Tools > City & Building Generator** and switch to the **"🏙️ Grid Town"** tab.
2. Adjust grid dimensions:
   - Blocks (X-Axis) & Blocks (Z-Axis) (e.g. 3x3 or 5x5)
   - Block Size (e.g. 50m)
   - Road Width & Sidewalk Width
3. Toggle street props: Avenue Trees, Street Lights, Park Benches.
4. Click **"🚀 Generate Procedural Grid Town"**.
5. Use **"🎲 Randomize Seed & Regenerate"** to explore infinite town variations.

---

### 3. Spawning & Customizing Single Buildings

1. Right-click anywhere in the Hierarchy and select:
   **3D Object > City Builder > Procedural Building**.
2. In the Inspector, adjust:
   - **Footprint Shape:** Rectangle, L-Shape, U-Shape, T-Shape, or Custom Polygon.
   - **Dimensions:** Width, Depth, Floor count (1 to 40+), Floor height.
   - **Facade Style:** European Plaster, Modern Glass, Contemporary Panels, Brick.
   - **Roof Style:** Flat Parapet (with rooftop HVAC chillers), Gabled, Hipped, Skyscraper Crown.
   - **Details:** Add balconies, ground floor commercial storefronts, and canopies.
3. **Sculpting with 3D Vertex Handles:**
   - Set shape to CustomPolygon.
   - In the Unity Scene View, click and drag the yellow vertex handles to shape your building footprint in real-time.
4. Click **"💾 Save Mesh to Assets as Prefab"** to save your building as an independent asset.

---

## 💻 Scripting API Example

You can generate real towns and buildings programmatically from C# scripts:

### Generate a Real Town from Code:
`csharp
using UnityEngine;
using CityBuilderToolkit.OSM;

public class CitySpawner : MonoBehaviour
{
    void Start()
    {
        var go = new GameObject("MyRealCity");
        var gen = go.AddComponent<RealTownGenerator>();

        // Set coordinates (e.g. Paris Eiffel Tower)
        gen.latitude = 48.8584;
        gen.longitude = 2.2945;
        gen.radiusMeters = 600f;

        // Configure layers
        gen.generateBuildings = true;
        gen.generateRoads = true;
        gen.generateParksAndWater = true;
        gen.spawnStreetFurniture = true;

        // Download and generate from Overpass API
        gen.GenerateFromOverpass(() =>
        {
            Debug.Log("Real city successfully built in Unity!");
        });
    }
}
`

### Generate a Procedural Building from Code:
`csharp
using UnityEngine;
using CityBuilderToolkit.Buildings;
using CityBuilderToolkit.Core;

public class BuildingSpawner : MonoBehaviour
{
    void Start()
    {
        var go = new GameObject("CustomSkyscraper");
        var bg = go.AddComponent<BuildingGenerator>();

        bg.shape = FootprintShape.Rectangle;
        bg.width = 24f;
        bg.depth = 18f;
        bg.floors = 16;
        bg.setbackPerFloor = 0.04f;

        bg.style.facadeStyle = FacadeStyle.ModernGlass;
        bg.style.roofStyle = RoofStyle.SkyscraperCrown;
        bg.style.hasGroundFloorCommercial = true;

        bg.Generate();
    }
}
`

---

## 📁 Repository Structure

`
CityBuilderToolkit/
├── Documentation/
│   └── Images/                       # High-resolution screenshots for documentation
├── Assets/
│   └── CityBuilderToolkit/
│       ├── Runtime/
│       │   ├── OSM/                  # OpenStreetMap GIS Engine
│       │   │   ├── OsmDataModel.cs           # Nodes, ways, relations, and GPS-to-Unity meters projection
│       │   │   ├── OsmParser.cs              # Fast XML (.osm) and JSON OpenStreetMap parser
│       │   │   ├── OsmOverpassClient.cs      # Live Overpass API client with mirror failover
│       │   │   ├── OsmBuildingBuilder.cs     # Real 3D building mesh compiler
│       │   │   ├── OsmRoadBuilder.cs         # Road ribbons, curbs, sidewalks, and markings
│       │   │   ├── OsmEnvironmentBuilder.cs  # Parks, water bodies, and street furniture
│       │   │   └── RealTownGenerator.cs      # Master MonoBehaviour component for real cities
│       │   ├── Core/                 # Procedural Mesh & Math Engine
│       │   │   ├── MeshBuilder.cs            # 6-submesh geometry engine
│       │   │   ├── Polygon2D.cs              # Ear-clipping triangulation & polygon offsets
│       │   │   └── BuildingData.cs           # Architectural enums & configs
│       │   ├── Buildings/            # Building Generators
│       │   │   ├── BuildingGenerator.cs      # Single building component
│       │   │   ├── FacadeGenerator.cs        # Windows, sills, balconies, and canopies
│       │   │   └── BuildingPreset.cs         # ScriptableObject preset container
│       │   ├── Roads/                # Road Networks
│       │   │   ├── RoadNetworkGenerator.cs   # Grid road network
│       │   │   └── StreetFurnitureSpawner.cs # Trees, streetlights, and benches
│       │   ├── Town/                 # Grid Town & Zoning
│       │   │   ├── TownGenerator.cs          # Grid town controller
│       │   │   ├── TownBlock.cs              # Parcel subdivision & courtyards
│       │   │   └── TownZoning.cs             # Zoning rules
│       │   ├── Materials/            # Pipeline-Agnostic Shaders & Palettes
│       │   │   └── CityMaterialPalette.cs    # URP & Built-in shader detection & palettes
│       │   └── Demo/                 # Player Controls
│       │       └── FreeFlyCamera.cs          # WASD + Mouse look inspection camera
│       ├── Samples/
│       │   └── Kaiserslautern_Sample.osm     # Bundled 1MB offline real-world OSM GIS sample
│       ├── Scenes/
│       │   └── TownGeneratorDemo.unity       # Pre-configured demo scene
│       └── Editor/                   # Unity Editor Windows & Custom Inspectors
│           ├── TownGeneratorWindow.cs        # 4-tab Editor Window (Tools > City & Building Generator)
│           ├── RealTownGeneratorEditor.cs    # Custom Inspector for Real Town Generator
│           ├── BuildingGeneratorEditor.cs    # Scene View 3D polygon handles & Inspector
│           ├── TownBuilderMenu.cs            # Hierarchy menu shortcuts
│           ├── RealTownDemoRunner.cs         # Headless test & screenshot runner
│           └── CityBuilderDemoRunner.cs      # Demo scene creator
├── Packages/
│   └── manifest.json                 # Standard Unity packages
├── ProjectSettings/
│   └── ProjectVersion.txt            # Unity Editor version specification
├── .gitignore                        # Complete Unity ignore rules
├── .gitattributes                    # Line ending and format handling
└── README.md                         # This file
`

---

## ❓ FAQ & Troubleshooting

#### Q: Which Unity versions are supported?
A: Any version from **Unity 2022.3 LTS** up to **Unity 6 (6000.x)**.

#### Q: Do I need to install any external packages or Python?
A: **No.** The entire toolkit is written in 100% pure C# using standard Unity APIs. No Python, no external DLLs, no NuGet packages required.

#### Q: How do I export an OSM file for my hometown?
A: 
1. Visit [OpenStreetMap.org](https://www.openstreetmap.org).
2. Pan/zoom to your desired neighborhood.
3. Click the **"Export"** button at the top.
4. Click the blue **"Export"** button on the left sidebar to download the .osm file.
5. In Unity, open Tools > City & Building Generator, choose Select Local .osm File..., and select your downloaded file!

#### Q: Can I use this in commercial games?
A: Yes. The code is provided under the MIT license. When using real-world data from OpenStreetMap, please include standard OSM credit: © OpenStreetMap contributors (ODbL).

---

## 📄 License
This project is licensed under the MIT License - see the LICENSE file for details.  
Map data is © [OpenStreetMap contributors](https://www.openstreetmap.org/copyright), available under the Open Database License (ODbL).
