using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using VContainer;

/// <summary>
/// ゲーム全体のフロー管理。
///
/// - Stateパターンで状態を管理する
/// - 依存クラスはVContainerでInjectする
/// - パズル側・タイマーのイベントをR3で購読する
/// - リセット処理をResetAllSystems()に一本化する
/// - ゲーム終了をIGameLifecycleEvents経由で外部へ通知する
///
/// 画面遷移の流れ：
/// TitleState → ManualState → CountdownState → PlayingState
/// PlayingState → GameOverState or FinishState → ResultState
/// ResultState → CountdownState or TitleState or RankingSubmitState
/// RankingSubmitState → RankingViewState → TitleState
/// </summary>
public class GameFlowManager : MonoBehaviour, IGameLifecycleEvents
{
    [Header("Panels")]
    [SerializeField] private GameObject _titlePanel;
    [SerializeField] private GameObject _manualPanel;
    [SerializeField] private GameObject _readyGoGroup;
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private GameObject _finishPanel;
    [SerializeField] private GameObject _rankingSubmitPanel;
    [SerializeField] private GameObject _rankingViewPanel;

    [Header("ReadyGo")]
    [SerializeField] private TextMeshProUGUI _countdownText;

    [Header("UI")]
    [SerializeField] private GameObject _killCountObject;
    [SerializeField] private GameObject _timerTextObject;

    // VContainerでDI注入される依存クラス
    private IPuzzleField _puzzleField;
    private IBattleField _battleField;
    private ICameraShakeable _cameraShakeable;
    private GameTimer _gameTimer;
    private EnemySpawner _enemySpawner;
    private IScoreWriter _scoreWriter;
    private ResultManager _resultManager;
    private RankingSubmitUI _rankingSubmitUI;
    private RankingView _rankingView;

    // 現在のゲーム状態
    private IGameState _currentState;

    // ゲーム終了を外部の機能へ通知する
    public event Action GameEnded;

    // 現在の状態が実際のゲームプレイ中かどうか
    public bool IsGameActive => _currentState is PlayingState;

    // ==================================================
    // Inject: VContainerから依存を注入する
    // ==================================================
    [Inject]
    public void Construct(
        IPuzzleField puzzleField,
        IBattleField battleField,
        ICameraShakeable cameraShakeable,
        GameTimer gameTimer,
        EnemySpawner enemySpawner,
        IScoreWriter scoreWriter,
        ResultManager resultManager,
        RankingSubmitUI rankingSubmitUI,
        RankingView rankingView)
    {
        _puzzleField = puzzleField;
        _battleField = battleField;
        _cameraShakeable = cameraShakeable;
        _gameTimer = gameTimer;
        _enemySpawner = enemySpawner;
        _scoreWriter = scoreWriter;
        _resultManager = resultManager;
        _rankingSubmitUI = rankingSubmitUI;
        _rankingView = rankingView;
    }

    // ==================================================
    // Start: 初期状態をTitleStateに設定する
    // ==================================================
    private void Start()
    {
        // パズルのゲームオーバーを監視する
        _puzzleField.OnGameOver
            .Subscribe(_ => ChangeState(new GameOverState(this)))
            .AddTo(this);

        // 制限時間の終了を監視する
        _gameTimer.OnTimeUp
            .Subscribe(_ => ChangeState(new FinishState(this)))
            .AddTo(this);

        // 全パネルを非表示にしてからTitleStateに入る
        _titlePanel.SetActive(false);
        _manualPanel.SetActive(false);
        _readyGoGroup.SetActive(false);
        _gameOverPanel.SetActive(false);
        _finishPanel.SetActive(false);
        _rankingSubmitPanel?.SetActive(false);
        _rankingViewPanel?.SetActive(false);

        // 最初の状態はタイトル画面
        ChangeState(new TitleState(this));
    }

    // ==================================================
    // Update: 現在の状態のUpdateを呼ぶ
    // ==================================================
    private void Update()
    {
        _currentState?.Update();
    }

    // ==================================================
    // ChangeState: 状態を切り替える
    // ゲーム終了状態への遷移時には外部へ通知する
    // ==================================================
    public void ChangeState(IGameState newState)
    {
        // ゲームオーバーまたは制限時間終了への遷移か確認する
        bool isGameEnded =
            newState is GameOverState ||
            newState is FinishState;

        // 現在の状態のExit処理を呼ぶ
        _currentState?.Exit();

        // 新しい状態へ切り替える
        _currentState = newState;

        // 終了通知を必要とする機能へ知らせる
        if (isGameEnded)
        {
            GameEnded?.Invoke();
        }

        // 新しい状態のEnter処理を呼ぶ
        _currentState.Enter();
    }

