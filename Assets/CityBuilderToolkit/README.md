# 🏙️ City & Building Generator Toolkit for Unity (with OpenStreetMap)

A modular, zero-dependency, procedural 3D town and building generator for Unity. Designed for game developers, urban planners, and creators who want to create **real-world towns from OpenStreetMap (OSM)** or **procedural grid cities and standalone buildings** in Unity with a single click.

---

## ✨ Key Features

### 🌍 Real-World Town Generator (OpenStreetMap)
- **Live Overpass API Integration:** Download real-world GIS data directly inside the Unity Editor or in Play mode for any city or town globally.
- **Instant City Presets:**
  - 🇩🇪 **Kaiserslautern City Center** (Town Hall, Mall, Historic Center)
  - 🎓 **Kaiserslautern RPTU Campus** (University grounds, Kurt-Schumacher-Straße)
  - 🇩🇪 **Berlin Alexanderplatz & Mitte**
  - 🇫🇷 **Paris Eiffel Tower & Champ de Mars**
  - 🇬🇧 **London Westminster & Big Ben**
  - 🇺🇸 **New York Times Square**
  - 🇯🇵 **Tokyo Shibuya Crossing**
  - 🌐 **Custom Coordinates:** Enter any Latitude, Longitude, and Radius (150m to 2000m+).
- **Offline / Local File Importer:** Open standard `.osm` XML or `.json` files exported from [OpenStreetMap.org](https://www.openstreetmap.org) with zero network dependency.
- **Real Building Footprints:** Accurate polygon extrusions using ear-clipping triangulation, multi-storey facade subdivisions, window bays, ground-floor commercial storefronts, and architectural roofs (flat with parapets, gabled, hipped).
- **Real Road Networks:** Extruded 3D road ribbons along OSM highway vectors (highways, primary, secondary, residential, pedestrian, and cycle paths) with raised concrete curbs (+0.15m) and paved sidewalks.
- **Real Environmental Features:** Triangulated parks (`leisure=park`, `landuse=grass`), water bodies (`natural=water`, waterways), avenue trees, and steel streetlights with real-time `Spotlight` components.

---

### 🏢 Procedural Building & Grid Town Generator
- **Procedural Grid Cities:** Customizable blocks (`blocksX` × `blocksZ`), block dimensions, road widths, and zoning distribution (skyscrapers, mid-rise commercial, residential apartments, suburban houses, and public park plazas).
- **Single Building Spawner:**
  - Multiple footprint shapes: **Rectangle**, **L-Shape**, **U-Shape**, **T-Shape**, and **Custom Polygon**.
  - Interactive **Scene View Vertex Handles** to sculpt custom footprints directly in 3D space.
  - Multi-style facades (European Plaster, Contemporary Panels, Modern Glass, Brick).
  - Save any generated building directly to Assets as a reusable Unity Prefab.

---

### 🎨 Render Pipeline Agnostic & Zero Dependencies
- Works out of the box with **Universal Render Pipeline (URP)** and **Built-in Render Pipeline**. Automatically assigns proper shaders.
- Pure C# using Unity's standard Mesh and GameObject API. **Zero external DLLs or plugins required.**

---

## 🚀 Quickstart Guide

### Option 1: Generating a Real Town from OpenStreetMap
1. In the Unity top menu, open **`Tools > City & Building Generator`** (or **`Window > City & Building Generator`**).
2. Go to the **"🗺️ Real Town (OSM)"** tab.
3. Select a location preset (e.g. *Kaiserslautern City Center*, *Berlin*, or *Paris*), or enter custom GPS coordinates.
4. Click **"🌍 Download & Generate Real Town (Overpass API)"**.
5. Alternatively, click **"📂 Load Bundled Offline Sample (Kaiserslautern)"** to generate a real town immediately without an internet connection!

### Option 2: Hierarchy Right-Click Menu
- Right-click anywhere in the Hierarchy:
  - **`3D Object > City Builder > Real Town (OpenStreetMap)`**
  - **`3D Object > City Builder > Procedural Grid Town`**
  - **`3D Object > City Builder > Procedural Building`**

---

## 📁 File Structure

```
CityBuilderToolkit/
├── Runtime/
│   ├── OSM/
│   │   ├── OsmDataModel.cs           # Nodes, ways, relations, and GPS-to-Unity meters projection
│   │   ├── OsmParser.cs              # Fast XML and JSON OpenStreetMap parser
│   │   ├── OsmOverpassClient.cs      # Live Overpass API client with mirror failover
│   │   ├── OsmBuildingBuilder.cs     # Real 3D building mesh compiler
│   │   ├── OsmRoadBuilder.cs         # Road ribbons, curbs, sidewalks, and markings
│   │   ├── OsmEnvironmentBuilder.cs  # Parks, water bodies, and street furniture
│   │   └── RealTownGenerator.cs      # Master MonoBehaviour component for real cities
│   ├── Core/
│   │   ├── MeshBuilder.cs            # 6-submesh geometry engine
│   │   ├── Polygon2D.cs              # Ear-clipping triangulation & polygon offsets
│   │   └── BuildingData.cs           # Architectural enums & configs
│   ├── Buildings/
│   │   ├── BuildingGenerator.cs      # Single building component
│   │   ├── FacadeGenerator.cs        # Windows, sills, balconies, and canopies
│   │   └── BuildingPreset.cs         # ScriptableObject preset container
│   ├── Roads/
│   │   ├── RoadNetworkGenerator.cs   # Grid road network
│   │   └── StreetFurnitureSpawner.cs # Trees, streetlights, and benches
│   ├── Town/
│   │   ├── TownGenerator.cs          # Grid town controller
│   │   ├── TownBlock.cs              # Parcel subdivision & courtyards
│   │   └── TownZoning.cs             # Zoning rules
│   ├── Materials/
│   │   └── CityMaterialPalette.cs    # URP & Built-in shader detection & palettes
│   └── Demo/
│       └── FreeFlyCamera.cs          # WASD + Mouse look inspection camera
├── Samples/
│   └── Kaiserslautern_Sample.osm     # Bundled 1MB offline real-world OSM GIS sample
├── Editor/
│   ├── TownGeneratorWindow.cs        # Multi-tab EditorWindow (Tools > City & Building Generator)
│   ├── RealTownGeneratorEditor.cs    # Custom Inspector for Real Town Generator
│   ├── BuildingGeneratorEditor.cs    # Scene View 3D polygon handles & Inspector
│   └── TownBuilderMenu.cs            # Hierarchy menu shortcuts
└── README.md
```

---

## 📄 License & Sharing
This toolkit is free to use, modify, and include in any commercial or personal Unity project.
All OpenStreetMap data is © OpenStreetMap contributors and available under the Open Database License (ODbL).
