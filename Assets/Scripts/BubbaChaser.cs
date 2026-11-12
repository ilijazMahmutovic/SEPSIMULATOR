using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BubbaChaser : MonoBehaviour
{
    public Transform target;
    public float catchDistance = 1.35f;
    public float repathInterval = 0.2f;
    NavMeshAgent agent;
    float nextPathTime;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = 3.65f;
        agent.acceleration = 12f;
        agent.angularSpeed = 540f;
        agent.stoppingDistance = 0.6f;
        agent.autoBraking = false;
    }

    void Update()
    {
        if (target == null || GameManager.GameOver) return;
        if (Time.time >= nextPathTime)
        {
            nextPathTime = Time.time + repathInterval;
            if (agent.isOnNavMesh) agent.SetDestination(target.position);
            else transform.position = Vector3.MoveTowards(transform.position, target.position, agent.speed * Time.deltaTime);
        }
        if (Vector3.Distance(transform.position, target.position) <= catchDistance)
            GameManager.EndGame(false);
    }
}

