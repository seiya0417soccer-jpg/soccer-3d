using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// ゲームバランス検証用の自動プレイヤー。
///
/// 役割：
/// - 現在の盤面情報を取得する
/// - 合法な配置候補を生成する
/// - Jevに配置候補を選択させる
/// - 選択結果をパズルに反映する
/// - 盤面処理完了後、次のピースでも同じ処理を繰り返す
///
/// ゲーム終了時は自動プレイを停止し、特殊ブロックの戦略は扱わない。
/// </summary>
public sealed class JevPuzzleAutoPlayer : MonoBehaviour
{
    [Header("自動プレイ設定")]

    [Header("Jev自動プレイ設定")]

    [SerializeField]
    private JevAutoPlaySettingsSO _settings;

    // 判断を開始できない場合の再確認間隔
    private const int IdleDelayMilliseconds = 250;

    // 1手の実行後、次の判断までに空ける時間
    private const int BetweenMovesMilliseconds = 150;

    // Unity Editorまたは開発用ビルドかどうか
    private static bool IsDevelopmentEnvironment =>
        Application.isEditor || Debug.isDebugBuild;

    // Jevへの判断指示
    private const string Instructions =
        "あなたはゲームバランス検証用の落下パズル自動プレイヤーです。" +
        "状態情報に記載された合法な配置候補から、" +
        "穴を増やしにくく、列の高さを抑え、" +
        "完成行数を増やせる配置を選んでください。" +
        "特殊ブロックの効果は判断対象外とします。" +
        "選択肢として与えられた候補IDから1つだけ選んでください。";

    private IPuzzleAutomationPort _puzzleAutomationPort;
    private IGameLifecycleEvents _gameLifecycleEvents;

    private JevClient _jevClient;
    private PlacementCandidateGenerator _candidateGenerator;

    private CancellationToken _destroyToken;

    // 現在の自動プレイをキャンセルするためのトークン
    private CancellationTokenSource _runCancellation;

    // 自動プレイを継続するか
    private bool _isRunning;

    // 前の非同期処理が終了しているか
    private bool _loopActive;

    // UIから参照する自動プレイの状態
    public JevAutoPlayStatus Status { get; private set; }
        = JevAutoPlayStatus.Idle;

    // 自動プレイが進行中か
    public bool IsRunning => _isRunning;

    // 停止要求後も含め、処理が完全に終了していないか
    public bool IsBusy => _loopActive;

    // 状態変化をUIへ通知する
    public event Action<JevAutoPlayStatus> StatusChanged;

    [Inject]
    public void Construct(
        IPuzzleAutomationPort puzzleAutomationPort,
        IGameLifecycleEvents gameLifecycleEvents)
    {
        _puzzleAutomationPort = puzzleAutomationPort;
        _gameLifecycleEvents = gameLifecycleEvents;

        // ゲーム終了を検知して自動プレイを強制停止する
        _gameLifecycleEvents.GameEnded += OnGameEnded;
    }

    private void Awake()
    {
        // 製品版では自動プレイ機能を無効にする
        if (!IsDevelopmentEnvironment)
        {
            enabled = false;
            return;
        }

        _jevClient = new JevClient();
        _candidateGenerator = new PlacementCandidateGenerator();
        _destroyToken = this.GetCancellationTokenOnDestroy();
    }

    /// <summary>
    /// 自動プレイを開始する。
    /// 実際のゲームプレイ中だけ開始できる。
    /// </summary>
    public void StartAutoPlay()
    {
        if (!IsDevelopmentEnvironment)
        {
            return;
        }

        if (_isRunning)
        {
            return;
        }

        // 前回の非同期処理が完全に終わるまでは再開しない
        if (_loopActive)
        {
            Debug.LogWarning(
                "[Jev AutoPlayer] 前回の判断処理が終了するまで待ってください。");

            return;
        }

        // タイトル・カウントダウン・リザルト画面では開始しない
        if (_gameLifecycleEvents == null ||
            !_gameLifecycleEvents.IsGameActive)
        {
            Debug.LogWarning(
                "[Jev AutoPlayer] ゲームプレイ中のみ開始できます。");

            return;
        }

        if (_puzzleAutomationPort == null)
        {
            SetStatus(JevAutoPlayStatus.Error);

            Debug.LogError(
                "[Jev AutoPlayer] IPuzzleAutomationPortが注入されていません。");

            return;
        }

        if (_settings == null)
        {
            SetStatus(JevAutoPlayStatus.Error);

            Debug.LogError(
                "[Jev AutoPlayer] JevAutoPlaySettingsSOが設定されていません。");

            return;
        }

        // 自動プレイ専用の落下速度を適用する
        _puzzleAutomationPort.SetAutomationFallStepInterval(
            _settings.FallStepInterval);

        // 今回の実行専用のキャンセルトークンを作成する
        _runCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(_destroyToken);

        CancellationToken runToken = _runCancellation.Token;

        _loopActive = true;
        _isRunning = true;

        SetStatus(JevAutoPlayStatus.WaitingForBoard);

        Debug.Log("[Jev AutoPlayer] 自動プレイ開始");

        RunLoopAsync(runToken).Forget();
    }

