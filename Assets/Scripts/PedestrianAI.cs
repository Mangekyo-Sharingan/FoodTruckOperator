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
