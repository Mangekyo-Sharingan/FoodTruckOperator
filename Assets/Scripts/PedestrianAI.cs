using UnityEngine;
using UnityEngine.AI;

public class PedestrianAI : MonoBehaviour
{
    private const float SamplePositionRadius = 10f;
    private const float RemainingDistanceThreshold = 0.5f;

    private NavMeshAgent _agent;
    private float _waitTime;
    private float _waitTimer;
    private Vector3 _spawnCenter = Vector3.zero;
    private float _cityRadius = 50f;
    private float _despawnRadius = 80f;

    public void Initialize()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null)
        {
            Debug.LogError($"NavMeshAgent not found on {gameObject.name}");
            return;
        }

        _agent.speed = 2f;
        _agent.enabled = true;
        PickNewDestination();
    }

    private void Update()
    {
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return;

        _waitTimer += Time.deltaTime;
        if (_waitTimer >= _waitTime || _agent.remainingDistance < RemainingDistanceThreshold)
        {
            PickNewDestination();
        }

        float sqrDespawnRadius = _despawnRadius * _despawnRadius;
        if ((transform.position - _spawnCenter).sqrMagnitude > sqrDespawnRadius)
        {
            gameObject.SetActive(false);
        }
    }

    private void PickNewDestination()
    {
        _waitTimer = 0f;
        _waitTime = Random.Range(1f, 2f);

        Vector2 randomPoint = Random.insideUnitCircle * _cityRadius;
        Vector3 target = new Vector3(randomPoint.x, 0, randomPoint.y);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(target, out hit, SamplePositionRadius, NavMesh.AllAreas))
        {
            _agent.SetDestination(hit.position);
        }
    }
}
