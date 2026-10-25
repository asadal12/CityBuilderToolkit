using System;
using System.Collections.Generic;
using UnityEngine;
using CityBuilderToolkit.Core;
using CityBuilderToolkit.Roads;
using CityBuilderToolkit.Materials;

namespace CityBuilderToolkit.Town
{
    /// <summary>
    /// Master Town & City Generator component.
    /// Procedurally creates a complete 3D town with road networks, zoning,
    /// customizable buildings, sidewalks, trees, streetlights, and parks with a single click.
    /// </summary>
    [ExecuteInEditMode]
    public class TownGenerator : MonoBehaviour
    {
        [Header("Town Grid Layout")]
        [Range(1, 10)]
        public int blocksX = 3;
        [Range(1, 10)]
        public int blocksZ = 3;

        [Range(30f, 100f)]
        public float blockSizeX = 50f;
        [Range(30f, 100f)]
        public float blockSizeZ = 50f;

        [Header("Roads & Sidewalks")]
        [Range(6f, 20f)]
        public float roadWidth = 10f;
        [Range(1.5f, 5f)]
        public float sidewalkWidth = 2.5f;

        [Header("Urban Details")]
        public bool spawnStreetTrees = true;
        public bool spawnStreetLights = true;
        public bool spawnParkBenches = true;

        [Header("Seed & Randomization")]
        public int randomSeed = 1337;

        [ContextMenu("Generate Town")]
        public void GenerateTown()
        {
            ClearTown();

            System.Random rng = (randomSeed == 0) ? new System.Random() : new System.Random(randomSeed);

            // 1. Calculate City Dimensions
            float totalRoadCorridor = roadWidth + (sidewalkWidth * 2f);
            float stepX = blockSizeX + totalRoadCorridor;
            float stepZ = blockSizeZ + totalRoadCorridor;

            float cityOriginX = -(blocksX * stepX) * 0.5f;
            float cityOriginZ = -(blocksZ * stepZ) * 0.5f;

            // 2. Generate Combined Road Network Mesh
            GenerateRoads(cityOriginX, cityOriginZ, stepX, stepZ, totalRoadCorridor);

            // 3. Generate City Blocks & Buildings
            var blocksRoot = new GameObject("City_Blocks").transform;
            blocksRoot.SetParent(transform, false);

            int totalBuildings = 0;

            for (int bx = 0; bx < blocksX; bx++)
            {
                for (int bz = 0; bz < blocksZ; bz++)
                {
                    float blockMinX = cityOriginX + totalRoadCorridor + bx * stepX;
                    float blockMinZ = cityOriginZ + totalRoadCorridor + bz * stepZ;
                    Rect blockRect = new Rect(blockMinX, blockMinZ, blockSizeX, blockSizeZ);

                    ZoneType zone = DetermineZone(bx, bz, blocksX, blocksZ, rng);
                    TownBlock.GenerateBlock(blocksRoot, blockRect, zone, rng, spawnStreetTrees, spawnParkBenches);
                }
            }

            // 4. Generate Street Furniture (Streetlights and Avenue Trees)
            if (spawnStreetLights || spawnStreetTrees)
            {
                var furnitureRoot = new GameObject("Street_Furniture").transform;
                furnitureRoot.SetParent(transform, false);

                GenerateStreetFurniture(furnitureRoot, cityOriginX, cityOriginZ, stepX, stepZ, totalRoadCorridor);
            }

            // Count buildings generated
            var allBldg = GetComponentsInChildren<Buildings.BuildingGenerator>();
            totalBuildings = allBldg.Length;

            Debug.Log($"[TownGenerator] Successfully generated town! Blocks: {blocksX}x{blocksZ}, Buildings: {totalBuildings}");
        }

        [ContextMenu("Clear Town")]
        public void ClearTown()
        {
            var children = new List<GameObject>();
            foreach (Transform child in transform)
            {
                children.Add(child.gameObject);
            }

            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                {
                    Destroy(children[i]);
                }
                else
                {
                    DestroyImmediate(children[i]);
                }
            }
        }

