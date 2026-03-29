using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class AISpawner : MonoBehaviour
{
    public static AISpawner Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject pedestrianPrefab;
    [SerializeField] private GameObject customerPrefab;

    [Header("Settings")]
    [SerializeField] private int pedestrianCount = 20;
    [SerializeField] private int customerCount = 20;
    [SerializeField] private int poolSize = 25;

    [Header("Materials")]
    [SerializeField] private Material pedestrianMaterial;
    [SerializeField] private Material customerMaterial;

    public void SetMaterials(Material pedMat, Material custMat)
    {
        pedestrianMaterial = pedMat;
        customerMaterial = custMat;
    }

    [Header("Spawn Settings")]
    [SerializeField] private float cityRadius = 50f;
    [SerializeField] private float spawnSearchRadius = 10f;

    [SerializeField] private List<GameObject> _pedestrianPool = new List<GameObject>();
    [SerializeField] private List<GameObject> _customerPool = new List<GameObject>();
    [SerializeField] private float _spawnTimer;
    private const float SpawnInterval = 0.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; }
    }

    private void Start()
    {
        CreatePools();
    }

    private void Update()
    {
        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= SpawnInterval)
        {
            _spawnTimer = 0f;
            MaintainPopulation();
        }
    }

    private void CreatePools()
    {
        for (int i = 0; i < poolSize; i++)
        {
            _pedestrianPool.Add(CreateAI(pedestrianPrefab, pedestrianMaterial, "Pedestrian"));
            _customerPool.Add(CreateAI(customerPrefab, customerMaterial, "Customer"));
        }
    }

    private GameObject CreateAI(GameObject prefab, Material mat, string name)
    {
        GameObject obj;
        if (prefab != null)
        {
            obj = Instantiate(prefab);
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            obj.name = name;
        }

        if (mat != null)
        {
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null) renderer.material = mat;
        }

        if (obj.GetComponent<NavMeshAgent>() == null)
        {
            obj.AddComponent<NavMeshAgent>();
        }

        obj.SetActive(false);
        return obj;
    }

    private void MaintainPopulation()
    {
        int activePedestrians = CountActive(_pedestrianPool);
        if (activePedestrians < pedestrianCount)
        {
            SpawnFromPool(_pedestrianPool, true);
        }

        int activeCustomers = CountActive(_customerPool);
        if (activeCustomers < customerCount)
        {
            SpawnFromPool(_customerPool, false);
        }
    }

    private int CountActive(List<GameObject> pool)
    {
        int count = 0;
        foreach (var obj in pool)
        {
            if (obj.activeInHierarchy) count++;
        }
        return count;
    }

    private void SpawnFromPool(List<GameObject> pool, bool isPedestrian)
    {
        foreach (var obj in pool)
        {
            if (!obj.activeInHierarchy)
            {
                Vector3 spawnPosition = isPedestrian 
                    ? GetRandomCityPosition() 
                    : GetEdgePosition();

                obj.transform.position = spawnPosition;
                obj.SetActive(true);

                var pedestrian = obj.GetComponent<PedestrianAI>();
                if (pedestrian != null)
                {
                    pedestrian.Initialize();
                    return;
                }

                var customer = obj.GetComponent<CustomerAI>();
                if (customer != null)
                {
                    customer.Initialize();
                    return;
                }

                return;
            }
        }
    }

    private Vector3 GetRandomCityPosition()
    {
        Vector2 randomCircle = Random.insideUnitCircle * cityRadius;
        Vector3 candidatePosition = new Vector3(randomCircle.x, 0f, randomCircle.y);

        if (NavMesh.SamplePosition(candidatePosition, out NavMeshHit hit, spawnSearchRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return candidatePosition;
    }

    private Vector3 GetEdgePosition()
    {
        int edge = Random.Range(0, 4);
        float z = 0f;

        switch (edge)
        {
            case 0: z = cityRadius; break;
            case 1: z = -cityRadius; break;
            case 2: case 3: break;
        }

        float x = (edge < 2) ? Random.Range(-cityRadius, cityRadius) : cityRadius * (edge == 2 ? 1 : -1);
        
        if (edge >= 2)
        {
            x = Random.Range(-cityRadius, cityRadius);
            z = cityRadius * (edge == 2 ? 1 : -1);
        }

        Vector3 candidatePosition = new Vector3(x, 0f, z);

        if (NavMesh.SamplePosition(candidatePosition, out NavMeshHit hit, spawnSearchRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return candidatePosition;
    }
}
