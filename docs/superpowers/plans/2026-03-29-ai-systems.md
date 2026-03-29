# AI Systems Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add 20 wandering pedestrians and 20 customers walking past the food truck using object pooling.

**Architecture:** AISpawner singleton manages two pools (pedestrians/customers). Each AI uses NavMeshAgent for movement with simple state machine behavior.

**Tech Stack:** Unity C#, NavMesh, Object Pooling

---

## File Structure

```
Assets/Scripts/
├── AISpawner.cs           # Main spawner singleton (NEW)
├── PedestrianAI.cs       # Pedestrian behavior (NEW)
├── CustomerAI.cs          # Customer behavior (NEW)
└── Editor/
    └── SceneSetupEditor.cs  # Add AISpawner to scene (MODIFY)
```

---

## Tasks

### Task 1: Create PedestrianAI Script

**Files:**
- Create: `Assets/Scripts/PedestrianAI.cs`

- [ ] **Step 1: Create PedestrianAI.cs**

```csharp
using UnityEngine;
using UnityEngine.AI;

public class PedestrianAI : MonoBehaviour
{
    private NavMeshAgent _agent;
    private float _waitTime;
    private float _waitTimer;
    private Vector3 _spawnCenter = Vector3.zero;
    private float _cityRadius = 50f;
    private float _despawnRadius = 80f;

    public void Initialize()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = 2f;
        _agent.enabled = true;
        PickNewDestination();
    }

    void Update()
    {
        if (!_agent.enabled || !_agent.isOnNavMesh) return;

        _waitTimer += Time.deltaTime;
        if (_waitTimer >= _waitTime || _agent.remainingDistance < 0.5f)
        {
            PickNewDestination();
        }

        if (Vector3.Distance(transform.position, _spawnCenter) > _despawnRadius)
        {
            gameObject.SetActive(false);
        }
    }

    void PickNewDestination()
    {
        _waitTimer = 0f;
        _waitTime = Random.Range(1f, 2f);

        Vector2 randomPoint = Random.insideUnitCircle * _cityRadius;
        Vector3 target = new Vector3(randomPoint.x, 0, randomPoint.y);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(target, out hit, 10f, NavMesh.AllAreas))
        {
            _agent.SetDestination(hit.position);
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/PedestrianAI.cs
git commit -m "feat: add PedestrianAI with random wandering behavior"
```

---

### Task 2: Create CustomerAI Script

**Files:**
- Create: `Assets/Scripts/CustomerAI.cs`

- [ ] **Step 1: Create CustomerAI.cs**

```csharp
using UnityEngine;
using UnityEngine.AI;

public class CustomerAI : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Vector3 _truckArea = Vector3.zero;
    private float _passRadius = 15f;
    private float _cityEdge = 60f;

    public void Initialize()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = 2.5f;
        _agent.enabled = true;
        SetPathThroughCity();
    }

    void Update()
    {
        if (!_agent.enabled || !_agent.isOnNavMesh) return;

        if (_agent.remainingDistance < 1f || IsPastEdge())
        {
            gameObject.SetActive(false);
        }
    }

    bool IsPastEdge()
    {
        return Mathf.Abs(transform.position.x) > _cityEdge || 
               Mathf.Abs(transform.position.z) > _cityEdge;
    }

    void SetPathThroughCity()
    {
        int edge = Random.Range(0, 4);
        Vector3 startPos, endPos;

        switch (edge)
        {
            case 0: // North to South
                startPos = new Vector3(Random.Range(-_passRadius, _passRadius), 0, _cityEdge);
                endPos = new Vector3(Random.Range(-_passRadius, _passRadius), 0, -_cityEdge);
                break;
            case 1: // South to North
                startPos = new Vector3(Random.Range(-_passRadius, _passRadius), 0, -_cityEdge);
                endPos = new Vector3(Random.Range(-_passRadius, _passRadius), 0, _cityEdge);
                break;
            case 2: // East to West
                startPos = new Vector3(_cityEdge, 0, Random.Range(-_passRadius, _passRadius));
                endPos = new Vector3(-_cityEdge, 0, Random.Range(-_passRadius, _passRadius));
                break;
            default: // West to East
                startPos = new Vector3(-_cityEdge, 0, Random.Range(-_passRadius, _passRadius));
                endPos = new Vector3(_cityEdge, 0, Random.Range(-_passRadius, _passRadius));
                break;
        }

        NavMeshHit hit;
        if (NavMesh.SamplePosition(startPos, out hit, 10f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
        }

        if (NavMesh.SamplePosition(endPos, out hit, 10f, NavMesh.AllAreas))
        {
            _agent.SetDestination(hit.position);
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/CustomerAI.cs
git commit -m "feat: add CustomerAI that walks past truck area"
```

