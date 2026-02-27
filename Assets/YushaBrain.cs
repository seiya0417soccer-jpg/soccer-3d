using UnityEngine;
using UnityEngine.AI;

public class YushaBrain : MonoBehaviour
{
    NavMeshAgent agent;
    float defaultSpeed; // 元の速さを保存する用

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        defaultSpeed = agent.speed; // 今のトボトボ速度を記憶
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

    // --- 【ここから追加：テトリス連動用】 ---

    // MinoController から「Boost」という名前で呼び出される
    public void Boost()
    {
        // 加速中の色変え（任意：黄色に光るぜ！）
        GetComponent<Renderer>().material.color = Color.yellow;

        agent.speed = 15f; // 爆速モード

        // 3秒後に元のスピードと色に戻す
        CancelInvoke("ResetStatus");
        Invoke("ResetStatus", 3f);
    }

    void ResetStatus()
    {
        agent.speed = defaultSpeed;
        GetComponent<Renderer>().material.color = Color.blue; // 元の青に戻す
    }
}