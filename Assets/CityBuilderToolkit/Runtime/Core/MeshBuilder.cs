using System.Collections.Generic;
using UnityEngine;

namespace CityBuilderToolkit.Core
{
    /// <summary>
    /// Lightweight procedural mesh compiler supporting up to 6 submeshes,
    /// arbitrary vertex attributes, and automatic normals/bounds calculation.
    /// </summary>
    public class MeshBuilder
    {
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<Color> _colors = new List<Color>();
        private readonly List<List<int>> _submeshTriangles = new List<List<int>>();

        public int VertexCount => _vertices.Count;

        public MeshBuilder(int expectedSubmeshes = 6)
        {
            EnsureSubmeshes(expectedSubmeshes);
        }

        public void Clear()
        {
            _vertices.Clear();
            _normals.Clear();
            _uvs.Clear();
            _colors.Clear();
            for (int i = 0; i < _submeshTriangles.Count; i++)
            {
                _submeshTriangles[i].Clear();
            }
        }

        public void EnsureSubmeshes(int count)
        {
            while (_submeshTriangles.Count < count)
            {
                _submeshTriangles.Add(new List<int>());
            }
        }

        public int AddVertex(Vector3 position, Vector3 normal, Vector2 uv, Color color = default)
        {
            int index = _vertices.Count;
            _vertices.Add(position);
            _normals.Add(normal);
            _uvs.Add(uv);
            _colors.Add(color == default ? Color.white : color);
            return index;
        }

        public void AddTriangle(int submesh, int i0, int i1, int i2)
        {
            EnsureSubmeshes(submesh + 1);
            _submeshTriangles[submesh].Add(i0);
            _submeshTriangles[submesh].Add(i1);
            _submeshTriangles[submesh].Add(i2);
        }

        public void AddTriangle(int submesh, Vector3 v0, Vector3 v1, Vector3 v2, Vector2 uv0, Vector2 uv1, Vector2 uv2, Color color = default, Vector3 normal = default)
        {
            if (normal == default)
            {
                normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
            }
            int idx0 = AddVertex(v0, normal, uv0, color);
            int idx1 = AddVertex(v1, normal, uv1, color);
            int idx2 = AddVertex(v2, normal, uv2, color);
            AddTriangle(submesh, idx0, idx1, idx2);
        }

        public void AddQuad(int submesh,
            Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3,
            Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3,
            Vector3 normal)
        {
            AddQuad(submesh, v0, v1, v2, v3, uv0, uv1, uv2, uv3, Color.white, normal);
        }

        public void AddQuad(int submesh,
            Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3,
            Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3,
            Color color = default, Vector3 normal = default)
        {
            if (normal == default)
            {
                normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
            }

            int idx0 = AddVertex(v0, normal, uv0, color);
            int idx1 = AddVertex(v1, normal, uv1, color);
            int idx2 = AddVertex(v2, normal, uv2, color);
            int idx3 = AddVertex(v3, normal, uv3, color);

            EnsureSubmeshes(submesh + 1);
            // Triangle 1
            _submeshTriangles[submesh].Add(idx0);
            _submeshTriangles[submesh].Add(idx1);
            _submeshTriangles[submesh].Add(idx2);
            // Triangle 2
            _submeshTriangles[submesh].Add(idx0);
            _submeshTriangles[submesh].Add(idx2);
            _submeshTriangles[submesh].Add(idx3);
        }

        public void AddExtrudedWall(int submesh, Vector3 start, Vector3 end, float bottomY, float topY,
            float uvScaleX = 1f, float uvScaleY = 1f)
        {
            AddWallQuad(submesh, start, end, bottomY, topY, uvScaleX, uvScaleY);
        }

        public void AddWallQuad(int submesh, Vector3 start, Vector3 end, float bottomY, float topY,
            float uvScaleX = 1f, float uvScaleY = 1f)
        {
            Vector3 p0 = new Vector3(start.x, bottomY, start.z);
            Vector3 p1 = new Vector3(end.x, bottomY, end.z);
            Vector3 p2 = new Vector3(end.x, topY, end.z);
            Vector3 p3 = new Vector3(start.x, topY, start.z);

            float width = Vector3.Distance(new Vector3(start.x, 0, start.z), new Vector3(end.x, 0, end.z));
            float height = topY - bottomY;

            float uMax = width * uvScaleX;
            float vMax = height * uvScaleY;

            AddQuad(submesh, p0, p1, p2, p3,
                new Vector2(0, 0),
                new Vector2(uMax, 0),
                new Vector2(uMax, vMax),
                new Vector2(0, vMax));
        }

