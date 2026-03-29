using UnityEngine;
using System.Collections.Generic;

public class AISpawner : MonoBehaviour
{
    public static AISpawner Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject pedestrianPrefab;
    public GameObject customerPrefab;

    [Header("Settings")]
    public int pedestrianCount = 20;
    public int customerCount = 20;
    public int poolSize = 25;

    [Header("Materials")]
    public Material pedestrianMaterial;
    public Material customerMaterial;

    private List<GameObject> _pedestrianPool = new List<GameObject>();
    private List<GameObject> _customerPool = new List<GameObject>();
    private float _spawnTimer;
    private const float SpawnInterval = 0.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
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
            SpawnFromPool(_pedestrianPool);
        }

        int activeCustomers = CountActive(_customerPool);
        if (activeCustomers < customerCount)
        {
            SpawnFromPool(_customerPool);
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

    private void SpawnFromPool(List<GameObject> pool)
    {
        foreach (var obj in pool)
        {
            if (!obj.activeInHierarchy)
            {
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
}
