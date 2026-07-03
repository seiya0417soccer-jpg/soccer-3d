using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// YushaBrain.cs
/// 勇者のAI制御
/// 
/// - 最も近い敵を追いかけて攻撃
/// - 敵がいない場合は中央待機
/// - 移動中はRunアニメーション、攻撃時はAttackアニメーション
/// - バフ・デバフによる速度変更
/// - もう一度プレイ時にResetPosition()で初期位置に戻す
/// </summary>
public class YushaBrain : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;

    public float defaultSpeed = 2f;
    public float attackDistance = 2.5f; // 攻撃範囲（Inspectorから調整可）
    public float attackDelay = 0.3f; // 攻撃モーション後にDestroyするまでの待機時間

    private const string ParamIsMoving = "IsMoving";    // Bool
    private const string ParamAttack = "IsAttacking"; // Trigger

    private Coroutine debuffCoroutine;
    private bool isAttacking = false; // 攻撃中フラグ（連続攻撃防止）

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogWarning("YushaBrain: Animatorが見つかりません");
        else
            Debug.Log("YushaBrain: Animator取得OK → " + animator.gameObject.name);

        agent.speed = defaultSpeed;
        agent.Warp(new Vector3(0, 1, 0));
    }

    void Update()
    {
        GameObject nearest = GetNearestEnemy();

        if (nearest != null)
        {
            agent.SetDestination(nearest.transform.position);

            float dist = Vector3.Distance(transform.position, nearest.transform.position);

            if (dist < attackDistance && !isAttacking)
            {
                // 攻撃範囲内かつ攻撃中でない：攻撃アニメーション→少し待ってDestroy
                animator?.SetBool(ParamIsMoving, false);
                animator?.SetTrigger(ParamAttack);
                StartCoroutine(DestroyAfterAnim(nearest));
            }
            else if (!isAttacking)
            {
                // 移動中：Runアニメーション
                animator?.SetBool(ParamIsMoving, true);
            }
        }
        else
        {
            // 敵がいない：中央待機
            agent.SetDestination(Vector3.zero);
            if (!isAttacking)
            {
                bool moving = agent.velocity.magnitude > 0.1f;
                animator?.SetBool(ParamIsMoving, moving);
            }
        }
    }

    // ==================================================
    // 攻撃モーションの頭出し後にDestroyする
    // attackDelay秒待ってから敵を消去してスコア加算
    // ==================================================
    IEnumerator DestroyAfterAnim(GameObject enemy)
    {
        isAttacking = true;
        yield return new WaitForSeconds(attackDelay);

        if (enemy != null)
        {
            Destroy(enemy);
            ScoreManager.Instance.AddScore(1);
        }

        isAttacking = false;
        animator?.SetBool(ParamIsMoving, true); // 攻撃後にRunに戻す
    }

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

    public void UpdateSpeed(float bonusSpeed)
    {
        agent.speed = Mathf.Max(0f, defaultSpeed + bonusSpeed);
    }

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
        animator?.SetBool(ParamIsMoving, false);
        yield return new WaitForSeconds(duration);
        agent.speed = originalSpeed;
        debuffCoroutine = null;
    }

    public void ResetPosition()
    {
        agent.Warp(new Vector3(0, 1, 0));
        agent.speed = defaultSpeed;
        isAttacking = false;
        animator?.SetBool(ParamIsMoving, false);

        if (debuffCoroutine != null)
        {
            StopCoroutine(debuffCoroutine);
            debuffCoroutine = null;
        }
    }
}
