using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ScoreManager.cs
/// キル数（スコア）の管理とUI表示
/// 
/// - scoreはprivateでカプセル化（外部から直接書き換え不可）
/// - AddScore()で加算・ResetScore()でリセット
/// - Score プロパティで読み取りのみ可能
/// - スコアが変わった時だけUIを更新する（毎フレームGetComponentをやめる）
/// </summary>
public class ScoreManager : MonoBehaviour
{
    // シングルトン：他スクリプトからScoreManager.Instanceでアクセス
    public static ScoreManager Instance;

    // スコアはprivateで管理（外部から直接書き換えできない）
    private int _score = 0;

    // 外部からは読み取りのみ可能
    public int Score => _score;

    // 旧TextコンポーネントをキャッシュしておくI（毎フレームGetComponentしない）
    [SerializeField] private Text _scoreText;

    // ==================================================
    // Awake: Singleton登録
    // ==================================================
    void Awake()
    {
        // 既にインスタンスが存在する場合は自分を破棄する（重複防止）
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 最初の1つだけ登録する
        Instance = this;
    }

    // ==================================================
    // Start: UI初期化
    // ==================================================
    void Start()
    {
        // 起動時にUIを初期状態に更新する
        UpdateUI();
    }

    // ==================================================
    // スコア加算
    // 外部からはこのメソッドを通してスコアを変更する
    // ==================================================
    public void AddScore(int amount)
    {
        _score += amount;

        // スコアが変わった時だけUIを更新する
        UpdateUI();
    }

    // ==================================================
    // スコアリセット
    // ResultManagerから呼ぶ
    // ==================================================
    public void ResetScore()
    {
        _score = 0;

        // リセット後にUIを更新する
        UpdateUI();
    }

    // ==================================================
    // UI更新（内部処理）
    // スコアが変わった時だけ呼ぶ
    // ==================================================
    private void UpdateUI()
    {
        if (_scoreText != null)
            _scoreText.text = "KILLS: " + _score;
    }
}