using R3;
using System;
using UnityEngine;

/// <summary>
/// RankingSubmitState.cs
/// オンラインランキング登録画面の状態
/// 
/// - Enter：名前入力パネルを表示・UIをリセット・Observableを購読する
/// - Update：3回失敗後にEnterキーでリザルトへ遷移する
/// - Exit：名前入力パネルを非表示・購読を解除する
/// - RankingSubmitUIのObservableを購読して遷移を判断する
///   → UIはGameFlowManagerを知らなくていい設計にした（疎結合）
///   → IPuzzleField・IBattleFieldと同じ思想で一貫している
/// - 最大3回失敗時はEnterキー待ちに移行してリザルトへ戻る
///   → オンラインランキングを諦めて自己ベストで記録する
/// </summary>
public class RankingSubmitState : IGameState
{
    private readonly GameFlowManager _gameFlowManager;
    private readonly RankingSubmitUI _rankingSubmitUI;

    // 購読を管理するDisposable
    private IDisposable _submitDisposable;
    private IDisposable _cancelDisposable;
    private IDisposable _maxRetryDisposable;

    // 3回失敗後のEnter待ちフラグ
    private bool _waitingForEnterAfterMaxRetry = false;

    public RankingSubmitState(GameFlowManager gameFlowManager, RankingSubmitUI rankingSubmitUI)
    {
        _gameFlowManager = gameFlowManager;
        _rankingSubmitUI = rankingSubmitUI;
    }

    // ==================================================
    // Enter: 名前入力パネルを表示する
    // UIをリセットしてObservableを購読する
    // ==================================================
    public void Enter()
    {
        Time.timeScale = 0f;
        _waitingForEnterAfterMaxRetry = false;
        _gameFlowManager.ShowRankingSubmitPanel(true);

        // UIをリセットする（前回の入力・状態を消す）
        _rankingSubmitUI.ResetUI();

        // 送信完了を購読してRankingViewStateへ遷移する
        _submitDisposable = _rankingSubmitUI.OnSubmitCompleted
            .Subscribe(_ => _gameFlowManager.GoToRankingView());

        // キャンセルを購読してタイトルへ遷移する
        _cancelDisposable = _rankingSubmitUI.OnCancelled
            .Subscribe(_ => _gameFlowManager.GoToTitle());

        // 最大リトライ到達を購読してEnter待ちに移行する
        // リザルトへの遷移はUpdate()のEnterキー入力で行う
        _maxRetryDisposable = _rankingSubmitUI.OnMaxRetryReached
            .Subscribe(_ => _waitingForEnterAfterMaxRetry = true);
    }

    // ==================================================
    // Update: 3回失敗後にEnterキーでリザルトへ遷移する
    // 通常時は何もしない（送信・キャンセルはObservableで通知される）
    // ==================================================
    public void Update()
    {
        // 3回失敗後のEnter待ち状態の時だけ入力を受け付ける
        if (!_waitingForEnterAfterMaxRetry) return;

        if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Return) ||
            UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.KeypadEnter))
        {
            // オンラインランキングを諦めてリザルトへ戻る
            // 自己ベストはResultManagerのPlayerPrefsで管理されている
            _gameFlowManager.GoToResult();
        }
    }

    // ==================================================
    // Exit: 名前入力パネルを非表示にする
    // 購読を解除してメモリリークを防ぐ
    // ==================================================
    public void Exit()
    {
        _gameFlowManager.ShowRankingSubmitPanel(false);

        // 購読を解除する（メモリリーク防止）
        _submitDisposable?.Dispose();
        _cancelDisposable?.Dispose();
        _maxRetryDisposable?.Dispose();
    }
}