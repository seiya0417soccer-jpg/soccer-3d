using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class YushaBrain : MonoBehaviour
{
    // NavMeshAgentコンポーネント（勇者の移動制御用）
    private NavMeshAgent agent;

    // 勇者の基本速度
    public float defaultSpeed = 3f;

    // デバフ用コルーチンの参照（Eキー爆弾の一時停止用）
    private Coroutine debuffCoroutine;

    void Start()
    {
        // NavMeshAgentコンポーネントを取得
        agent = GetComponent<NavMeshAgent>();

        // 初期速度を設定
        agent.speed = defaultSpeed;

        // 初期位置をセット（Y=1で地面上）
        agent.Warp(new Vector3(0, 1, 0));
    }

    void Update()
    {
        // シーン上のEnemyタグを持つオブジェクトを取得
        GameObject enemy = GameObject.FindWithTag("Enemy");

        if (enemy != null)
        {
            // 敵が存在する場合、勇者を敵の位置へ移動させる
            agent.SetDestination(enemy.transform.position);

            // 敵に接近したら破壊してスコア加算
            if (Vector3.Distance(transform.position, enemy.transform.position) < 1.5f)
            {
                Destroy(enemy);
                ScoreManager.score += 1;
            }
        }
        else if (agent.remainingDistance < 0.5f)
        {
            // 敵がいなく、目的地に到達していた場合はランダム移動
            Vector3 randomPos = Random.insideUnitSphere * 15f;
            randomPos.y = transform.position.y;
            agent.SetDestination(randomPos);
        }
        else
        {
            // その他の場合は中央(0,0,0)を目指す
            agent.SetDestination(Vector3.zero);
        }
    }

    // --- 通常の速度バフ用関数 ---
    // bonusSpeedを加算して勇者の移動速度を更新
    public void UpdateSpeed(float bonusSpeed)
    {
        agent.speed = defaultSpeed + bonusSpeed;
    }

    // --- Eキー爆弾による一時停止デバフ ---
    // 指定時間、勇者を停止させる
    public void ApplyEKeyDebuff(float duration)
    {
        // すでにデバフ中の場合はコルーチンを停止
        if (debuffCoroutine != null)
            StopCoroutine(debuffCoroutine);

        // デバフ用コルーチンを開始
        debuffCoroutine = StartCoroutine(DebuffCoroutine(duration));
    }

    // デバフのコルーチン
    private IEnumerator DebuffCoroutine(float duration)
    {
        // 現在の速度を保持
        float originalSpeed = agent.speed;

        // 勇者を一時停止
        agent.speed = 0f;

        // 指定時間待機
        yield return new WaitForSeconds(duration);

        // デバフ解除、元の速度に戻す
        agent.speed = originalSpeed;

        // コルーチン参照をクリア
        debuffCoroutine = null;
    }
}