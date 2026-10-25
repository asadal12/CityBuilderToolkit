using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilderToolkit.OSM
{
    /// <summary>
    /// Represents an OpenStreetMap geographic node with GPS coordinates and projected Unity meters.
    /// </summary>
    public class OsmNode
    {
        public long Id;
        public double Lat;
        public double Lon;
        public Vector2 WorldPos; // Projected X, Z in Unity meters relative to center
        public Dictionary<string, string> Tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public OsmNode(long id, double lat, double lon)
        {
            Id = id;
            Lat = lat;
            Lon = lon;
        }

        public string GetTag(string key, string defaultValue = "")
        {
            return Tags != null && Tags.TryGetValue(key, out var val) ? val : defaultValue;
        }
    }

    /// <summary>
    /// Represents an OpenStreetMap way (linear road/path or closed building/polygon).
    /// </summary>
    public class OsmWay
    {
        public long Id;
        public List<long> NodeRefs = new List<long>();
        public List<Vector2> Points = new List<Vector2>();
        public Dictionary<string, string> Tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public bool IsClosed => NodeRefs.Count >= 4 && NodeRefs[0] == NodeRefs[NodeRefs.Count - 1];

        public bool HasTag(string key) => Tags != null && Tags.ContainsKey(key);

        public string GetTag(string key, string defaultValue = "")
        {
            return Tags != null && Tags.TryGetValue(key, out var val) ? val : defaultValue;
        }

        public bool IsBuilding => HasTag("building") && GetTag("building") != "no";
        public bool IsHighway => HasTag("highway");
        public bool IsWater => HasTag("water") || HasTag("natural") && GetTag("natural") == "water" || HasTag("waterway");
        public bool IsPark => (HasTag("leisure") && (GetTag("leisure") == "park" || GetTag("leisure") == "garden" || GetTag("leisure") == "pitch"))
                              || (HasTag("landuse") && (GetTag("landuse") == "grass" || GetTag("landuse") == "recreation_ground" || GetTag("landuse") == "village_green"));

        public float GetHeight(float defaultHeight = 9f)
        {
            if (Tags.TryGetValue("height", out var hStr))
            {
                hStr = hStr.Replace("m", "").Trim();
                if (float.TryParse(hStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float h))
                    return Mathf.Clamp(h, 3f, 250f);
            }

            if (Tags.TryGetValue("building:levels", out var lvlStr))
            {
                if (float.TryParse(lvlStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float lvls))
                    return Mathf.Clamp(lvls * 3.2f, 3.5f, 250f);
            }

            string bType = GetTag("building").ToLowerInvariant();
            switch (bType)
            {
                case "apartments": return 14f;
                case "commercial":
                case "office": return 18f;
                case "church":
                case "cathedral": return 22f;
                case "house":
                case "residential":
                case "detached": return 8.5f;
                case "garage":
                case "garages":
                case "shed": return 3.2f;
                case "school":
                case "university": return 12f;
                case "industrial":
                case "warehouse": return 10f;
                default: return defaultHeight;
            }
        }

        public int GetFloors(int defaultFloors = 3)
        {
            if (Tags.TryGetValue("building:levels", out var lvlStr))
            {
                if (int.TryParse(lvlStr, out int lvls))
                    return Mathf.Clamp(lvls, 1, 60);
            }
            float h = GetHeight();
            return Mathf.Max(1, Mathf.RoundToInt(h / 3.2f));
        }
    }

    /// <summary>
    /// Represents an OSM Relation member.
    /// </summary>
    public class OsmMember
    {
        public string Type;
        public long Ref;
        public string Role;
    }

    /// <summary>
    /// Represents an OSM Relation (e.g. multipolygon building or boundary).
    /// </summary>
    public class OsmRelation
    {
        public long Id;
        public List<OsmMember> Members = new List<OsmMember>();
        public Dictionary<string, string> Tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public bool HasTag(string key) => Tags != null && Tags.ContainsKey(key);
        public string GetTag(string key, string defaultValue = "") => Tags != null && Tags.TryGetValue(key, out var v) ? v : defaultValue;
    }

    /// <summary>
    /// Container holding all parsed OSM features and coordinate conversion projection.
    /// </summary>
    public class OsmDataSet
    {
        public double CenterLat;
        public double CenterLon;
        public Dictionary<long, OsmNode> Nodes = new Dictionary<long, OsmNode>();
        public List<OsmWay> Ways = new List<OsmWay>();
        public List<OsmRelation> Relations = new List<OsmRelation>();

        public Bounds WorldBounds;

        private const double EarthRadius = 6378137.0; // WGS84 Major radius in meters

        /// <summary>
        /// Projects GPS Lat/Lon to Unity World Space (X, Z in meters) using equirectangular projection
        /// relative to the dataset's geographic center.
        /// </summary>
        public Vector2 GeoToWorld(double lat, double lon)
        {
            double latRad = CenterLat * (Math.PI / 180.0);
            double dLat = (lat - CenterLat) * (Math.PI / 180.0);
            double dLon = (lon - CenterLon) * (Math.PI / 180.0);

            double x = EarthRadius * dLon * Math.Cos(latRad);
            double z = EarthRadius * dLat;

            return new Vector2((float)x, (float)z);
        }

        /// <summary>
        /// Inverse projection: Unity world meters (X, Z) back to GPS Lat/Lon.
        /// </summary>
        public Vector2 WorldToGeo(float x, float z)
        {
            double latRad = CenterLat * (Math.PI / 180.0);
            double dLat = z / EarthRadius;
            double dLon = x / (EarthRadius * Math.Cos(latRad));

            double lat = CenterLat + dLat * (180.0 / Math.PI);
            double lon = CenterLon + dLon * (180.0 / Math.PI);

            return new Vector2((float)lat, (float)lon);
        }

        /// <summary>
        /// Rebuilds way geometry point lists and calculates dataset bounds.
        /// </summary>
        public void BuildGeometry()
        {
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            bool hasPoints = false;

            foreach (var node in Nodes.Values)
            {
                node.WorldPos = GeoToWorld(node.Lat, node.Lon);
                min = Vector2.Min(min, node.WorldPos);
                max = Vector2.Max(max, node.WorldPos);
                hasPoints = true;
            }

            foreach (var way in Ways)
            {
                way.Points.Clear();
                for (int i = 0; i < way.NodeRefs.Count; i++)
                {
                    if (Nodes.TryGetValue(way.NodeRefs[i], out var n))
                    {
                        way.Points.Add(n.WorldPos);
                    }
                }
            }

            if (hasPoints)
            {
                Vector3 center = new Vector3((min.x + max.x) * 0.5f, 0f, (min.y + max.y) * 0.5f);
                Vector3 size = new Vector3(Mathf.Max(10f, max.x - min.x), 50f, Mathf.Max(10f, max.y - min.y));
                WorldBounds = new Bounds(center, size);
            }
        }
    }
}