        private void GenerateRoads(float originX, float originZ, float stepX, float stepZ, float roadCorridor)
        {
            var roadsGo = new GameObject("Road_Network");
            roadsGo.transform.SetParent(transform, false);

            var mb = new MeshBuilder(4);

            float totalWidth = blocksX * stepX + roadCorridor;
            float totalLength = blocksZ * stepZ + roadCorridor;

            // Horizontal Streets (East-West along X)
            for (int bz = 0; bz <= blocksZ; bz++)
            {
                float zCenter = originZ + roadCorridor * 0.5f + bz * stepZ;
                Vector3 start = new Vector3(originX, 0f, zCenter);
                Vector3 end = new Vector3(originX + totalWidth, 0f, zCenter);

                RoadNetworkGenerator.GenerateRoadSegment(mb, start, end, roadWidth, sidewalkWidth);
            }

            // Vertical Avenues (North-South along Z)
            for (int bx = 0; bx <= blocksX; bx++)
            {
                float xCenter = originX + roadCorridor * 0.5f + bx * stepX;
                Vector3 start = new Vector3(xCenter, 0f, originZ);
                Vector3 end = new Vector3(xCenter, 0f, originZ + totalLength);

                RoadNetworkGenerator.GenerateRoadSegment(mb, start, end, roadWidth, sidewalkWidth);
            }

            var mf = roadsGo.AddComponent<MeshFilter>();
            mf.sharedMesh = mb.ToMesh("TownRoadMesh");

            var mr = roadsGo.AddComponent<MeshRenderer>();
            mr.sharedMaterials = RoadNetworkGenerator.GetRoadMaterials();

            var mc = roadsGo.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
        }

        private void GenerateStreetFurniture(Transform parent, float originX, float originZ, float stepX, float stepZ, float roadCorridor)
        {
            float spacing = 22f;

            // Along East-West Sidewalks
            for (int bz = 0; bz <= blocksZ; bz++)
            {
                float zCenter = originZ + roadCorridor * 0.5f + bz * stepZ;
                float totalWidth = blocksX * stepX + roadCorridor;

                for (float x = originX + 10f; x < originX + totalWidth - 10f; x += spacing)
                {
                    // North Sidewalk
                    Vector3 posNorth = new Vector3(x, 0.15f, zCenter + (roadWidth * 0.5f + sidewalkWidth * 0.5f));
                    // South Sidewalk
                    Vector3 posSouth = new Vector3(x + spacing * 0.5f, 0.15f, zCenter - (roadWidth * 0.5f + sidewalkWidth * 0.5f));

                    if (spawnStreetLights)
                    {
                        StreetFurnitureSpawner.SpawnStreetLight(parent, posNorth, Quaternion.Euler(0f, 180f, 0f));
                    }

                    if (spawnStreetTrees)
                    {
                        StreetFurnitureSpawner.SpawnTree(parent, posSouth, 1.0f);
                    }
                }
            }
        }

        private ZoneType DetermineZone(int bx, int bz, int countX, int countZ, System.Random rng)
        {
            int midX = countX / 2;
            int midZ = countZ / 2;

            int dist = Mathf.Max(Mathf.Abs(bx - midX), Mathf.Abs(bz - midZ));

            if (dist == 0)
            {
                // Central square: either park or downtown skyscraper
                return (rng.NextDouble() > 0.4) ? ZoneType.DowntownSkyscrapers : ZoneType.PublicPark;
            }
            else if (dist == 1)
            {
                return (rng.NextDouble() > 0.5) ? ZoneType.DowntownSkyscrapers : ZoneType.CommercialMedium;
            }
            else
            {
                return (rng.NextDouble() > 0.3) ? ZoneType.ResidentialApartments : ZoneType.CommercialMedium;
            }
        }
    }
}
