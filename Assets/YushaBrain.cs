using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class YushaBrain : MonoBehaviour
{
    private NavMeshAgent agent;
    public float defaultSpeed = 3f;

    private Coroutine debuffCoroutine;

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

    // 通常の速度バフ用
    public void UpdateSpeed(float bonusSpeed)
    {
        agent.speed = defaultSpeed + bonusSpeed;
    }

    // Eキー爆弾での一時停止デバフ
    public void ApplyEKeyDebuff(float duration)
    {
        if (debuffCoroutine != null)
            StopCoroutine(debuffCoroutine);

        debuffCoroutine = StartCoroutine(DebuffCoroutine(duration));
    }

    private IEnumerator DebuffCoroutine(float duration)
    {
        float originalSpeed = agent.speed;
        agent.speed = 0f; // 一時停止

        yield return new WaitForSeconds(duration);

        agent.speed = originalSpeed; // 元の速度に戻す
        debuffCoroutine = null;
    }
}