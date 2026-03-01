using UnityEngine;
using UnityEngine.AI;

public class YushaBrain : MonoBehaviour
{
    NavMeshAgent agent;
    public float defaultSpeed = 3f;
    

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = defaultSpeed;
        agent.Warp(new Vector3(0, 1, 0));
    }

    void Update()
    {
        GameObject enemy = GameObject.FindWithTag("Enemy");

        if (enemy != null)
        {
            agent.SetDestination(enemy.transform.position);
            if (Vector3.Distance(transform.position, enemy.transform.position) < 1.5f)
            {
                Destroy(enemy);
                ScoreManager.score += 1;
            }
        }
        else if (agent.remainingDistance < 0.5f)
        {
            Vector3 randomPos = Random.insideUnitSphere * 15f;
            randomPos.y = transform.position.y;
            agent.SetDestination(randomPos);
        }
        else
        {
            agent.SetDestination(Vector3.zero);
        }
    }
}