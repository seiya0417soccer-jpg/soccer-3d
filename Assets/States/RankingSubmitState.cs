using R3;
using System;
using UnityEngine;

/// <summary>
/// RankingSubmitState.cs
/// オンラインランキング登録画面の状態
/// 
/// - Enter：名前入力パネルを表示・UIをリセット・Observableを購読する
/// - Update：何もしない（送信・キャンセルはRankingSubmitUIのObservableで通知される）
/// - Exit：名前入力パネルを非表示・購読を解除する
/// - RankingSubmitUIのObservableを購読して遷移を判断する
///   → UIはGameFlowManagerを知らなくていい設計にした（疎結合）
///   → IPuzzleField・IBattleFieldと同じ思想で一貫している
/// </summary>
public class RankingSubmitState : IGameState
{
    private readonly GameFlowManager _gameFlowManager;
    private readonly RankingSubmitUI _rankingSubmitUI;

    // 購読を管理するDisposable
    private IDisposable _submitDisposable;
    private IDisposable _cancelDisposable;

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
        _gameFlowManager.ShowRankingSubmitPanel(true);

        // UIをリセットする（前回の入力・状態を消す）
        _rankingSubmitUI.ResetUI();

        // 送信完了を購読してRankingViewStateへ遷移する
        _submitDisposable = _rankingSubmitUI.OnSubmitCompleted
            .Subscribe(_ => _gameFlowManager.GoToRankingView());

        // キャンセルを購読してタイトルへ遷移する
        _cancelDisposable = _rankingSubmitUI.OnCancelled
            .Subscribe(_ => _gameFlowManager.GoToTitle());
    }

    // ==================================================
    // Update: RankingSubmitUIのObservableで通知されるため何もしない
    // ==================================================
    public void Update()
    {
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
    }
}