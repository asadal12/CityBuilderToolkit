using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using UnityEngine;

namespace CityBuilderToolkit.OSM
{
    /// <summary>
    /// Parses OpenStreetMap XML (.osm) and Overpass API data into an OsmDataSet.
    /// Supports both XML streaming and JSON formats with zero external dependencies.
    /// </summary>
    public static class OsmParser
    {
        public static OsmDataSet Parse(string content, double? centerLat = null, double? centerLon = null)
        {
            if (string.IsNullOrEmpty(content))
                throw new ArgumentException("OSM content cannot be null or empty.");

            content = content.TrimStart();
            if (content.StartsWith("<"))
            {
                return ParseXml(content, centerLat, centerLon);
            }
            else if (content.StartsWith("{"))
            {
                return ParseJson(content, centerLat, centerLon);
            }
            else
            {
                throw new FormatException("Unrecognized OSM format. Expected XML (<osm>) or JSON ({ elements }).");
            }
        }

        public static OsmDataSet ParseFile(string filePath, double? centerLat = null, double? centerLon = null)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"OSM file not found: {filePath}");

            string text = File.ReadAllText(filePath);
            return Parse(text, centerLat, centerLon);
        }

        #region XML Parsing
        public static OsmDataSet ParseXml(string xmlContent, double? overrideCenterLat = null, double? overrideCenterLon = null)
        {
            var dataSet = new OsmDataSet();

            double sumLat = 0;
            double sumLon = 0;
            long nodeCount = 0;

            OsmNode currentNode = null;
            OsmWay currentWay = null;
            OsmRelation currentRelation = null;

            using (var reader = XmlReader.Create(new StringReader(xmlContent), new XmlReaderSettings { IgnoreWhitespace = true }))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        string name = reader.Name;
                        if (name == "bounds")
                        {
                            // <bounds minlat="49.4" minlon="7.7" maxlat="49.5" maxlon="7.8"/>
                            string minLatStr = reader.GetAttribute("minlat");
                            string maxLatStr = reader.GetAttribute("maxlat");
                            string minLonStr = reader.GetAttribute("minlon");
                            string maxLonStr = reader.GetAttribute("maxlon");

                            if (!overrideCenterLat.HasValue && !string.IsNullOrEmpty(minLatStr) && !string.IsNullOrEmpty(maxLatStr))
                            {
                                if (double.TryParse(minLatStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double minLat) &&
                                    double.TryParse(maxLatStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double maxLat) &&
                                    double.TryParse(minLonStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double minLon) &&
                                    double.TryParse(maxLonStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double maxLon))
                                {
                                    dataSet.CenterLat = (minLat + maxLat) * 0.5;
                                    dataSet.CenterLon = (minLon + maxLon) * 0.5;
                                }
                            }
                        }
                        else if (name == "node")
                        {
                            string idStr = reader.GetAttribute("id");
                            string latStr = reader.GetAttribute("lat");
                            string lonStr = reader.GetAttribute("lon");

                            if (long.TryParse(idStr, out long id) &&
                                double.TryParse(latStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) &&
                                double.TryParse(lonStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double lon))
                            {
                                currentNode = new OsmNode(id, lat, lon);
                                dataSet.Nodes[id] = currentNode;
                                sumLat += lat;
                                sumLon += lon;
                                nodeCount++;

                                if (reader.IsEmptyElement)
                                {
                                    currentNode = null;
                                }
                            }
                        }
                        else if (name == "way")
                        {
                            string idStr = reader.GetAttribute("id");
                            if (long.TryParse(idStr, out long id))
                            {
                                currentWay = new OsmWay { Id = id };
                                dataSet.Ways.Add(currentWay);

                                if (reader.IsEmptyElement)
                                {
                                    currentWay = null;
                                }
                            }
                        }
                        else if (name == "relation")
                        {
                            string idStr = reader.GetAttribute("id");
                            if (long.TryParse(idStr, out long id))
                            {
                                currentRelation = new OsmRelation { Id = id };
                                dataSet.Relations.Add(currentRelation);

                                if (reader.IsEmptyElement)
                                {
                                    currentRelation = null;
                                }
                            }
                        }
                        else if (name == "nd" && currentWay != null)
                        {
                            string refStr = reader.GetAttribute("ref");
                            if (long.TryParse(refStr, out long ndRef))
                            {
                                currentWay.NodeRefs.Add(ndRef);
                            }
                        }
                        else if (name == "member" && currentRelation != null)
                        {
                            string type = reader.GetAttribute("type");
                            string refStr = reader.GetAttribute("ref");
                            string role = reader.GetAttribute("role");
                            if (long.TryParse(refStr, out long mRef))
                            {
                                currentRelation.Members.Add(new OsmMember { Type = type, Ref = mRef, Role = role });
                            }
                        }
                        else if (name == "tag")
                        {
                            string k = reader.GetAttribute("k");
                            string v = reader.GetAttribute("v");
                            if (!string.IsNullOrEmpty(k) && v != null)
                            {
                                if (currentWay != null)
                                    currentWay.Tags[k] = v;
                                else if (currentNode != null)
                                    currentNode.Tags[k] = v;
                                else if (currentRelation != null)
                                    currentRelation.Tags[k] = v;
                            }
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement)
                    {
                        if (reader.Name == "node") currentNode = null;
                        else if (reader.Name == "way") currentWay = null;
                        else if (reader.Name == "relation") currentRelation = null;
                    }
                }
            }

            // Determine Center
            if (overrideCenterLat.HasValue && overrideCenterLon.HasValue)
            {
                dataSet.CenterLat = overrideCenterLat.Value;
                dataSet.CenterLon = overrideCenterLon.Value;
            }
            else if (dataSet.CenterLat == 0 && dataSet.CenterLon == 0 && nodeCount > 0)
            {
                dataSet.CenterLat = sumLat / nodeCount;
                dataSet.CenterLon = sumLon / nodeCount;
            }

            dataSet.BuildGeometry();
            return dataSet;
        }
        #endregion

        #region JSON Parsing (Zero external dependency lightweight reader)
        public static OsmDataSet ParseJson(string jsonContent, double? overrideCenterLat = null, double? overrideCenterLon = null)
        {
            var dataSet = new OsmDataSet();

            double sumLat = 0;
            double sumLon = 0;
            long nodeCount = 0;

            // Fast token-based parsing for Overpass elements
            int idx = jsonContent.IndexOf("\"elements\"", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return dataSet;

            int arrStart = jsonContent.IndexOf('[', idx);
            if (arrStart < 0) return dataSet;

            int i = arrStart + 1;
            int length = jsonContent.Length;

            while (i < length)
            {
                // find next object '{'
                while (i < length && jsonContent[i] != '{' && jsonContent[i] != ']') i++;
                if (i >= length || jsonContent[i] == ']') break;

                int objStart = i;
                int braceDepth = 1;
                i++;
                while (i < length && braceDepth > 0)
                {
                    if (jsonContent[i] == '{') braceDepth++;
                    else if (jsonContent[i] == '}') braceDepth--;
                    i++;
                }
                int objEnd = i;
                string objStr = jsonContent.Substring(objStart, objEnd - objStart);

                // Determine element type: "node", "way", "relation"
                string type = ExtractJsonString(objStr, "type");
                long id = ExtractJsonLong(objStr, "id");

                if (type == "node")
                {
                    double lat = ExtractJsonDouble(objStr, "lat");
                    double lon = ExtractJsonDouble(objStr, "lon");
                    var node = new OsmNode(id, lat, lon);
                    ExtractJsonTags(objStr, node.Tags);
                    dataSet.Nodes[id] = node;
                    sumLat += lat;
                    sumLon += lon;
                    nodeCount++;
                }
                else if (type == "way")
                {
                    var way = new OsmWay { Id = id };
                    ExtractJsonNodesArray(objStr, way.NodeRefs);
                    ExtractJsonTags(objStr, way.Tags);
                    dataSet.Ways.Add(way);
                }
                else if (type == "relation")
                {
                    var rel = new OsmRelation { Id = id };
                    ExtractJsonTags(objStr, rel.Tags);
                    dataSet.Relations.Add(rel);
                }
            }

            if (overrideCenterLat.HasValue && overrideCenterLon.HasValue)
            {
                dataSet.CenterLat = overrideCenterLat.Value;
                dataSet.CenterLon = overrideCenterLon.Value;
            }
            else if (nodeCount > 0)
            {
                dataSet.CenterLat = sumLat / nodeCount;
                dataSet.CenterLon = sumLon / nodeCount;
            }

            dataSet.BuildGeometry();
            return dataSet;
        }

        private static string ExtractJsonString(string json, string key)
        {
            string needle = $"\"{key}\"";
            int kIdx = json.IndexOf(needle, StringComparison.Ordinal);
            if (kIdx < 0) return "";
            int colon = json.IndexOf(':', kIdx + needle.Length);
            if (colon < 0) return "";
            int quote1 = json.IndexOf('"', colon + 1);
            if (quote1 < 0) return "";
            int quote2 = json.IndexOf('"', quote1 + 1);
            if (quote2 < 0) return "";
            return json.Substring(quote1 + 1, quote2 - quote1 - 1);
        }

        private static long ExtractJsonLong(string json, string key)
        {
            string needle = $"\"{key}\"";
            int kIdx = json.IndexOf(needle, StringComparison.Ordinal);
            if (kIdx < 0) return 0;
            int colon = json.IndexOf(':', kIdx + needle.Length);
            if (colon < 0) return 0;
            int p = colon + 1;
            while (p < json.Length && (char.IsWhiteSpace(json[p]) || json[p] == '"')) p++;
            int start = p;
            while (p < json.Length && (char.IsDigit(json[p]) || json[p] == '-')) p++;
            string num = json.Substring(start, p - start);
            long.TryParse(num, out long val);
            return val;
        }

        private static double ExtractJsonDouble(string json, string key)
        {
            string needle = $"\"{key}\"";
            int kIdx = json.IndexOf(needle, StringComparison.Ordinal);
            if (kIdx < 0) return 0.0;
            int colon = json.IndexOf(':', kIdx + needle.Length);
            if (colon < 0) return 0.0;
            int p = colon + 1;
            while (p < json.Length && (char.IsWhiteSpace(json[p]) || json[p] == '"')) p++;
            int start = p;
            while (p < json.Length && (char.IsDigit(json[p]) || json[p] == '-' || json[p] == '.' || json[p] == 'e' || json[p] == 'E' || json[p] == '+')) p++;
            string num = json.Substring(start, p - start);
            double.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out double val);
            return val;
        }

        private static void ExtractJsonNodesArray(string json, List<long> nodeRefs)
        {
            string needle = "\"nodes\"";
            int kIdx = json.IndexOf(needle, StringComparison.Ordinal);
            if (kIdx < 0) return;
            int arrStart = json.IndexOf('[', kIdx + needle.Length);
            if (arrStart < 0) return;
            int arrEnd = json.IndexOf(']', arrStart);
            if (arrEnd < 0) return;

            string inner = json.Substring(arrStart + 1, arrEnd - arrStart - 1);
            var parts = inner.Split(',');
            foreach (var part in parts)
            {
                if (long.TryParse(part.Trim(), out long id))
                    nodeRefs.Add(id);
            }
        }

        private static void ExtractJsonTags(string json, Dictionary<string, string> tags)
        {
            string needle = "\"tags\"";
            int kIdx = json.IndexOf(needle, StringComparison.Ordinal);
            if (kIdx < 0) return;
            int objStart = json.IndexOf('{', kIdx + needle.Length);
            if (objStart < 0) return;
            int objEnd = json.IndexOf('}', objStart);
            if (objEnd < 0) return;

            string inner = json.Substring(objStart + 1, objEnd - objStart - 1);
            int p = 0;
            while (p < inner.Length)
            {
                int q1 = inner.IndexOf('"', p);
                if (q1 < 0) break;
                int q2 = inner.IndexOf('"', q1 + 1);
                if (q2 < 0) break;
                string k = inner.Substring(q1 + 1, q2 - q1 - 1);

                int colon = inner.IndexOf(':', q2 + 1);
                if (colon < 0) break;

                int q3 = inner.IndexOf('"', colon + 1);
                if (q3 < 0) break;
                int q4 = inner.IndexOf('"', q3 + 1);
                if (q4 < 0) break;
                string v = inner.Substring(q3 + 1, q4 - q3 - 1);

                tags[k] = v;
                p = q4 + 1;
            }
        }
        #endregion
    }
}
