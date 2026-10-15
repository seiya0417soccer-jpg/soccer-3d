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
/// - 次のピースでも同じ処理を繰り返す
///
/// 特殊ブロックの戦略は扱わず、基本的な配置判断を行う。
/// </summary>
public sealed class JevPuzzleAutoPlayer : MonoBehaviour
{
    [Header("自動プレイ設定")]

    [SerializeField]
    [Tooltip("自動プレイの開始・停止に使用するキー")]
    private KeyCode _toggleKey = KeyCode.J;

    [Header("Jev自動プレイ設定")]

    [SerializeField]
    private JevAutoPlaySettingsSO _settings;

    // 判断を開始できない場合の再確認間隔
    private const int IdleDelayMilliseconds = 250;

    // 1手を実行した後、次の判断までに空ける時間
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

    private JevClient _jevClient;
    private PlacementCandidateGenerator _candidateGenerator;

    private CancellationToken _destroyToken;

    // 自動プレイを継続するか
    private bool _isRunning;

    // 前の非同期処理が終了しているか
    // API応答待ちの処理が重複するのを防ぐ
    private bool _loopActive;

    // UIから参照する自動プレイの状態
    public JevAutoPlayStatus Status { get; private set; }
        = JevAutoPlayStatus.Idle;

    // 自動プレイが進行中か
    public bool IsRunning => _isRunning;

    // 停止要求後も含め、処理が完全に終了していないか
    public bool IsBusy => _loopActive;

    // 状態が変わったときにUIへ通知する
    public event Action<JevAutoPlayStatus> StatusChanged;

    [Inject]
    public void Construct(IPuzzleAutomationPort puzzleAutomationPort)
    {
        _puzzleAutomationPort = puzzleAutomationPort;
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

    private void Update()
    {
        // Jキーによる従来の開始・停止操作を維持する
        if (!Input.GetKeyDown(_toggleKey))
        {
            return;
        }

        if (_isRunning)
        {
            StopAutoPlay();
        }
        else
        {
            StartAutoPlay();
        }
    }

    /// <summary>
    /// 自動プレイを開始する。
    /// UIからも呼び出せる。
    /// </summary>
    public void StartAutoPlay()
    {
        // 製品版からの開始要求は受け付けない
        if (!IsDevelopmentEnvironment)
        {
            return;
        }

        if (_isRunning)
        {
            return;
        }

        // 前回の非同期処理が終了するまでは再開しない
        if (_loopActive)
        {
            Debug.LogWarning(
                "[Jev AutoPlayer] 前回の判断処理が終了するまで待ってください。");

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

        _loopActive = true;
        _isRunning = true;

        SetStatus(JevAutoPlayStatus.WaitingForBoard);

        Debug.Log("[Jev AutoPlayer] 自動プレイ開始");

        RunLoopAsync().Forget();
    }

    /// <summary>
    /// 自動プレイの停止を要求する。
    /// API判断中なら結果を適用せず、移動中なら現在の1手の終了を待つ。
    /// </summary>
    public void StopAutoPlay()
    {
        if (!_isRunning)
        {
            return;
        }

        _isRunning = false;

        // 実行中の処理が残っている場合は、その終了を待つ
        SetStatus(
            _loopActive
                ? JevAutoPlayStatus.Stopping
                : JevAutoPlayStatus.Stopped);

        Debug.Log("[Jev AutoPlayer] 停止要求");
    }

    /// <summary>
    /// 自動プレイのメインループ。
    /// </summary>
    private async UniTaskVoid RunLoopAsync()
    {
        try
        {
            while (_isRunning)
            {
                SetStatus(JevAutoPlayStatus.WaitingForBoard);

                // 盤面情報を取得し、通常操作を停止する
                if (!_puzzleAutomationPort.TryBeginDecision(
                    out PuzzleAutomationSnapshot snapshot))
                {
                    await UniTask.Delay(
                        IdleDelayMilliseconds,
                        cancellationToken: _destroyToken);

                    continue;
                }

                try
                {
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

                    // 1ピースにつき原則1回だけ問い合わせる
                    JevDecision decision =
                        await _jevClient.SendChoiceRequestAsync(
                            state,
                            Instructions,
                            choices);

                    // 停止要求後に古い判断結果を実行しない
                    if (!_isRunning || _destroyToken.IsCancellationRequested)
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

                    // 移動・落下・盤面処理が完了するまで待つ
                    SetStatus(JevAutoPlayStatus.Executing);

                    bool applied =
                        await _puzzleAutomationPort.ExecutePlacementAsync(
                            selectedCandidate,
                            _destroyToken);

                    if (!applied)
                    {
                        SetStatus(JevAutoPlayStatus.Error);

                        Debug.LogWarning(
                            "[Jev AutoPlayer] 配置を実行できなかったため停止します: " +
                            selectedCandidate.Id);

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
                    // 成功・失敗のどちらでも判断中の状態を解除する
                    _puzzleAutomationPort.EndDecision();
                }

                // 次のピースに移るまで少し待つ
                if (_isRunning)
                {
                    SetStatus(JevAutoPlayStatus.WaitingForBoard);

                    await UniTask.Delay(
                        BetweenMovesMilliseconds,
                        cancellationToken: _destroyToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // オブジェクト破棄時などのキャンセルは正常終了として扱う
        }
        catch (Exception exception)
        {
            SetStatus(JevAutoPlayStatus.Error);
            Debug.LogException(exception);
        }
        finally
        {
            // 予期しない終了時にも通常操作への復帰を試みる
            _puzzleAutomationPort?.EndDecision();

            _isRunning = false;
            _loopActive = false;

            // エラー状態は維持し、通常の終了だけ停止状態にする
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
        // シーン終了時などに自動プレイを停止する
        _isRunning = false;
        _puzzleAutomationPort?.EndDecision();
    }
}