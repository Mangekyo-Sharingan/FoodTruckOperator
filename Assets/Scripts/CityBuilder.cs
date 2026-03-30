using UnityEngine;

/// <summary>
/// Procedurally builds a simple city at start.
/// Buildings are primitive cubes; roads are ground-level quads.
/// </summary>
public class CityBuilder : MonoBehaviour
{
    [Header("City Layout")]
    public int blocksX = 12;
    public int blocksZ = 12;
    public float blockSize = 20f;
    public float roadWidth = 6f;

    [Header("Buildings")]
    public int buildingsPerBlock = 4;
    public float minHeight = 4f;
    public float maxHeight = 28f;
    public float buildingPadding = 2f;

    [Header("Materials")]
    public Material roadMaterial;
    public Material sidewalkMaterial;
    public Material[] buildingMaterials;

    static readonly Color[] _buildingColors = new Color[]
    {
        new Color(0.75f, 0.72f, 0.68f),
        new Color(0.62f, 0.68f, 0.72f),
        new Color(0.55f, 0.52f, 0.50f),
        new Color(0.80f, 0.78f, 0.74f),
        new Color(0.45f, 0.50f, 0.55f),
    };

    Shader _cachedShader;

    void Start()
    {
        Build();
    }

    public void Build()
    {
        // Clear any existing city
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
                DestroyImmediate(child);
            else
#endif
                Destroy(child);
        }

        float cellSize = blockSize + roadWidth;
        float totalW = cellSize * blocksX + roadWidth;
        float totalD = cellSize * blocksZ + roadWidth;

        BuildGround(totalW, totalD);
        BuildRoads(cellSize, totalW, totalD);
        BuildBlocks(cellSize);
    }

    void BuildGround(float w, float d)
    {
        // Cube gives a thick BoxCollider (two-sided, no fall-through risk).
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.parent = transform;
        // Centre it under the city, sink half a unit so top surface sits at y=0.
        ground.transform.localPosition = new Vector3(w / 2f - roadWidth / 2f, -0.5f, d / 2f - roadWidth / 2f);
        ground.transform.localScale = new Vector3(w, 1f, d);

        Material mat = roadMaterial != null ? roadMaterial : CreateFlatMaterial(new Color(0.15f, 0.15f, 0.15f));
        ground.GetComponent<Renderer>().material = mat;
    }

    void BuildRoads(float cellSize, float totalW, float totalD)
    {
        Material mat = roadMaterial != null ? roadMaterial : CreateFlatMaterial(new Color(0.22f, 0.22f, 0.22f));

        // Horizontal road strips
        for (int z = 0; z <= blocksZ; z++)
        {
            float posZ = z * cellSize;
            CreateRoadStrip("RoadH_" + z, new Vector3(totalW / 2f, 0, posZ), totalW, roadWidth, mat);
        }

        // Vertical road strips
        for (int x = 0; x <= blocksX; x++)
        {
            float posX = x * cellSize;
            CreateRoadStrip("RoadV_" + x, new Vector3(posX, 0, totalD / 2f), roadWidth, totalD, mat);
        }
    }

    void CreateRoadStrip(string name, Vector3 pos, float sizeX, float sizeZ, Material mat)
    {
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = name;
        road.transform.parent = transform;
        road.transform.localPosition = pos;
        road.transform.localScale = new Vector3(sizeX, 0.05f, sizeZ);
        road.GetComponent<Renderer>().material = mat;
    }

    void BuildBlocks(float cellSize)
    {
        for (int bx = 0; bx < blocksX; bx++)
        {
            for (int bz = 0; bz < blocksZ; bz++)
            {
                float originX = bx * cellSize + roadWidth;
                float originZ = bz * cellSize + roadWidth;

                // Sidewalk/block base
                CreateSidewalk(originX, originZ);

                // Place buildings in a 2x2 grid within the block
                float usable = blockSize - buildingPadding * 2f;
                float subSize = usable / 2f;

                for (int sx = 0; sx < 2; sx++)
                {
                    for (int sz = 0; sz < 2; sz++)
                    {
                        float bHeight = Random.Range(minHeight, maxHeight);

                        // Some blocks are empty lots
                        if (Random.value < 0.15f) continue;

                        float bw = Random.Range(subSize * 0.6f, subSize * 0.92f);
                        float bd = Random.Range(subSize * 0.6f, subSize * 0.92f);

                        float cx = originX + buildingPadding + sx * subSize + subSize / 2f;
                        float cz = originZ + buildingPadding + sz * subSize + subSize / 2f;

                        Color col = _buildingColors[Random.Range(0, _buildingColors.Length)];
                        // Vary the color slightly
                        col = new Color(
                            col.r + Random.Range(-0.05f, 0.05f),
                            col.g + Random.Range(-0.05f, 0.05f),
                            col.b + Random.Range(-0.05f, 0.05f));

                        CreateBuilding(cx, bHeight / 2f, cz, bw, bHeight, bd,
                            col, $"Building_{bx}_{bz}_{sx}_{sz}");
                    }
                }
            }
        }
    }

    void CreateSidewalk(float originX, float originZ)
    {
        Material mat = sidewalkMaterial != null ? sidewalkMaterial
            : CreateFlatMaterial(new Color(0.70f, 0.68f, 0.64f));

        GameObject sw = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sw.name = "Sidewalk";
        sw.transform.parent = transform;
        sw.transform.localPosition = new Vector3(
            originX + blockSize / 2f, 0.03f, originZ + blockSize / 2f);
        sw.transform.localScale = new Vector3(blockSize, 0.06f, blockSize);
        sw.GetComponent<Renderer>().material = mat;
    }

    void CreateBuilding(float x, float y, float z,
        float w, float h, float d, Color color, string name)
    {
        GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
        building.name = name;
        building.transform.parent = transform;
        building.transform.localPosition = new Vector3(x, y, z);
        building.transform.localScale = new Vector3(w, h, d);

        Material mat = CreateFlatMaterial(color);

        // Add simple window texture via tiling
        mat.mainTextureScale = new Vector2(w / 2f, h / 3f);
        building.GetComponent<Renderer>().material = mat;
    }

    static void DestroyPoly(UnityEngine.Object obj)
    {
        if (obj == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying) { DestroyImmediate(obj); return; }
#endif
        Destroy(obj);
    }

    Material CreateFlatMaterial(Color color)
    {
        if (_cachedShader == null)
        {
            _cachedShader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard");
        }
        Material mat = new Material(_cachedShader);
        mat.SetColor("_BaseColor", color);
        mat.color = color;
        mat.SetFloat("_Smoothness", 0.1f);
        return mat;
    }
}