    /// <summary>
    /// 自動プレイの停止を要求する。
    /// 判断中なら通信をキャンセルし、移動中なら現在の1手を完了して停止する。
    /// </summary>
    public void StopAutoPlay()
    {
        if (!_isRunning)
        {
            return;
        }

        _isRunning = false;

        // 移動実行中はその1手を完了させる。
        // それ以外では待機や通信をキャンセルする。
        if (Status != JevAutoPlayStatus.Executing)
        {
            _runCancellation?.Cancel();
        }

        SetStatus(
            _loopActive
                ? JevAutoPlayStatus.Stopping
                : JevAutoPlayStatus.Stopped);

        Debug.Log("[Jev AutoPlayer] 停止要求");
    }

    /// <summary>
    /// ゲームオーバーまたは制限時間終了時に自動プレイを強制停止する。
    /// </summary>
    private void OnGameEnded()
    {
        _isRunning = false;

        // ゲーム終了時は移動中でもキャンセルする
        _runCancellation?.Cancel();

        SetStatus(
            _loopActive
                ? JevAutoPlayStatus.Stopping
                : JevAutoPlayStatus.Stopped);

        Debug.Log("[Jev AutoPlayer] ゲーム終了により自動プレイを停止しました。");
    }

    /// <summary>
    /// 自動プレイのメインループ。
    /// </summary>
    private async UniTaskVoid RunLoopAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            while (_isRunning)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // ゲーム終了状態なら次の判断を開始しない
                if (_gameLifecycleEvents == null ||
                    !_gameLifecycleEvents.IsGameActive)
                {
                    break;
                }

                SetStatus(JevAutoPlayStatus.WaitingForBoard);

                // 盤面情報を取得し、通常操作を停止する
                if (!_puzzleAutomationPort.TryBeginDecision(
                    out PuzzleAutomationSnapshot snapshot))
                {
                    await UniTask.Delay(
                        IdleDelayMilliseconds,
                        cancellationToken: cancellationToken);

                    continue;
                }

