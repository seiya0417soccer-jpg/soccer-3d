using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// GameFlowManager.cs
/// ゲーム全体のフロー管理
/// 
/// 画面遷移の流れ：
/// タイトル → 操作説明 → カウントダウン → ゲーム中
/// ゲーム中 → ゲームオーバー or フィニッシュ → リザルト
/// リザルト → もう一度（カウントダウンから） or タイトルへ
/// 
/// カウントダウン中・ゲームオーバー・フィニッシュ中は
/// Time.timeScale = 0 で全ての動きを停止
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    // シングルトン：他スクリプトからGameFlowManager.Instanceでアクセス
    public static GameFlowManager Instance;

    [Header("Panels")]
    [SerializeField] private GameObject titlePanel;    // タイトル画面
    [SerializeField] private GameObject manualPanel;   // 操作説明画面
    [SerializeField] private GameObject readyGoGroup;  // カウントダウン画面
    [SerializeField] private GameObject gameOverPanel; // ゲームオーバー画面
    [SerializeField] private GameObject finishPanel;   // フィニッシュ画面

    [Header("ReadyGo")]
    [SerializeField] private TextMeshProUGUI countdownText; // 3,2,1,GO!を表示するTMPテキスト

    [Header("References")]
    [SerializeField] private GameTimer gameTimer;       // タイマー（GO!後に開始・もう一度でリセット）
    [SerializeField] private DropPuzzleBattle dropPuzzleBattle; // テトリス（もう一度時にリセット）
    [SerializeField] private GameObject killCountObject;  // キルカウント表示（終了時に非表示）
    [SerializeField] private GameObject timerTextObject;  // タイマー表示（終了時に非表示）

    // ==================================================
    // Awake: シングルトン登録
    // ==================================================
    void Awake()
    {
        Instance = this;
    }

    // ==================================================
    // Start: 初期状態設定
    // 全停止・タイトル表示・他パネルを非表示
    // ==================================================
    void Start()
    {
        Time.timeScale = 0f; // 全停止（タイトル表示中はゲームを動かさない）
        titlePanel.SetActive(true);
        manualPanel.SetActive(false);
        readyGoGroup.SetActive(false);
        gameOverPanel.SetActive(false);
        finishPanel.SetActive(false);
    }

    // ==================================================
    // Update: エンターキーで画面遷移
    // タイトルまたは操作説明が表示中のみ反応
    // ==================================================
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (titlePanel.activeSelf)
                OnTitleStart();
            else if (manualPanel.activeSelf)
                OnManualNext();
        }
    }

    // ==================================================
    // タイトル画面：エンターキーで操作説明へ
    // ==================================================
    public void OnTitleStart()
    {
        titlePanel.SetActive(false);
        manualPanel.SetActive(true);
    }

    // ==================================================
    // 操作説明画面：エンターキーでカウントダウンへ
    // ==================================================
    public void OnManualNext()
    {
        manualPanel.SetActive(false);
        readyGoGroup.SetActive(true);
        StartCoroutine(CountdownCoroutine());
    }

    // ==================================================
    // カウントダウン → GO! → ゲーム開始
    // Time.timeScale = 0 中でも動くWaitForSecondsRealtimeを使用
    // ==================================================
    IEnumerator CountdownCoroutine()
    {
        countdownText.text = "3";
        yield return new WaitForSecondsRealtime(1f);
        countdownText.text = "2";
        yield return new WaitForSecondsRealtime(1f);
        countdownText.text = "1";
        yield return new WaitForSecondsRealtime(1f);
        countdownText.text = "GO!";
        yield return new WaitForSecondsRealtime(0.8f);

        readyGoGroup.SetActive(false);
        Time.timeScale = 1f; // ゲーム開始

        // タイマーのカウントを開始
        if (gameTimer != null)
            gameTimer.StartTimer();
    }

    // ==================================================
    // フィニッシュ（時間切れ）
    // GameTimerから呼ばれる
    // エンターキーが押されるまでフィニッシュ画面を表示し続ける
    // ==================================================
    public void OnFinish()
    {
        Time.timeScale = 0f;
        killCountObject?.SetActive(false); // キルカウントを非表示
        timerTextObject?.SetActive(false); // タイマーを非表示
        finishPanel.SetActive(true);
        StartCoroutine(WaitForFinishInput());
    }

    // フィニッシュ：エンターキー待ち（時間制限なし）
    IEnumerator WaitForFinishInput()
    {
        while (true)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                GoToResult();
                yield break;
            }
            yield return null;
        }
    }

    // ==================================================
    // ゲームオーバー
    // DropPuzzleBattleから呼ばれる
    // 3秒経過 または エンターキーでリザルトへ
    // ==================================================
    public void OnGameOver()
    {
        Time.timeScale = 0f;
        killCountObject?.SetActive(false); // キルカウントを非表示
        timerTextObject?.SetActive(false); // タイマーを非表示
        gameOverPanel.SetActive(true);
        StartCoroutine(WaitForGameOverTransition());
    }

    // ゲームオーバー：3秒待機またはエンターキーでリザルトへ
    IEnumerator WaitForGameOverTransition()
    {
        float elapsed = 0f;
        while (elapsed < 3f)
        {
            // エンターキーで即リザルトへ
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                GoToResult();
                yield break;
            }
            elapsed += Time.unscaledDeltaTime; // timeScale=0でも動くunscaledDeltaTimeを使用
            yield return null;
        }
        GoToResult(); // 3秒経過で自動遷移
    }

    // ==================================================
    // リザルト画面へ遷移
    // ゲームオーバー・フィニッシュ両方から呼ばれる
    // ==================================================
    void GoToResult()
    {
        gameOverPanel.SetActive(false);
        finishPanel.SetActive(false);
        ResultManager.Instance.ShowResult(); // リザルト画面を表示してスコアを反映
    }

    // ==================================================
    // もう一度（カウントダウンから再スタート）
    // ResultManagerから呼ばれる
    // テトリス・タイマーをリセットしてカウントダウンから再開
    // ==================================================
    public void RestartFromCountdown()
    {
        // 全パネルを非表示
        titlePanel.SetActive(false);
        manualPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        finishPanel.SetActive(false);

        // キルカウントとタイマーを再表示
        killCountObject?.SetActive(true);
        timerTextObject?.SetActive(true);

        // テトリスをリセット（フィールドクリア・ピース再生成）
        dropPuzzleBattle?.ResetGame();

        // タイマーをリセット（残り時間を戻してStartTimer待ち状態に）
        gameTimer?.ResetTimer();

        // カウントダウン開始
        readyGoGroup.SetActive(true);
        StartCoroutine(CountdownCoroutine());
    }

    // ==================================================
    // タイトルへ戻る
    // ResultManagerから呼ばれる
    // ==================================================
    public void GoToTitle()
    {
        gameOverPanel.SetActive(false);
        finishPanel.SetActive(false);

        // キルカウントとタイマーを再表示
        killCountObject?.SetActive(true);
        timerTextObject?.SetActive(true);

        Time.timeScale = 0f;
        titlePanel.SetActive(true);
    }
}
