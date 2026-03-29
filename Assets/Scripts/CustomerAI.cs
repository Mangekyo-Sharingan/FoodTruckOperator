using UnityEngine;
using UnityEngine.AI;

public class CustomerAI : MonoBehaviour
{
    private NavMeshAgent _agent;
    private const float PassRadius = 15f;
    private const float CityEdge = 60f;
    private const float SamplePositionRadius = 10f;

    public void Initialize()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null)
        {
            Debug.LogError("[CustomerAI] NavMeshAgent not found!");
            return;
        }
        _agent.speed = 2.5f;
        _agent.enabled = true;
        SetPathThroughCity();
    }

    private void Update()
    {
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return;

        if (_agent.remainingDistance < 1f || IsPastEdge())
        {
            gameObject.SetActive(false);
        }
    }

    private bool IsPastEdge()
    {
        return Mathf.Abs(transform.position.x) > CityEdge || 
               Mathf.Abs(transform.position.z) > CityEdge;
    }

    private void SetPathThroughCity()
    {
        int edge = Random.Range(0, 4);
        Vector3 startPos, endPos;

        switch (edge)
        {
            case 0: // North to South
                startPos = new Vector3(Random.Range(-PassRadius, PassRadius), 0, CityEdge);
                endPos = new Vector3(Random.Range(-PassRadius, PassRadius), 0, -CityEdge);
                break;
            case 1: // South to North
                startPos = new Vector3(Random.Range(-PassRadius, PassRadius), 0, -CityEdge);
                endPos = new Vector3(Random.Range(-PassRadius, PassRadius), 0, CityEdge);
                break;
            case 2: // East to West
                startPos = new Vector3(CityEdge, 0, Random.Range(-PassRadius, PassRadius));
                endPos = new Vector3(-CityEdge, 0, Random.Range(-PassRadius, PassRadius));
                break;
            default: // West to East
                startPos = new Vector3(-CityEdge, 0, Random.Range(-PassRadius, PassRadius));
                endPos = new Vector3(CityEdge, 0, Random.Range(-PassRadius, PassRadius));
                break;
        }

        NavMeshHit hit;
        if (NavMesh.SamplePosition(startPos, out hit, SamplePositionRadius, NavMesh.AllAreas))
        {
            transform.position = hit.position;
        }

        if (NavMesh.SamplePosition(endPos, out hit, SamplePositionRadius, NavMesh.AllAreas))
        {
            _agent.SetDestination(hit.position);
        }
    }
}
