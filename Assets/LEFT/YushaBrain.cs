using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class YushaBrain : MonoBehaviour
{
    private NavMeshAgent agent;
    public float defaultSpeed = 2f;

    private Coroutine debuffCoroutine;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = defaultSpeed;
        agent.Warp(new Vector3(0, 1, 0));
    }

    void Update()
    {
        GameObject nearest = GetNearestEnemy();

        if (nearest != null)
        {
            agent.SetDestination(nearest.transform.position);

            if (Vector3.Distance(transform.position, nearest.transform.position) < 1.5f)
            {
                Destroy(nearest);
                ScoreManager.score += 1;
            }
        }
        else
        {
            // 敵がいない場合は中央待機
            agent.SetDestination(Vector3.zero);
        }
    }

    // ==================================================
    // 最も近い敵を返す
    // FindObjectsByTagで全敵を取得し、距離で比較
    // ==================================================
    GameObject GetNearestEnemy()
    {
        // Unity6以降はFindObjectsByTag、旧バージョンはFindGameObjectsWithTag
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        GameObject nearest = null;
        float minDist = float.MaxValue;

        foreach (var enemy in enemies)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = enemy;
            }
        }

        return nearest;
    }

    // ==================================================
    // 速度バフ更新
    // ==================================================
    public void UpdateSpeed(float bonusSpeed)
    {
        agent.speed = Mathf.Max(0f, defaultSpeed + bonusSpeed);
    }

    // ==================================================
    // Eキー爆弾デバフ（BattleMainManagerから呼ばれる想定だが念のため残す）
    // ==================================================
    public void ApplyEKeyDebuff(float duration)
    {
        if (debuffCoroutine != null)
            StopCoroutine(debuffCoroutine);
        debuffCoroutine = StartCoroutine(DebuffCoroutine(duration));
    }

    private IEnumerator DebuffCoroutine(float duration)
    {
        float originalSpeed = agent.speed;
        agent.speed = 0f;
        yield return new WaitForSeconds(duration);
        agent.speed = originalSpeed;
        debuffCoroutine = null;
    }
}
