using UnityEditor;
using UnityEngine;
using CityBuilderToolkit.Buildings;
using CityBuilderToolkit.Core;

namespace CityBuilderToolkit.Editor
{
    [CustomEditor(typeof(BuildingGenerator))]
    [CanEditMultipleObjects]
    public class BuildingGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            BuildingGenerator bg = (BuildingGenerator)target;

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox("Procedural Building Generator. Tweak settings below and click 'Generate Building' to update the mesh.", MessageType.Info);
            EditorGUILayout.Space(4);

            DrawDefaultInspector();

            EditorGUILayout.Space(12);

            GUI.backgroundColor = new Color(0.2f, 0.75f, 0.35f);
            if (GUILayout.Button("🔄 Regenerate Building", GUILayout.Height(34)))
            {
                foreach (var t in targets)
                {
                    if (t is BuildingGenerator b)
                    {
                        Undo.RecordObject(b, "Regenerate Building");
                        b.Generate();
                        EditorUtility.SetDirty(b);
                    }
                }
            }

            GUI.backgroundColor = new Color(0.25f, 0.65f, 0.95f);
            if (GUILayout.Button("🎲 Randomize Seed & Regenerate", GUILayout.Height(28)))
            {
                foreach (var t in targets)
                {
                    if (t is BuildingGenerator b)
                    {
                        Undo.RecordObject(b, "Randomize Building Seed");
                        b.randomSeed = Random.Range(1, 99999);
                        b.Generate();
                        EditorUtility.SetDirty(b);
                    }
                }
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(4);

            if (GUILayout.Button("💾 Save Mesh to Assets as Prefab"))
            {
                SaveAsPrefab(bg);
            }
        }

        private void OnSceneGUI()
        {
            BuildingGenerator bg = (BuildingGenerator)target;
            if (bg.shape != FootprintShape.CustomPolygon || bg.customPolygon == null) return;

            Transform tr = bg.transform;
            EditorGUI.BeginChangeCheck();

            for (int i = 0; i < bg.customPolygon.Count; i++)
            {
                Vector3 worldPt = tr.TransformPoint(new Vector3(bg.customPolygon[i].x, 0f, bg.customPolygon[i].y));
                Vector3 newWorldPt = Handles.PositionHandle(worldPt, Quaternion.identity);

                if (Vector3.Distance(worldPt, newWorldPt) > 0.001f)
                {
                    Vector3 localPt = tr.InverseTransformPoint(newWorldPt);
                    bg.customPolygon[i] = new Vector2(localPt.x, localPt.z);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(bg, "Modify Building Footprint");
                bg.Generate();
                EditorUtility.SetDirty(bg);
            }
        }

        private void SaveAsPrefab(BuildingGenerator bg)
        {
            string folder = "Assets/CityBuilderToolkit/ExportedPrefabs";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/CityBuilderToolkit"))
                {
                    AssetDatabase.CreateFolder("Assets", "CityBuilderToolkit");
                }
                AssetDatabase.CreateFolder("Assets/CityBuilderToolkit", "ExportedPrefabs");
            }

            var mf = bg.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                EditorUtility.DisplayDialog("Error", "No mesh found on this building.", "OK");
                return;
            }

            string meshPath = $"{folder}/{bg.gameObject.name}_Mesh.asset";
            AssetDatabase.CreateAsset(Instantiate(mf.sharedMesh), meshPath);

            string prefabPath = $"{folder}/{bg.gameObject.name}.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(bg.gameObject, prefabPath, InteractionMode.UserAction);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Success", $"Saved building prefab and mesh to:\n{prefabPath}", "OK");
        }
    }
}