                try
                {
                    // ゲーム終了後に判断を開始しない
                    if (!_isRunning ||
                        !_gameLifecycleEvents.IsGameActive)
                    {
                        break;
                    }

                    SetStatus(JevAutoPlayStatus.Judging);

                    // 合法な配置候補を生成する
                    List<PuzzlePlacementCandidate> candidates =
                        _candidateGenerator.Generate(snapshot);

                    if (candidates.Count == 0)
                    {
                        SetStatus(JevAutoPlayStatus.Error);

                        Debug.LogWarning(
                            "[Jev AutoPlayer] 合法な配置候補がありません。");

                        _isRunning = false;
                        break;
                    }

                    // Jevに渡す選択肢は候補IDに統一する
                    string[] choices = new string[candidates.Count];

                    for (int i = 0; i < candidates.Count; i++)
                    {
                        choices[i] = candidates[i].Id;
                    }

                    // 盤面と候補の評価情報をJevに渡す
                    string state = BuildStateDescription(
                        snapshot,
                        candidates);

                    // キャンセル可能なAPIリクエストを行う
                    // 1ピースにつき原則1回だけ問い合わせる
                    JevDecision decision =
                        await _jevClient.SendChoiceRequestAsync(
                            state,
                            Instructions,
                            cancellationToken,
                            choices);

                    cancellationToken.ThrowIfCancellationRequested();

                    // 停止要求後やゲーム終了後に古い判断結果を実行しない
                    if (!_isRunning ||
                        !_gameLifecycleEvents.IsGameActive)
                    {
                        break;
                    }

                    // API通信・レスポンス解析に失敗した場合は停止する
                    if (decision == null ||
                        string.IsNullOrWhiteSpace(decision.choice))
                    {
                        SetStatus(JevAutoPlayStatus.Error);

                        Debug.LogWarning(
                            "[Jev AutoPlayer] 判断に失敗したため自動プレイを停止します。");

                        _isRunning = false;
                        break;
                    }

                    // Jevの回答IDに対応する配置候補を取得する
                    PuzzlePlacementCandidate selectedCandidate =
                        candidates.Find(
                            candidate => candidate.Id == decision.choice);

                    if (selectedCandidate == null)
                    {
                        SetStatus(JevAutoPlayStatus.Error);

                        Debug.LogWarning(
                            "[Jev AutoPlayer] 不明な候補IDが返されたため停止します: " +
                            decision.choice);

                        _isRunning = false;
                        break;
                    }

                    // 移動・落下・盤面処理を実行する
                    SetStatus(JevAutoPlayStatus.Executing);

                    bool applied =
                        await _puzzleAutomationPort.ExecutePlacementAsync(
                            selectedCandidate,
                            cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();

                    if (!applied)
                    {
                        // ゲーム終了が原因なら通常のエラー扱いにしない
                        if (_gameLifecycleEvents.IsGameActive)
                        {
                            SetStatus(JevAutoPlayStatus.Error);

                            Debug.LogWarning(
                                "[Jev AutoPlayer] 配置を実行できなかったため停止します: " +
                                selectedCandidate.Id);
                        }

                        _isRunning = false;
                        break;
                    }

                    Debug.Log(
                        $"[Jev AutoPlayer] " +
                        $"配置={selectedCandidate.Id}, " +
                        $"Confidence={decision.confidence:F2}");
                }
                finally
                {
                    // 成功・失敗・キャンセル時も通常操作への復帰を試みる
                    _puzzleAutomationPort.EndDecision();
                }

                // 次のピースに移るまで少し待つ
                if (_isRunning)
                {
                    await UniTask.Delay(
                        BetweenMovesMilliseconds,
                        cancellationToken: cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 手動停止・ゲーム終了・オブジェクト破棄によるキャンセル
        }
        catch (Exception exception)
        {
            SetStatus(JevAutoPlayStatus.Error);
            Debug.LogException(exception);
        }
        finally
        {
            // 予期しない終了時にも判断中の状態を解除する
            _puzzleAutomationPort?.EndDecision();

            _isRunning = false;

            // 今回の実行で使用したキャンセルトークンを破棄する
            _runCancellation?.Dispose();
            _runCancellation = null;

            _loopActive = false;

            // エラー状態は維持し、それ以外は停止済みにする
            if (Status != JevAutoPlayStatus.Error &&
                !_destroyToken.IsCancellationRequested)
            {
                SetStatus(JevAutoPlayStatus.Stopped);
            }
        }
    }

    /// <summary>
    /// 盤面情報と各配置候補の評価を1つの文章にまとめる。
    /// </summary>
    private static string BuildStateDescription(
        PuzzleAutomationSnapshot snapshot,
        List<PuzzlePlacementCandidate> candidates)
    {
        StringBuilder builder =
            new StringBuilder(snapshot.ToStateDescription());

        builder.AppendLine();
        builder.AppendLine("配置候補一覧：");

        foreach (PuzzlePlacementCandidate candidate in candidates)
        {
            builder.AppendLine(
                $"{candidate.ToChoiceDescription()}, " +
                $"基準X={candidate.AnchorX}, " +
                $"着地基準Y={candidate.LandingY}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// 状態が変わったときにUIへ通知する。
    /// </summary>
    private void SetStatus(JevAutoPlayStatus status)
    {
        if (Status == status)
        {
            return;
        }

        Status = status;
        StatusChanged?.Invoke(status);
    }

    private void OnDestroy()
    {
        // ゲーム終了イベントの購読を解除する
        if (_gameLifecycleEvents != null)
        {
            _gameLifecycleEvents.GameEnded -= OnGameEnded;
        }

        // シーン終了時などにも自動プレイを停止する
        _isRunning = false;
        _runCancellation?.Cancel();

        _puzzleAutomationPort?.EndDecision();
    }
}