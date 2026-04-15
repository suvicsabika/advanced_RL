using System.Collections.Generic;
using UnityEngine;

public class EnvironmentManager : MonoBehaviour
{
    [Header("Prefab references")]
    public GameObject treePrefab;
    public GameObject firePrefab;

    [Header("Spawn settings")]
    public int treeCount = 100;
    public float spawnRadius = 80f;
    public float minTreeDistanceFromAgent = 6f;
    public float minFireDistanceFromAgent = 12f;
    public float minFireDistanceFromTrees = 3f;

    [Header("Tags (must already exist in Unity Tags and Layers)")]
    public string treeTag = "Tree1";
    public string fireTag = "Fire";

    private Terrain terrain;
    private readonly List<GameObject> spawnedTrees = new List<GameObject>();
    private GameObject fireInstance;

    public Transform FireTransform => fireInstance != null ? fireInstance.transform : null;

    // Induláskor betölti a fontos referenciákat és létrehozza a tűz objektumot.
    private void Awake()
    {
        terrain = Terrain.activeTerrain;

        if (terrain == null)
        {
            Debug.LogError("EnvironmentManager: No active Terrain found in scene.");
            return;
        }

        if (treePrefab == null)
        {
            Debug.LogError("EnvironmentManager: Tree prefab is not assigned in the Inspector.");
            return;
        }

        if (firePrefab == null)
        {
            Debug.LogError("EnvironmentManager: Fire prefab is not assigned in the Inspector.");
            return;
        }

        fireInstance = Instantiate(firePrefab, Vector3.zero, Quaternion.identity, transform);
        fireInstance.tag = fireTag;
    }

    // Egy új epizódhoz újragenerálja az agentet, a fákat és a tüzet.
    public void ResetEnvironment(Transform agentTransform)
    {
        if (terrain == null || treePrefab == null || fireInstance == null)
            return;

        ClearTrees();

        Vector3 agentPos = GetRandomPointNearCenter(spawnRadius);
        agentTransform.position = agentPos;
        agentTransform.rotation = Quaternion.identity;

        SpawnTrees(agentPos);
        PlaceFire(agentPos);
    }

    // Véletlenszerűen lerakja a fákat a megadott területen belül.
    private void SpawnTrees(Vector3 agentPos)
    {
        int spawned = 0;
        int attempts = 0;
        int maxAttempts = treeCount * 30;

        while (spawned < treeCount && attempts < maxAttempts)
        {
            attempts++;
            Vector3 pos = GetRandomPointNearCenter(spawnRadius);

            if (Vector3.Distance(pos, agentPos) < minTreeDistanceFromAgent)
                continue;

            GameObject tree = Instantiate(
                treePrefab,
                pos,
                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                transform
            );

            tree.tag = treeTag;
            spawnedTrees.Add(tree);
            spawned++;
        }
    }

    // Véletlenszerűen elhelyezi a tüzet úgy, hogy ne legyen túl közel máshoz.
    private void PlaceFire(Vector3 agentPos)
    {
        int attempts = 0;
        Vector3 pos = GetRandomPointNearCenter(spawnRadius);

        while (attempts < 200)
        {
            pos = GetRandomPointNearCenter(spawnRadius);

            bool tooCloseToAgent = Vector3.Distance(pos, agentPos) < minFireDistanceFromAgent;
            bool tooCloseToTree = IsTooCloseToAnyTree(pos);

            if (!tooCloseToAgent && !tooCloseToTree)
                break;

            attempts++;
        }

        fireInstance.transform.position = pos;
    }

    // Ellenőrzi, hogy a megadott pont túl közel van-e valamelyik fához.
    private bool IsTooCloseToAnyTree(Vector3 pos)
    {
        foreach (GameObject tree in spawnedTrees)
        {
            if (tree != null && Vector3.Distance(pos, tree.transform.position) < minFireDistanceFromTrees)
                return true;
        }

        return false;
    }

    // Törli az előző epizódban létrehozott fákat a jelenetből.
    private void ClearTrees()
    {
        foreach (GameObject tree in spawnedTrees)
        {
            if (tree != null)
                Destroy(tree);
        }

        spawnedTrees.Clear();
    }

    // Visszaad egy véletlen pontot a terrain közepe körüli körön belül.
    private Vector3 GetRandomPointNearCenter(float radius = 80f)
    {
        TerrainData data = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;

        Vector3 center = new Vector3(
            terrainPos.x + data.size.x * 0.5f,
            0f,
            terrainPos.z + data.size.z * 0.5f
        );

        Vector2 random2D = Random.insideUnitCircle * radius;

        float worldX = center.x + random2D.x;
        float worldZ = center.z + random2D.y;
        float worldY = terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + terrainPos.y;

        return new Vector3(worldX, worldY, worldZ);
    }
}