using System.Collections.Generic;
using UnityEngine;

public class EnvironmentManager : MonoBehaviour
{
    [Header("Prefab references")]
    public GameObject treePrefab;
    public GameObject firePrefab;

    [Header("Scene references")]
    public Terrain terrain;
    public Transform spawnCenter;

    [Header("Spawn settings")]
    public int treeCount = 0;
    public float spawnRadius = 20f;
    public float minTreeDistanceFromAgent = 3f;
    public float minFireDistanceFromAgent = 6f;
    public float minFireDistanceFromTrees = 1f;

    [Header("Tags")]
    public string treeTag = "Tree1";
    public string fireTag = "Fire";

    private readonly List<GameObject> spawnedTrees = new List<GameObject>();
    private GameObject fireInstance;

    public Transform FireTransform => fireInstance != null ? fireInstance.transform : null;

    // Induláskor beállítja a referenciákat és létrehozza a tűz objektumot.
    private void Awake()
    {
        if (terrain == null)
        {
            terrain = Terrain.activeTerrain;
        }

        if (terrain == null)
        {
            Debug.LogError("EnvironmentManager: No terrain assigned.");
            return;
        }

        if (spawnCenter == null)
        {
            Debug.LogError("EnvironmentManager: No spawnCenter assigned.");
            return;
        }

        if (treePrefab == null)
        {
            Debug.LogError("EnvironmentManager: Tree prefab is not assigned.");
            return;
        }

        if (firePrefab == null)
        {
            Debug.LogError("EnvironmentManager: Fire prefab is not assigned.");
            return;
        }

        fireInstance = Instantiate(firePrefab, Vector3.zero, Quaternion.identity, transform);
        fireInstance.tag = fireTag;
    }

    // Új epizódnál újrapozicionálja az agentet, a fákat és a tüzet.
    public void ResetEnvironment(Transform agentTransform)
    {
        if (terrain == null || spawnCenter == null || treePrefab == null || fireInstance == null)
            return;

        ClearTrees();

        Vector3 agentPos = GetRandomPointNearSpawnCenter(spawnRadius);
        agentTransform.position = agentPos;
        agentTransform.rotation = Quaternion.identity;

        SpawnTrees(agentPos);
        PlaceFire(agentPos);
    }

    // Véletlenszerűen lerakja a fákat az adott környezet középpontja körül.
    private void SpawnTrees(Vector3 agentPos)
    {
        int spawned = 0;
        int attempts = 0;
        int maxAttempts = treeCount * 30;

        while (spawned < treeCount && attempts < maxAttempts)
        {
            attempts++;
            Vector3 pos = GetRandomPointNearSpawnCenter(spawnRadius);

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

    // Véletlen helyre teszi a tüzet az adott környezeten belül.
    private void PlaceFire(Vector3 agentPos)
    {
        int attempts = 0;
        Vector3 pos = GetRandomPointNearSpawnCenter(spawnRadius);

        // while (attempts < 200)
        // {
        //     pos = GetRandomPointNearSpawnCenter(spawnRadius);

        //     bool tooCloseToAgent = Vector3.Distance(pos, agentPos) < minFireDistanceFromAgent;
        //     bool tooCloseToTree = IsTooCloseToAnyTree(pos);

        //     if (!tooCloseToAgent && !tooCloseToTree)
        //         break;

        //     attempts++;
        // }

        fireInstance.transform.position = pos;
    }

    // Megnézi, hogy a pont túl közel van-e valamelyik fához.
    private bool IsTooCloseToAnyTree(Vector3 pos)
    {
        foreach (GameObject tree in spawnedTrees)
        {
            if (tree != null && Vector3.Distance(pos, tree.transform.position) < minFireDistanceFromTrees)
                return true;
        }

        return false;
    }

    // Kitörli az előző kör fáit.
    private void ClearTrees()
    {
        foreach (GameObject tree in spawnedTrees)
        {
            if (tree != null)
                Destroy(tree);
        }

        spawnedTrees.Clear();
    }

    // Visszaad egy pontot a saját környezet közepéhez képest.
    private Vector3 GetRandomPointNearSpawnCenter(float radius)
    {
        // Vector2 random2D = Random.insideUnitCircle * radius;
        Vector2 random2D = Random.insideUnitCircle * radius;

        float worldX = spawnCenter.position.x + random2D.x;
        float worldZ = spawnCenter.position.z + random2D.y;
        float worldY = terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + terrain.transform.position.y;

        return new Vector3(worldX, worldY, worldZ);
    }
}