        public void AddBox(int submesh, Vector3 center, Vector3 size, Quaternion rotation = default, Color color = default)
        {
            Vector3 h = size * 0.5f;
            Vector3[] localCorners = new Vector3[]
            {
                new Vector3(-h.x, -h.y, -h.z), // 0
                new Vector3( h.x, -h.y, -h.z), // 1
                new Vector3( h.x, -h.y,  h.z), // 2
                new Vector3(-h.x, -h.y,  h.z), // 3
                new Vector3(-h.x,  h.y, -h.z), // 4
                new Vector3( h.x,  h.y, -h.z), // 5
                new Vector3( h.x,  h.y,  h.z), // 6
                new Vector3(-h.x,  h.y,  h.z)  // 7
            };

            Vector3[] worldCorners = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                worldCorners[i] = center + (rotation == default ? localCorners[i] : rotation * localCorners[i]);
            }

            // Front (Z-)
            AddQuad(submesh, worldCorners[0], worldCorners[1], worldCorners[5], worldCorners[4],
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1), color);
            // Back (Z+)
            AddQuad(submesh, worldCorners[2], worldCorners[3], worldCorners[7], worldCorners[6],
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1), color);
            // Left (X-)
            AddQuad(submesh, worldCorners[3], worldCorners[0], worldCorners[4], worldCorners[7],
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1), color);
            // Right (X+)
            AddQuad(submesh, worldCorners[1], worldCorners[2], worldCorners[6], worldCorners[5],
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1), color);
            // Top (Y+)
            AddQuad(submesh, worldCorners[4], worldCorners[5], worldCorners[6], worldCorners[7],
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1), color);
            // Bottom (Y-)
            AddQuad(submesh, worldCorners[3], worldCorners[2], worldCorners[1], worldCorners[0],
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1), color);
        }

        public void AddHorizontalPolygonFace(int submesh, List<Vector2> points2D, float y, bool facingUp = true, float uvScale = 0.25f, Color color = default)
        {
            if (points2D == null || points2D.Count < 3) return;

            var indices = Polygon2D.Triangulate(points2D);
            if (indices == null || indices.Count == 0) return;

            Vector3 normal = facingUp ? Vector3.up : Vector3.down;
            int baseIdx = _vertices.Count;

            for (int i = 0; i < points2D.Count; i++)
            {
                Vector2 p = points2D[i];
                Vector3 pos = new Vector3(p.x, y, p.y);
                Vector2 uv = p * uvScale;
                AddVertex(pos, normal, uv, color);
            }

            EnsureSubmeshes(submesh + 1);
            for (int i = 0; i < indices.Count; i += 3)
            {
                if (facingUp)
                {
                    AddTriangle(submesh, baseIdx + indices[i], baseIdx + indices[i + 1], baseIdx + indices[i + 2]);
                }
                else
                {
                    AddTriangle(submesh, baseIdx + indices[i], baseIdx + indices[i + 2], baseIdx + indices[i + 1]);
                }
            }
        }

        public void AddHorizontalPolygon(int submesh, List<Vector2> points2D, float y, bool facingUp = true, Color color = default)
        {
            AddHorizontalPolygonFace(submesh, points2D, y, facingUp, 0.25f, color);
        }

        public Mesh BuildMesh(string meshName = "ProceduralMesh", bool autoNormals = false)
        {
            return ToMesh(meshName, autoNormals);
        }

        public Mesh ToMesh(string meshName = "ProceduralMesh", bool autoNormals = false)
        {
            var mesh = new Mesh { name = meshName };

            if (_vertices.Count > 65000)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uvs);
            mesh.SetColors(_colors);

            int submeshCount = 0;
            for (int i = 0; i < _submeshTriangles.Count; i++)
            {
                if (_submeshTriangles[i].Count > 0)
                {
                    submeshCount = i + 1;
                }
            }

            mesh.subMeshCount = Mathf.Max(1, submeshCount);
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                if (i < _submeshTriangles.Count)
                {
                    mesh.SetTriangles(_submeshTriangles[i], i, false);
                }
                else
                {
                    mesh.SetTriangles(new int[0], i, false);
                }
            }

            if (autoNormals)
            {
                mesh.RecalculateNormals();
            }

            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
