using UnityEngine;
using TMPro;

/// <summary>
/// GameTimer.cs
/// ゲームの残り時間を管理するタイマー
/// 
/// - StartTimer()が呼ばれるまでカウントしない
/// - 時間切れでGameFlowManager.OnFinish()を呼ぶ
/// - ResetTimer()でもう一度プレイ時にリセット＆テキスト即時更新
/// </summary>
public class GameTimer : MonoBehaviour
{
    // 残り時間はprivateで管理（外部から直接書き換え不可）
    private float _timeRemaining;
    private bool _isGameOver = false;
    private bool _isRunning = false;

    // 外部からは読み取りのみ可能
    public float TimeRemaining => _timeRemaining;
    public bool IsGameOver => _isGameOver;

    [SerializeField] private float _totalTime = 90f; // ゲーム時間（Inspectorから変更可）

    // TMPのテキストコンポーネントをキャッシュしておく（毎フレームGetComponentしない）
    [SerializeField] private TextMeshProUGUI _timerText;

    // ==================================================
    // Start: 初期化
    // ==================================================
    void Start()
    {
        _timeRemaining = _totalTime;
        _isGameOver = false;
        _isRunning = false;
        UpdateText();
    }

    // ==================================================
    // StartTimer: タイマー開始
    // カウントダウンのGO!後にGameFlowManagerから呼ばれる
    // ==================================================
    public void StartTimer()
    {
        _isRunning = true;
    }

    // ==================================================
    // ResetTimer: タイマーリセット
    // もう一度プレイ時にGameFlowManagerから呼ばれる
    // ==================================================
    public void ResetTimer()
    {
        _timeRemaining = _totalTime;
        _isGameOver = false;
        _isRunning = false;

        // テキストを即時更新（カウントダウン中に古い時間が見えないよう）
        UpdateText();
    }

    // ==================================================
    // Update: 毎フレームタイマーを減らしてテキストを更新
    // ==================================================
    void Update()
    {
        if (!_isRunning) return;  // 開始前は何もしない
        if (_isGameOver) return;  // 時間切れ後は何もしない

        _timeRemaining -= Time.deltaTime;

        if (_timeRemaining <= 0f)
        {
            _timeRemaining = 0f;
            _isGameOver = true;
            _isRunning = false;
            UpdateText();
            OnTimeUp();
            return;
        }

        UpdateText();
    }

    // ==================================================
    // UpdateText: タイマーテキストを更新
    // キャッシュしたTMPコンポーネントを使う
    // ==================================================
    void UpdateText()
    {
        if (_timerText != null)
            _timerText.text = FormatTime(_timeRemaining);
    }

    // ==================================================
    // FormatTime: 秒を "1:30" 形式にフォーマット
    // ==================================================
    string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return string.Format("{0}:{1:00}", m, s);
    }

    // ==================================================
    // OnTimeUp: 時間切れ処理
    // GameFlowManagerにフィニッシュを通知
    // ==================================================
    void OnTimeUp()
    {
        Debug.Log("Time Up!");
        GameFlowManager.Instance?.OnFinish();
    }
}