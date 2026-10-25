using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilderToolkit.Core
{
    /// <summary>
    /// Utility class providing 2D polygon manipulation, triangulation (ear clipping),
    /// insetting/buffering, and procedural building footprint generation.
    /// </summary>
    public static class Polygon2D
    {
        /// <summary>
        /// Computes the signed area of a 2D polygon.
        /// Positive if vertices are counter-clockwise, negative if clockwise.
        /// </summary>
        public static float GetSignedArea(List<Vector2> poly)
        {
            if (poly == null || poly.Count < 3) return 0f;
            float area = 0f;
            for (int i = 0; i < poly.Count; i++)
            {
                int next = (i + 1) % poly.Count;
                area += (poly[i].x * poly[next].y) - (poly[next].x * poly[i].y);
            }
            return area * 0.5f;
        }

        public static bool IsCounterClockwise(List<Vector2> poly)
        {
            return GetSignedArea(poly) > 0f;
        }

        public static List<Vector2> EnsureWinding(List<Vector2> poly, bool counterClockwise = true)
        {
            var result = new List<Vector2>(poly);
            bool isCCW = IsCounterClockwise(result);
            if (isCCW != counterClockwise)
            {
                result.Reverse();
            }
            return result;
        }

        public static Vector2 GetCentroid(List<Vector2> poly)
        {
            if (poly == null || poly.Count == 0) return Vector2.zero;
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < poly.Count; i++)
            {
                sum += poly[i];
            }
            return sum / poly.Count;
        }

        public static Rect GetBounds(List<Vector2> poly)
        {
            if (poly == null || poly.Count == 0) return Rect.zero;
            float minX = poly[0].x, maxX = poly[0].x;
            float minY = poly[0].y, maxY = poly[0].y;
            for (int i = 1; i < poly.Count; i++)
            {
                minX = Mathf.Min(minX, poly[i].x);
                maxX = Mathf.Max(maxX, poly[i].x);
                minY = Mathf.Min(minY, poly[i].y);
                maxY = Mathf.Max(maxY, poly[i].y);
            }
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// Offsets/insets a 2D polygon by a distance.
        /// Positive delta shrinks the polygon inward, negative delta expands it outward.
        /// </summary>
        public static List<Vector2> OffsetPolygon(List<Vector2> poly, float delta)
        {
            if (poly == null || poly.Count < 3) return new List<Vector2>(poly);
            var ccw = EnsureWinding(poly, true);
            var result = new List<Vector2>(ccw.Count);

            int n = ccw.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 prev = ccw[(i - 1 + n) % n];
                Vector2 curr = ccw[i];
                Vector2 next = ccw[(i + 1) % n];

                Vector2 d1 = (curr - prev).normalized;
                Vector2 d2 = (next - curr).normalized;

                // Inward normal for CCW winding
                Vector2 n1 = new Vector2(-d1.y, d1.x);
                Vector2 n2 = new Vector2(-d2.y, d2.x);

                Vector2 bisector = (n1 + n2).normalized;
                float dot = Vector2.Dot(bisector, n1);
                float len = Mathf.Abs(dot) > 0.05f ? delta / dot : delta;

                // Clamp extreme miter spikes
                len = Mathf.Clamp(len, -Mathf.Abs(delta) * 3f, Mathf.Abs(delta) * 3f);
                result.Add(curr + bisector * len);
            }

            return result;
        }

        /// <summary>
        /// Robust Ear Clipping triangulation algorithm for simple non-self-intersecting 2D polygons.
        /// Returns a list of vertex index triplets.
        /// </summary>
        public static List<int> Triangulate(List<Vector2> poly)
        {
            var indices = new List<int>();
            if (poly == null || poly.Count < 3) return indices;

            int n = poly.Count;
            var ccw = EnsureWinding(poly, true);

            var vList = new List<int>(n);
            for (int i = 0; i < n; i++) vList.Add(i);

            int maxIterations = n * 4;
            int count = n;
            int iter = 0;

            int current = 0;
            while (count > 2 && iter++ < maxIterations)
            {
                int prev = vList[(current - 1 + count) % count];
                int curr = vList[current % count];
                int next = vList[(current + 1) % count];

                Vector2 a = ccw[prev];
                Vector2 b = ccw[curr];
                Vector2 c = ccw[next];

                if (IsConvex(a, b, c) && !ContainsAnyPoint(a, b, c, ccw, vList, prev, curr, next))
                {
                    indices.Add(prev);
                    indices.Add(curr);
                    indices.Add(next);

                    vList.RemoveAt(current % count);
                    count--;
                    current = current % Mathf.Max(1, count);
                }
                else
                {
                    current = (current + 1) % count;
                }
            }

            return indices;
        }

        private static bool IsConvex(Vector2 a, Vector2 b, Vector2 c)
        {
            return ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) >= -0.0001f;
        }

        private static bool ContainsAnyPoint(Vector2 a, Vector2 b, Vector2 c, List<Vector2> points,
            List<int> activeIndices, int idxA, int idxB, int idxC)
        {
            for (int i = 0; i < activeIndices.Count; i++)
            {
                int idx = activeIndices[i];
                if (idx == idxA || idx == idxB || idx == idxC) continue;
                if (PointInTriangle(points[idx], a, b, c)) return true;
            }
            return false;
        }

        public static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b);
            float d2 = Sign(p, b, c);
            float d3 = Sign(p, c, a);

            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(hasNeg && hasPos);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        // =========================================================================
        // PROCEDURAL FOOTPRINT FACTORIES
        // =========================================================================

        public static List<Vector2> CreateRectangle(float width, float depth)
        {
            float hw = width * 0.5f;
            float hd = depth * 0.5f;
            return new List<Vector2>
            {
                new Vector2(-hw, -hd),
                new Vector2( hw, -hd),
                new Vector2( hw,  hd),
                new Vector2(-hw,  hd)
            };
        }

        public static List<Vector2> CreateLShape(float width, float depth, float cutXRatio = 0.5f, float cutZRatio = 0.5f)
        {
            float hw = width * 0.5f;
            float hd = depth * 0.5f;
            float cutX = -hw + width * cutXRatio;
            float cutZ = -hd + depth * cutZRatio;

            return new List<Vector2>
            {
                new Vector2(-hw, -hd),
                new Vector2( hw, -hd),
                new Vector2( hw, cutZ),
                new Vector2(cutX, cutZ),
                new Vector2(cutX,  hd),
                new Vector2(-hw,  hd)
            };
        }

        public static List<Vector2> CreateUShape(float width, float depth, float wingWidthRatio = 0.35f, float courtyardDepthRatio = 0.5f)
        {
            float hw = width * 0.5f;
            float hd = depth * 0.5f;
            float wW = width * wingWidthRatio;
            float cD = depth * courtyardDepthRatio;

            return new List<Vector2>
            {
                new Vector2(-hw, -hd),
                new Vector2( hw, -hd),
                new Vector2( hw,  hd),
                new Vector2( hw - wW,  hd),
                new Vector2( hw - wW,  hd - cD),
                new Vector2(-hw + wW,  hd - cD),
                new Vector2(-hw + wW,  hd),
                new Vector2(-hw,  hd)
            };
        }

        public static List<Vector2> CreateTShape(float width, float depth, float stemRatio = 0.4f, float barDepthRatio = 0.4f)
        {
            float hw = width * 0.5f;
            float hd = depth * 0.5f;
            float barD = depth * barDepthRatio;
            float stemHW = (width * stemRatio) * 0.5f;

            return new List<Vector2>
            {
                new Vector2(-stemHW, -hd),
                new Vector2( stemHW, -hd),
                new Vector2( stemHW,  hd - barD),
                new Vector2( hw,      hd - barD),
                new Vector2( hw,      hd),
                new Vector2(-hw,      hd),
                new Vector2(-hw,      hd - barD),
                new Vector2(-stemHW,  hd - barD)
            };
        }
    }
}
