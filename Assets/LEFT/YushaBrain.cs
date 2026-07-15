using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using VContainer;

/// <summary>
/// YushaBrain.cs
/// 勇者のAI制御
/// 
/// - 最も近い敵を追いかけて攻撃
/// - 敵が倒されたらEnemySpawnerに通知して次の敵をスポーン
/// - 移動中はRunアニメーション、攻撃時はAttackアニメーション
/// - バフ・デバフによる速度変更
/// - デバフ中はUpdate処理をスキップしてアニメーションが上書きされないようにする
/// - もう一度プレイ時にResetPosition()で初期位置に戻す
/// - IScoreWriterを通してスコアを加算（ScoreManager直接参照をやめる）
/// - 敵を倒した時にCameraFollowのShakeCamera()を呼んで画面を揺らす
/// - バフ量に応じてシアン色に発光・デバフ時に赤く発光する
/// - YushaSettingSOでパラメーターを管理（プランナーが調整可能）
/// </summary>
public class YushaBrain : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Animator _animator;

    // ==================================================
    // Inject: VContainerから依存を注入される
    // ==================================================
    [Inject]
    public void Construct(IScoreWriter scoreWriter)
    {
        // IScoreWriterをInjectで受け取る（ScoreManager.Instance直接参照をやめる）
        _scoreWriter = scoreWriter;
    }

    // プランナーが調整できるパラメーターをSOで管理
    [SerializeField] private YushaSettingSO _yushaSettingSO;

    // 敵を倒した時にEnemySpawnerに通知する（イベント駆動スポーン）
    [SerializeField] private EnemySpawner _enemySpawner;

    // 敵を倒した時にカメラを揺らす
    [SerializeField] private CameraFollow _cameraFollow;

    // DefaultSpeedは外部（BattleMainManager）から参照されるので読み取り用プロパティを公開
    public float DefaultSpeed => _yushaSettingSO.DefaultSpeed;

    // IScoreWriterを通してスコアを加算する（ScoreManager直接参照をやめる）
    private IScoreWriter _scoreWriter;

    // 発光制御用のSkinnedMeshRenderer・マテリアル
    private SkinnedMeshRenderer _meshRenderer;
    private Material _material; // 元のマテリアルを汚さないようにコピーして使う

    private const string ParamIsMoving = "IsMoving";   // Bool
    private const string ParamAttack = "IsAttacking";  // Trigger

    private Coroutine _debuffCoroutine;
    private bool _isAttacking = false;   // 攻撃中フラグ（連続攻撃防止）
    private bool _isDebuffActive = false; // デバフ中フラグ（Update処理をスキップする）

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

        if (_yushaSettingSO == null)
            Debug.LogError("YushaBrain: YushaSettingSOがセットされていません！");

        _agent.speed = _yushaSettingSO.DefaultSpeed;
        _agent.Warp(new Vector3(0, 1, 0));

        // SkinnedMeshRendererを取得してマテリアルをコピーする
        // 元のマテリアルを汚さないようにコピーして使う
        _meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (_meshRenderer != null)
        {
            _material = new Material(_meshRenderer.material);
            _meshRenderer.material = _material;
            _material.EnableKeyword("_EMISSION");
        }
    }

    // ==================================================
    // Update: 毎フレーム敵を追跡・攻撃判定
    // デバフ中はスキップしてアニメーションが上書きされないようにする
    // ==================================================
    void Update()
    {
        // デバフ中はUpdate処理をスキップ（足踏みアニメーション防止）
        if (_isDebuffActive) return;

        GameObject nearest = GetNearestEnemy();

        if (nearest != null)
        {
            _agent.SetDestination(nearest.transform.position);

            float dist = Vector3.Distance(transform.position, nearest.transform.position);

            if (dist < _yushaSettingSO.AttackDistance && !_isAttacking)
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
        yield return new WaitForSeconds(_yushaSettingSO.AttackDelay);

        if (enemy != null)
        {
            // 敵を倒したことをEnemySpawnerに通知（イベント駆動でスポーン）
            _enemySpawner?.OnEnemyDefeated(enemy);
            Destroy(enemy);

            // IScoreWriterを通してスコアを加算（ScoreManager直接参照をやめる）
            _scoreWriter?.AddScore(1);

            // 敵を倒した時にカメラを揺らして倒してる感を出す
            _cameraFollow?.ShakeCamera();
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
        _agent.speed = Mathf.Max(0f, _yushaSettingSO.DefaultSpeed + bonusSpeed);
    }

    // ==================================================
    // 発光強度を設定する
    // BattleMainManagerからバフ量に応じて呼ぶ
    // intensityが0なら発光なし・大きいほど強く光る
    // ==================================================
    public void SetEmission(float intensity)
    {
        if (_material == null) return;

        // バフ時はシアン色で発光
        Color emissionColor = Color.cyan * intensity;
        _material.SetColor("_EmissionColor", emissionColor);
    }

    // ==================================================
    // デバフ時の発光色を設定する
    // デバフ中は赤く光らせる
    // ==================================================
    public void SetDebuffEmission(bool isDebuff)
    {
        if (_material == null) return;

        if (isDebuff)
            // デバフ中は赤く光る
            _material.SetColor("_EmissionColor", Color.red * 1.5f);
        else
            // デバフ解除後は発光なしに戻す
            _material.SetColor("_EmissionColor", Color.black);
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

    // ==================================================
    // デバフCoroutine
    // デバフ中フラグをtrueにしてUpdateをスキップさせる
    // デバフ終了後にフラグをfalseに戻す
    // ==================================================
    private IEnumerator DebuffCoroutine(float duration)
    {
        float originalSpeed = _agent.speed;
        _agent.speed = 0f;
        _isDebuffActive = true;                        // デバフ開始・Updateをスキップ
        _animator?.SetBool(ParamIsMoving, false);      // 足踏みアニメーションを止める
        SetDebuffEmission(true);                       // デバフ中は赤く光る
        yield return new WaitForSeconds(duration);
        _agent.speed = originalSpeed;
        _isDebuffActive = false;                       // デバフ終了・Updateを再開
        SetDebuffEmission(false);                      // デバフ解除後は発光なしに戻す
        _debuffCoroutine = null;
    }

    // ==================================================
    // 初期位置にリセット
    // GameFlowManagerから呼ぶ
    // ==================================================
    public void ResetPosition()
    {
        _agent.Warp(new Vector3(0, 1, 0));
        _agent.speed = _yushaSettingSO.DefaultSpeed;
        _isAttacking = false;
        _isDebuffActive = false;                       // デバフフラグもリセット
        _animator?.SetBool(ParamIsMoving, false);
        SetDebuffEmission(false);                      // リセット時に発光をリセット

        if (_debuffCoroutine != null)
        {
            StopCoroutine(_debuffCoroutine);
            _debuffCoroutine = null;
        }
    }
}