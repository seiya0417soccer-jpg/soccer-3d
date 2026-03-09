using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// YushaBrain.cs
/// 勇者のAI制御
/// 
/// - 最も近い敵を追いかけて攻撃
/// - 敵がいない場合は中央待機
/// - バフ・デバフによる速度変更
/// - もう一度プレイ時にResetPosition()で初期位置に戻す
/// </summary>
public class YushaBrain : MonoBehaviour
{
    private NavMeshAgent agent;
    public float defaultSpeed = 2f; // 基本移動速度

    private Coroutine debuffCoroutine;

    // ==================================================
    // Start: 初期化
    // NavMeshAgentを取得して初期位置に配置
    // ==================================================
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = defaultSpeed;
        agent.Warp(new Vector3(0, 1, 0)); // 初期位置は中央
    }

    // ==================================================
    // Update: 毎フレーム処理
    // 最も近い敵を追いかけて1.5f以内で攻撃・消去
    // ==================================================
    void Update()
    {
        GameObject nearest = GetNearestEnemy();
        if (nearest != null)
        {
            agent.SetDestination(nearest.transform.position);

            // 敵に近づいたら攻撃（消去）してスコア加算
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
    // FindGameObjectsWithTagで全敵を取得し、距離で比較
    // ==================================================
    GameObject GetNearestEnemy()
    {
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
    // BattleMainManagerから呼ばれる
    // ==================================================
    public void UpdateSpeed(float bonusSpeed)
    {
        agent.speed = Mathf.Max(0f, defaultSpeed + bonusSpeed);
    }

    // ==================================================
    // Eキー爆弾デバフ
    // duration秒間速度を0にする
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

    // ==================================================
    // リセット
    // もう一度プレイ時にGameFlowManagerから呼ばれる
    // 初期位置に戻して速度をリセット
    // ==================================================
    public void ResetPosition()
    {
        agent.Warp(new Vector3(0, 1, 0)); // 初期位置に瞬間移動
        agent.speed = defaultSpeed;       // 速度をリセット

        // デバフ中なら止める
        if (debuffCoroutine != null)
        {
            StopCoroutine(debuffCoroutine);
            debuffCoroutine = null;
        }
    }
}