---

### Task 3: Create AISpawner Script

**Files:**
- Create: `Assets/Scripts/AISpawner.cs`

- [ ] **Step 1: Create AISpawner.cs**

```csharp
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
    private float _spawnInterval = 0.5f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        CreatePools();
    }

    void Update()
    {
        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= _spawnInterval)
        {
            _spawnTimer = 0f;
            MaintainPopulation();
        }
    }

    void CreatePools()
    {
        for (int i = 0; i < poolSize; i++)
        {
            _pedestrianPool.Add(CreateAI(pedestrianPrefab, pedestrianMaterial, "Pedestrian"));
            _customerPool.Add(CreateAI(customerPrefab, customerMaterial, "Customer"));
        }
    }

    GameObject CreateAI(GameObject prefab, Material mat, string name)
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

    void MaintainPopulation()
    {
        int activePedestrians = 0;
        foreach (var p in _pedestrianPool)
        {
            if (p.activeInHierarchy) activePedestrians++;
        }

        if (activePedestrians < pedestrianCount)
        {
            SpawnFromPool(_pedestrianPool);
        }

        int activeCustomers = 0;
        foreach (var c in _customerPool)
        {
            if (c.activeInHierarchy) activeCustomers++;
        }

        if (activeCustomers < customerCount)
        {
            SpawnFromPool(_customerPool);
        }
    }

    void SpawnFromPool(List<GameObject> pool)
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
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/AISpawner.cs
git commit -m "feat: add AISpawner with object pooling"
```

---

### Task 4: Update SceneSetupEditor

**Files:**
- Modify: `Assets/Scripts/Editor/SceneSetupEditor.cs`

- [ ] **Step 1: Read current SceneSetupEditor.cs**

Read the file to understand its structure.

- [ ] **Step 2: Add AISpawner setup**

Add a method to create materials and add AISpawner to the scene:

```csharp
[MenuItem("FoodTruck/Setup AI Systems")]
public static void SetupAISystems()
{
    var spawnerObj = new GameObject("AISpawner");
    var spawner = spawnerObj.AddComponent<AISpawner>();

    // Create blue material for pedestrians
    var pedMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
    pedMat.color = new Color(0.204f, 0.596f, 0.859f); // #3498db
    AssetDatabase.CreateAsset(pedMat, "Assets/Materials/Pedestrian.mat");
    spawner.pedestrianMaterial = pedMat;

    // Create green material for customers
    var custMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
    custMat.color = new Color(0.18f, 0.8f, 0.443f); // #2ecc71
    AssetDatabase.CreateAsset(custMat, "Assets/Materials/Customer.mat");
    spawner.customerMaterial = custMat;

    AssetDatabase.SaveAssets();
    Debug.Log("[SceneSetup] AI Systems setup complete");
}
```

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Editor/SceneSetupEditor.cs
git commit -m "feat: add SetupAISystems to SceneSetupEditor"
```

---

### Task 5: Test and Verify

**Files:**
- Run in Unity Editor

- [ ] **Step 1: Open scene and run**

Open CityScene.unity in Unity Editor and press Play.

- [ ] **Step 2: Verify pedestrians spawn**

You should see 20 blue capsules wandering randomly within city bounds.

- [ ] **Step 3: Verify customers spawn**

You should see 20 green capsules walking across the map, passing near the center.

- [ ] **Step 4: Check for errors**

Open Console - there should be no errors.

- [ ] **Step 5: Commit**

```bash
git commit -m "test: verify AI systems work correctly"
```

---

## Summary

| Task | Description |
|------|-------------|
| 1 | PedestrianAI - random wandering |
| 2 | CustomerAI - walks past truck |
| 3 | AISpawner - object pooling + spawn management |
| 4 | SceneSetupEditor - setup AI in scene |
| 5 | Test and verify |
