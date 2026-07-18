using R3;
using System.Collections;
using TMPro;
using UnityEngine;
using VContainer;

/// <summary>
/// GameFlowManager.cs
/// ゲーム全体のフロー管理
/// 
/// Stateパターンで状態を管理する
/// 画面遷移の流れ：
/// TitleState → ManualState → CountdownState → PlayingState
/// PlayingState → GameOverState or FinishState → ResultState
/// ResultState → CountdownState or TitleState
/// </summary>
public class GameFlowManager : MonoBehaviour
{

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

    // VContainerでDI注入される依存クラス
    private GameTimer _gameTimer;
    private DropPuzzleBattle _dropPuzzleBattle;
    private YushaBrain _yushaBrain;
    private EnemySpawner _enemySpawner;

    // IPuzzleFieldを購読する（VContainerで注入される）
    private IPuzzleField _puzzleField;

    // 現在のゲーム状態（Stateパターン）
    private IGameState _currentState;

    // IScoreWriterを購読する（VContainerで注入される）
    private IScoreWriter _scoreWriter;

    // ResultManagerを保持する（VContainerで注入される）
    private ResultManager _resultManager;

    // ==================================================
    // Inject: VContainerから依存を注入される
    // ==================================================
    [Inject]
    public void Construct(
    GameTimer gameTimer,
    DropPuzzleBattle dropPuzzleBattle,
    YushaBrain yushaBrain,
    EnemySpawner enemySpawner,
    IPuzzleField puzzleField,
    IScoreWriter scoreWriter,
    ResultManager resultManager)
    {
        _gameTimer = gameTimer;
        _dropPuzzleBattle = dropPuzzleBattle;
        _yushaBrain = yushaBrain;
        _enemySpawner = enemySpawner;
        _puzzleField = puzzleField;

        // IScoreWriterをInjectで受け取る
        _scoreWriter = scoreWriter;

        // ResultManagerをInjectで受け取る
        _resultManager = resultManager;
    }

    // ==================================================
    // Start: 初期状態をTitleStateに設定
    // ==================================================
    void Start()
    {
        // IPuzzleFieldのゲームオーバーSubjectを購読する
        _puzzleField.OnGameOver
            .Subscribe(_ => ChangeState(new GameOverState(this)));

        // GameTimerの時間切れSubjectを購読する
        _gameTimer.OnTimeUp
            .Subscribe(_ => ChangeState(new FinishState(this)));

        // 全パネルを非表示にしてからTitleStateに入る
        titlePanel.SetActive(false);
        manualPanel.SetActive(false);
        readyGoGroup.SetActive(false);
        gameOverPanel.SetActive(false);
        finishPanel.SetActive(false);

        // 最初の状態はタイトル画面
        ChangeState(new TitleState(this));
    }

    // ==================================================
    // Update: 現在の状態のUpdateを呼ぶ
    // ==================================================
    void Update()
    {
        // 現在の状態のUpdateに処理を委譲する
        _currentState?.Update();
    }

    // ==================================================
    // ChangeState: 状態を切り替える
    // 現在の状態のExit→新しい状態のEnterを呼ぶ
    // ==================================================
    public void ChangeState(IGameState newState)
    {
        // 現在の状態のExit処理を呼ぶ
        _currentState?.Exit();

        // 新しい状態に切り替えてEnter処理を呼ぶ
        _currentState = newState;
        _currentState.Enter();
    }

    // ==================================================
    // パネル表示切替メソッド群
    // 各StateクラスのEnter・Exitから呼ばれる
    // ==================================================
    public void ShowTitlePanel(bool show) => titlePanel.SetActive(show);
    public void ShowManualPanel(bool show) => manualPanel.SetActive(show);
    public void ShowReadyGoPanel(bool show) => readyGoGroup.SetActive(show);
    public void ShowGameOverPanel(bool show) => gameOverPanel.SetActive(show);
    public void ShowFinishPanel(bool show) => finishPanel.SetActive(show);

    // キルカウント・タイマーの表示切替
    public void ShowInGameUI(bool show)
    {
        killCountObject?.SetActive(show);
        timerTextObject?.SetActive(show);
    }

    // ==================================================
    // StartCountdown: カウントダウンを開始する
    // CountdownStateのEnterから呼ばれる
    // ==================================================
    public void StartCountdown()
    {
        StartCoroutine(CountdownCoroutine());
    }

    // ==================================================
    // カウントダウン → GO! → PlayingStateへ遷移
    // Time.timeScale = 0 中でも動くWaitForSecondsRealtimeを使用
    // ==================================================
    IEnumerator CountdownCoroutine()
    {
        Time.timeScale = 0f; // カウントダウン中は止める

        countdownText.text = "3";
        yield return new WaitForSecondsRealtime(1f);
        countdownText.text = "2";
        yield return new WaitForSecondsRealtime(1f);
        countdownText.text = "1";
        yield return new WaitForSecondsRealtime(1f);
        countdownText.text = "GO!";
        yield return new WaitForSecondsRealtime(0.8f);

        // カウントダウン完了→PlayingStateへ遷移
        ChangeState(new PlayingState(this));
    }

    // ==================================================
    // StartGameTimer: タイマーを開始する
    // PlayingStateのEnterから呼ばれる
    // ==================================================
    public void StartGameTimer()
    {
        _gameTimer?.StartTimer();
    }

    // ==================================================
    // StopYushaCameraShake: 勇者のカメラシェイクを止める
    // GameOverState・FinishStateから呼ぶ
    // ==================================================
    public void StopYushaCameraShake()
    {
        _yushaBrain?.StopCameraShake();
    }

    // ==================================================
    // GetResultManager: ResultManagerを取得する
    // ResultStateから呼ぶ（Instance直接参照をやめる）
    // ==================================================
    public ResultManager GetResultManager()
    {
        return _resultManager;
    }

    // ==================================================
    // RestartFromCountdown: もう一度プレイ
    // ResultStateから呼ばれる
    // ==================================================
    public void RestartFromCountdown()
    {
        // 各システムをリセット
        _dropPuzzleBattle?.ResetGame();
        _gameTimer?.ResetTimer();
        _yushaBrain?.ResetPosition();
        _enemySpawner?.ResetEnemies();

        // スコアをリセット
        _scoreWriter?.ResetScore();

        // インゲームUIを再表示
        ShowInGameUI(true);

        // カウントダウン状態へ遷移
        ChangeState(new CountdownState(this));
    }

    // ==================================================
    // GoToTitle: タイトルへ戻る
    // ResultStateから呼ばれる
    // ==================================================
    public void GoToTitle()
    {
        // 各システムをリセット
        _dropPuzzleBattle?.ResetGame();
        _gameTimer?.ResetTimer();
        _yushaBrain?.ResetPosition();
        _enemySpawner?.ResetEnemies();

        // スコアをリセット
        _scoreWriter?.ResetScore();

        // インゲームUIを再表示
        ShowInGameUI(true);

        // タイトル状態へ遷移
        ChangeState(new TitleState(this));
    }
}