    // ==================================================
    // パネル表示切替メソッド群
    // ==================================================
    public void ShowTitlePanel(bool show) =>
        _titlePanel.SetActive(show);

    public void ShowManualPanel(bool show) =>
        _manualPanel.SetActive(show);

    public void ShowReadyGoPanel(bool show) =>
        _readyGoGroup.SetActive(show);

    public void ShowGameOverPanel(bool show) =>
        _gameOverPanel.SetActive(show);

    public void ShowFinishPanel(bool show) =>
        _finishPanel.SetActive(show);

    public void ShowRankingSubmitPanel(bool show) =>
        _rankingSubmitPanel?.SetActive(show);

    public void ShowRankingViewPanel(bool show) =>
        _rankingViewPanel?.SetActive(show);

    // ==================================================
    // キルカウント・タイマーの表示切替
    // ==================================================
    public void ShowInGameUI(bool show)
    {
        _killCountObject?.SetActive(show);
        _timerTextObject?.SetActive(show);
    }

    // ==================================================
    // StartCountdown: カウントダウンを開始する
    // ==================================================
    public void StartCountdown()
    {
        StartCoroutine(CountdownCoroutine());
    }

    // ==================================================
    // CountdownCoroutine: カウントダウンからゲーム開始へ
    // Time.timeScale = 0中も進むRealtimeを使用する
    // ==================================================
    private IEnumerator CountdownCoroutine()
    {
        // カウントダウン中はゲームを止める
        Time.timeScale = 0f;

        _countdownText.text = "3";
        yield return new WaitForSecondsRealtime(1f);

        _countdownText.text = "2";
        yield return new WaitForSecondsRealtime(1f);

        _countdownText.text = "1";
        yield return new WaitForSecondsRealtime(1f);

        _countdownText.text = "GO!";
        yield return new WaitForSecondsRealtime(0.8f);

        // カウントダウン完了後、PlayingStateへ遷移する
        ChangeState(new PlayingState(this));
    }

    // ==================================================
    // StartGameTimer: タイマーを開始する
    // ==================================================
    public void StartGameTimer()
    {
        _gameTimer?.StartTimer();
    }

    // ==================================================
    // StopYushaCameraShake: カメラシェイクを停止する
    // ==================================================
    public void StopYushaCameraShake()
    {
        _cameraShakeable?.StopCameraShake();
    }

    // ==================================================
    // EnableYushaCameraShake: カメラシェイクの禁止を解除する
    // ==================================================
    public void EnableYushaCameraShake()
    {
        _cameraShakeable?.EnableCameraShake();
    }

    // ==================================================
    // GetResultManager: ResultManagerを取得する
    // ==================================================
    public ResultManager GetResultManager()
    {
        return _resultManager;
    }

    // ==================================================
    // GoToRankingSubmit: ランキング登録画面へ遷移する
    // ==================================================
    public void GoToRankingSubmit()
    {
        ChangeState(new RankingSubmitState(this, _rankingSubmitUI));
    }

    // ==================================================
    // GoToRankingView: ランキング表示画面へ遷移する
    // ==================================================
    public void GoToRankingView()
    {
        ChangeState(new RankingViewState(this));
    }

    // ==================================================
    // GoToResult: リザルト画面へ遷移する
    // FinishState・GameOverState・RankingSubmitStateから呼ぶ
    // ==================================================
    public void GoToResult()
    {
        ChangeState(new ResultState(this));
    }

    // ==================================================
    // LoadRanking: ランキングデータを取得して表示する
    // ==================================================
    public void LoadRanking()
    {
        _rankingView?.LoadAndDisplay(
            this.GetCancellationTokenOnDestroy());
    }

    // ==================================================
    // ResetAllSystems: 全システムをリセットする
    // ==================================================
    private void ResetAllSystems()
    {
        // パズルフィールドをリセットする
        _puzzleField?.ResetGame();

        // タイマーをリセットする
        _gameTimer?.ResetTimer();

        // 勇者を初期位置に戻す
        _battleField?.ResetPosition();

        // 敵をリセットして再スポーンする
        _enemySpawner?.ResetEnemies();

        // カメラシェイクの禁止を解除する
        EnableYushaCameraShake();

        // スコアをリセットする
        _scoreWriter?.ResetScore();

        // インゲームUIを再表示する
        ShowInGameUI(true);
    }

    // ==================================================
    // RestartFromCountdown: もう一度プレイ
    // ==================================================
    public void RestartFromCountdown()
    {
        // 全システムをリセットしてカウントダウンへ
        ResetAllSystems();
        ChangeState(new CountdownState(this));
    }

    // ==================================================
    // GoToTitle: タイトルへ戻る
    // ==================================================
    public void GoToTitle()
    {
        // 全システムをリセットしてタイトルへ
        ResetAllSystems();
        ChangeState(new TitleState(this));
    }
}