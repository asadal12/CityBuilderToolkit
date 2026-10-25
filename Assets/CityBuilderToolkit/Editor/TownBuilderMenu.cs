using UnityEditor;
using UnityEngine;
using CityBuilderToolkit.Town;
using CityBuilderToolkit.Buildings;
using CityBuilderToolkit.OSM;

namespace CityBuilderToolkit.Editor
{
    public static class TownBuilderMenu
    {
        [MenuItem("GameObject/3D Object/City Builder/Real Town (OpenStreetMap)", false, 9)]
        public static void CreateRealTownGenerator(MenuCommand menuCommand)
        {
            var go = new GameObject("Real_Town_OSM");
            var gen = go.AddComponent<RealTownGenerator>();
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create Real Town (OSM)");
            Selection.activeGameObject = go;
        }

        [MenuItem("GameObject/3D Object/City Builder/Procedural Grid Town", false, 10)]
        public static void CreateTownGenerator(MenuCommand menuCommand)
        {
            var go = new GameObject("Procedural_Town");
            var tg = go.AddComponent<TownGenerator>();
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create Grid Town Generator");
            Selection.activeGameObject = go;
            tg.GenerateTown();
        }

        [MenuItem("GameObject/3D Object/City Builder/Procedural Building", false, 11)]
        public static void CreateBuilding(MenuCommand menuCommand)
        {
            var go = new GameObject("Procedural_Building");
            var bg = go.AddComponent<BuildingGenerator>();
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create Procedural Building");
            Selection.activeGameObject = go;
            bg.Generate();
        }
    }
}
