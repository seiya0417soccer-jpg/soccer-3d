using UnityEngine;
using TMPro;

/// <summary>
/// GameTimer.cs
/// ゲームの残り時間を管理するタイマー
/// 
/// - StartTimer()が呼ばれるまでカウントしない
/// - 時間切れでGameFlowManager.OnFinish()を呼ぶ
/// - ResetTimer()でもう一度プレイ時にリセット
/// </summary>
public class GameTimer : MonoBehaviour
{
    // 他スクリプトから残り時間を参照できるよう static で公開
    public static float timeRemaining = 90f;
    public static bool isGameOver = false;

    [SerializeField] private float totalTime = 90f; // ゲーム時間（Inspectorから変更可）
    public GameObject timerObject;                  // タイマーテキストを持つオブジェクト

    private bool isRunning = false; // StartTimer()が呼ばれるまで動かない

    // ==================================================
    // Start: 初期化
    // ==================================================
    void Start()
    {
        timeRemaining = totalTime;
        isGameOver = false;
        isRunning = false;
    }

    // ==================================================
    // StartTimer: タイマー開始
    // カウントダウンのGO!後にGameFlowManagerから呼ばれる
    // ==================================================
    public void StartTimer()
    {
        isRunning = true;
    }

    // ==================================================
    // ResetTimer: タイマーリセット
    // もう一度プレイ時にGameFlowManagerから呼ばれる
    // ==================================================
    public void ResetTimer()
    {
        timeRemaining = totalTime; // 残り時間をリセット
        isGameOver = false;     // ゲームオーバーフラグをリセット
        isRunning = false;     // StartTimer()が呼ばれるまで止める
    }

    // ==================================================
    // Update: 毎フレームタイマーを減らしてテキストを更新
    // ==================================================
    void Update()
    {
        if (!isRunning) return; // 開始前は何もしない
        if (isGameOver) return; // 時間切れ後は何もしない

        // 残り時間を減らす
        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            isGameOver = true;
            isRunning = false;
            OnTimeUp(); // 時間切れ処理
        }

        // テキスト更新（TMPと標準Text両対応）
        string display = FormatTime(timeRemaining);
        var tmp = timerObject.GetComponent<TextMeshProUGUI>();
        var legacy = timerObject.GetComponent<UnityEngine.UI.Text>();
        if (tmp != null) tmp.text = display;
        if (legacy != null) legacy.text = display;
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
