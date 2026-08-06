using R3;
using System.Collections;
using TMPro;
using UnityEngine;
using VContainer;

/// <summary>
/// GameFlowManager.cs
/// ゲーム全体のフロー管理
/// 
/// - Stateパターンで状態を管理する
///   各状態クラスがEnter・Update・Exitを持ち、GameFlowManagerは
///   状態の切り替えだけを担当する（状態ごとの処理を分離）
/// - 依存クラスはVContainerでInjectする（Singleton不使用）
/// - パズル側・タイマーのイベントをR3のObservableで購読する
///   直接呼び出しをやめることでパズル・タイマーとの疎結合を実現した
/// - リセット処理をResetAllSystems()に一本化した
///   追加・変更があってもここだけ直せばよい（保守性・引き継ぎやすさの向上）
/// 
/// 画面遷移の流れ：
/// TitleState → ManualState → CountdownState → PlayingState
/// PlayingState → GameOverState or FinishState → ResultState
/// ResultState → CountdownState or TitleState
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject _titlePanel;    // タイトル画面
    [SerializeField] private GameObject _manualPanel;   // 操作説明画面
    [SerializeField] private GameObject _readyGoGroup;  // カウントダウン画面
    [SerializeField] private GameObject _gameOverPanel; // ゲームオーバー画面
    [SerializeField] private GameObject _finishPanel;   // フィニッシュ画面

    [Header("ReadyGo")]
    [SerializeField] private TextMeshProUGUI _countdownText; // 3,2,1,GO!を表示するTMPテキスト

    [Header("UI")]
    [SerializeField] private GameObject _killCountObject;  // キルカウント表示（終了時に非表示）
    [SerializeField] private GameObject _timerTextObject;  // タイマー表示（終了時に非表示）

    // VContainerでDI注入される依存クラス
    // 依存クラスはVContainerでInjectする（Singleton不使用）
    // IPuzzleField・IScoreWriterはInterface経由で注入し具体実装に依存しない設計にした
    // YushaBrain・EnemySpawner・GameTimer・ResultManagerは現状具体型で注入している
    // → IBattleField導入時にYushaBrainもInterface化する予定（次回改修リスト②）
    private IPuzzleField _puzzleField;
    private GameTimer _gameTimer;
    private YushaBrain _yushaBrain;
    private EnemySpawner _enemySpawner;
    private IScoreWriter _scoreWriter;
    private ResultManager _resultManager;

    // 現在のゲーム状態（Stateパターン）
    // IGameState経由で管理することで状態クラスの追加・変更が容易
    private IGameState _currentState;

    // ==================================================
    // Inject: VContainerから依存を注入される
    // DropPuzzleBattle具体型ではなくIPuzzleField経由で受け取る
    // → パズルの実装を差し替えてもGameFlowManagerは変更不要
    // ==================================================
    [Inject]
    public void Construct(
        IPuzzleField puzzleField,
        GameTimer gameTimer,
        YushaBrain yushaBrain,
        EnemySpawner enemySpawner,
        IScoreWriter scoreWriter,
        ResultManager resultManager)
    {
        _puzzleField = puzzleField;
        _gameTimer = gameTimer;
        _yushaBrain = yushaBrain;
        _enemySpawner = enemySpawner;
        _scoreWriter = scoreWriter;
        _resultManager = resultManager;
    }

    // ==================================================
    // Start: 初期状態をTitleStateに設定する
    // ==================================================
    void Start()
    {
        // IPuzzleFieldのゲームオーバーObservableを購読する
        // DropPuzzleBattleを直接知らずにゲームオーバーを検知できる（疎結合）
        // AddTo(this)でGameFlowManager破棄時に自動で購読解除する（メモリリーク防止）
        _puzzleField.OnGameOver
            .Subscribe(_ => ChangeState(new GameOverState(this)))
            .AddTo(this);

        // GameTimerの時間切れObservableを購読する
        // AddTo(this)でGameFlowManager破棄時に自動で購読解除する（メモリリーク防止）
        _gameTimer.OnTimeUp
            .Subscribe(_ => ChangeState(new FinishState(this)))
            .AddTo(this);

        // 全パネルを非表示にしてからTitleStateに入る
        _titlePanel.SetActive(false);
        _manualPanel.SetActive(false);
        _readyGoGroup.SetActive(false);
        _gameOverPanel.SetActive(false);
        _finishPanel.SetActive(false);

        // 最初の状態はタイトル画面
        ChangeState(new TitleState(this));
    }

    // ==================================================
    // Update: 現在の状態のUpdateを呼ぶ
    // 状態ごとの処理は各StateクラスのUpdateに委譲する
    // ==================================================
    void Update()
    {
        _currentState?.Update();
    }

    // ==================================================
    // ChangeState: 状態を切り替える
    // 現在の状態のExit → 新しい状態のEnterを呼ぶ
    // IGameState経由で管理するため状態クラスの追加時にここは変更不要
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
    // GameFlowManagerがパネルを一元管理することで
    // 各StateがUIの実体を知らなくて済む設計にした
    // ==================================================
    public void ShowTitlePanel(bool show) => _titlePanel.SetActive(show);
    public void ShowManualPanel(bool show) => _manualPanel.SetActive(show);
    public void ShowReadyGoPanel(bool show) => _readyGoGroup.SetActive(show);
    public void ShowGameOverPanel(bool show) => _gameOverPanel.SetActive(show);
    public void ShowFinishPanel(bool show) => _finishPanel.SetActive(show);

    // キルカウント・タイマーの表示切替
    // ゲーム終了時に非表示・再開時に再表示する
    public void ShowInGameUI(bool show)
    {
        _killCountObject?.SetActive(show);
        _timerTextObject?.SetActive(show);
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
    // CountdownCoroutine: カウントダウン → GO! → PlayingStateへ遷移
    // Time.timeScale = 0中でも動くWaitForSecondsRealtimeを使用する
    // ==================================================
    IEnumerator CountdownCoroutine()
    {
        Time.timeScale = 0f; // カウントダウン中はゲームを止める

        _countdownText.text = "3";
        yield return new WaitForSecondsRealtime(1f);
        _countdownText.text = "2";
        yield return new WaitForSecondsRealtime(1f);
        _countdownText.text = "1";
        yield return new WaitForSecondsRealtime(1f);
        _countdownText.text = "GO!";
        yield return new WaitForSecondsRealtime(0.8f);

        // カウントダウン完了 → PlayingStateへ遷移
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
    // ゲームオーバー・フィニッシュ時に呼ぶ
    // 敵を倒した直後に終了した場合に揺れっぱなしになるバグを防ぐ
    // ==================================================
    public void StopYushaCameraShake()
    {
        _yushaBrain?.StopCameraShake();
    }

    // ==================================================
    // EnableYushaCameraShake: 勇者のカメラシェイク禁止を解除する
    // ResetAllSystems()から呼ぶ
    // ==================================================
    public void EnableYushaCameraShake()
    {
        _yushaBrain?.EnableCameraShake();
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
    // ResetAllSystems: 全システムをリセットする（共通処理）
    // 
    // RestartFromCountdown・GoToTitleの両方から呼ぶ
    // リセット処理を1箇所にまとめることで
    // 「片方だけ直し忘れる」バグを防ぐ（保守性・引き継ぎやすさの向上）
    // 新しいリセット対象が増えてもここだけ追加すればよい（拡張性の向上）
    // ==================================================
    private void ResetAllSystems()
    {
        // パズルフィールドをリセット（IPuzzleField経由で具体型に依存しない）
        _puzzleField?.ResetGame();

        // タイマーをリセット
        _gameTimer?.ResetTimer();

        // 勇者を初期位置に戻す
        _yushaBrain?.ResetPosition();

        // 敵を全削除して再スポーン
        _enemySpawner?.ResetEnemies();

        // カメラシェイクの禁止を解除する
        // （前回ゲームオーバー・フィニッシュで禁止されている場合があるため）
        EnableYushaCameraShake();

        // スコアをリセット（IScoreWriter経由で具体型に依存しない）
        _scoreWriter?.ResetScore();

        // インゲームUIを再表示（キルカウント・タイマー）
        ShowInGameUI(true);
    }

    // ==================================================
    // RestartFromCountdown: もう一度プレイ
    // ResultStateから呼ばれる
    // ==================================================
    public void RestartFromCountdown()
    {
        // 全システムをリセットしてカウントダウンへ
        ResetAllSystems();
        ChangeState(new CountdownState(this));
    }

    // ==================================================
    // GoToTitle: タイトルへ戻る
    // ResultStateから呼ばれる
    // ==================================================
    public void GoToTitle()
    {
        // 全システムをリセットしてタイトルへ
        ResetAllSystems();
        ChangeState(new TitleState(this));
    }
}