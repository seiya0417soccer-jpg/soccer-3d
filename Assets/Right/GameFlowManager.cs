using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VContainer;

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

    [Header("UI")]
    [SerializeField] private GameObject killCountObject;  // キルカウント表示（終了時に非表示）
    [SerializeField] private GameObject timerTextObject;  // タイマー表示（終了時に非表示）

    // VContainerでDI注入される依存クラス（SerializeFieldをやめてInjectに変更）
    private GameTimer _gameTimer;
    private DropPuzzleBattle _dropPuzzleBattle;
    private YushaBrain _yushaBrain;
    private EnemySpawner _enemySpawner;

    // ==================================================
    // Inject: VContainerから依存を注入される
    // SerializeFieldの代わりにこちらで依存を受け取る
    // ==================================================
    [Inject]
    public void Construct(
        GameTimer gameTimer,
        DropPuzzleBattle dropPuzzleBattle,
        YushaBrain yushaBrain,
        EnemySpawner enemySpawner)
    {
        _gameTimer = gameTimer;
        _dropPuzzleBattle = dropPuzzleBattle;
        _yushaBrain = yushaBrain;
        _enemySpawner = enemySpawner;
    }

    // ==================================================
    // Awake: シングルトン登録
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
        Time.timeScale = 1f;

        // デバッグ用：_gameTimerがnullかどうか確認
        Debug.Log($"_gameTimer: {_gameTimer}");
        _gameTimer?.StartTimer();
    }

    // ==================================================
    // フィニッシュ（時間切れ）
    // GameTimerから呼ばれる
    // ==================================================
    public void OnFinish()
    {
        Time.timeScale = 0f;
        killCountObject?.SetActive(false); // キルカウントを非表示
        timerTextObject?.SetActive(false); // タイマーを非表示
        finishPanel.SetActive(true);
        StartCoroutine(WaitForFinishInput());
    }

    // フィニッシュ：エンターキー待ち
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
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                GoToResult();
                yield break;
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        GoToResult();
    }

    // ==================================================
    // リザルト画面へ遷移
    // ゲームオーバー・フィニッシュ両方から呼ばれる
    // ==================================================
    void GoToResult()
    {
        gameOverPanel.SetActive(false);
        finishPanel.SetActive(false);
        ResultManager.Instance.ShowResult();
    }

    // ==================================================
    // もう一度（カウントダウンから再スタート）
    // ResultManagerから呼ばれる
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

        // テトリスをリセット
        _dropPuzzleBattle?.ResetGame();

        // タイマーをリセット
        _gameTimer?.ResetTimer();

        // 勇者を初期位置に戻す
        _yushaBrain?.ResetPosition();

        // 敵を全リセットして再スポーン
        _enemySpawner?.ResetEnemies();

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

        // テトリスをリセット
        _dropPuzzleBattle?.ResetGame();

        // タイマーをリセット
        _gameTimer?.ResetTimer();

        // 勇者を初期位置に戻す
        _yushaBrain?.ResetPosition();

        // 敵を全リセットして再スポーン
        _enemySpawner?.ResetEnemies();

        Time.timeScale = 0f;
        titlePanel.SetActive(true);
    }
}