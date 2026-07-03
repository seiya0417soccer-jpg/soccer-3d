using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// YushaBrain.cs
/// 勇者のAI制御
/// 
/// - 最も近い敵を追いかけて攻撃
/// - 敵が倒されたらEnemySpawnerに通知して次の敵をスポーン
/// - 移動中はRunアニメーション、攻撃時はAttackアニメーション
/// - バフ・デバフによる速度変更
/// - もう一度プレイ時にResetPosition()で初期位置に戻す
/// </summary>
public class YushaBrain : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Animator _animator;

    // プランナーがInspectorから調整できるパラメーター
    [SerializeField] private float _defaultSpeed = 2f;
    [SerializeField] private float _attackDistance = 2.5f; // 攻撃範囲
    [SerializeField] private float _attackDelay = 0.3f;    // 攻撃モーション後にDestroyするまでの待機時間

    // 敵を倒した時にEnemySpawnerに通知する（イベント駆動スポーン）
    [SerializeField] private EnemySpawner _enemySpawner;

    // DefaultSpeedは外部（BattleMainManager）から参照されるので読み取り用プロパティを公開
    public float DefaultSpeed => _defaultSpeed;

    private const string ParamIsMoving = "IsMoving";   // Bool
    private const string ParamAttack = "IsAttacking";  // Trigger

    private Coroutine _debuffCoroutine;
    private bool _isAttacking = false; // 攻撃中フラグ（連続攻撃防止）

    // ==================================================
    // Start: 初期化
    // ==================================================
    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();

        if (_animator == null)
            Debug.LogWarning("YushaBrain: Animatorが見つかりません");
        else
            Debug.Log("YushaBrain: Animator取得OK → " + _animator.gameObject.name);

        _agent.speed = _defaultSpeed;
        _agent.Warp(new Vector3(0, 1, 0));
    }

    // ==================================================
    // Update: 毎フレーム敵を追跡・攻撃判定
    // ==================================================
    void Update()
    {
        GameObject nearest = GetNearestEnemy();

        if (nearest != null)
        {
            _agent.SetDestination(nearest.transform.position);

            float dist = Vector3.Distance(transform.position, nearest.transform.position);

            if (dist < _attackDistance && !_isAttacking)
            {
                // 攻撃範囲内かつ攻撃中でない：攻撃アニメーション→少し待ってDestroy
                _animator?.SetBool(ParamIsMoving, false);
                _animator?.SetTrigger(ParamAttack);
                StartCoroutine(DestroyAfterAnim(nearest));
            }
            else if (!_isAttacking)
            {
                // 移動中：Runアニメーション
                _animator?.SetBool(ParamIsMoving, true);
            }
        }
        else
        {
            // 敵がいない：中央待機
            _agent.SetDestination(Vector3.zero);
            if (!_isAttacking)
            {
                bool moving = _agent.velocity.magnitude > 0.1f;
                _animator?.SetBool(ParamIsMoving, moving);
            }
        }
    }

    // ==================================================
    // 攻撃モーションの頭出し後にDestroyする
    // attackDelay秒待ってから敵を消去してスコア加算
    // EnemySpawnerに通知して次の敵をスポーンさせる
    // ==================================================
    IEnumerator DestroyAfterAnim(GameObject enemy)
    {
        _isAttacking = true;
        yield return new WaitForSeconds(_attackDelay);

        if (enemy != null)
        {
            // 敵を倒したことをEnemySpawnerに通知（イベント駆動でスポーン）
            _enemySpawner?.OnEnemyDefeated(enemy);
            Destroy(enemy);

            // ScoreManagerのメソッド経由でスコアを加算（直接書き換え不可）
            ScoreManager.Instance.AddScore(1);
        }

        _isAttacking = false;
        _animator?.SetBool(ParamIsMoving, true); // 攻撃後にRunに戻す
    }

    // ==================================================
    // 最も近い敵を取得
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
    // 速度バフ適用
    // BattleMainManagerから呼ぶ
    // ==================================================
    public void UpdateSpeed(float bonusSpeed)
    {
        _agent.speed = Mathf.Max(0f, _defaultSpeed + bonusSpeed);
    }

    // ==================================================
    // Eキーデバフ適用
    // BattleMainManagerから呼ぶ
    // ==================================================
    public void ApplyEKeyDebuff(float duration)
    {
        if (_debuffCoroutine != null)
            StopCoroutine(_debuffCoroutine);
        _debuffCoroutine = StartCoroutine(DebuffCoroutine(duration));
    }

    private IEnumerator DebuffCoroutine(float duration)
    {
        float originalSpeed = _agent.speed;
        _agent.speed = 0f;
        _animator?.SetBool(ParamIsMoving, false);
        yield return new WaitForSeconds(duration);
        _agent.speed = originalSpeed;
        _debuffCoroutine = null;
    }

    // ==================================================
    // 初期位置にリセット
    // GameFlowManagerから呼ぶ
    // ==================================================
    public void ResetPosition()
    {
        _agent.Warp(new Vector3(0, 1, 0));
        _agent.speed = _defaultSpeed;
        _isAttacking = false;
        _animator?.SetBool(ParamIsMoving, false);

        if (_debuffCoroutine != null)
        {
            StopCoroutine(_debuffCoroutine);
            _debuffCoroutine = null;
        }
    }
}