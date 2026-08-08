using UnityEngine;

/// <summary>
/// RankingSubmitState.cs
/// オンラインランキング登録画面の状態
/// 
/// - Enter：名前入力パネルを表示する
/// - Update：送信・キャンセルの入力はRankingSubmitUIが担当する
/// - Exit：名前入力パネルを非表示にする
/// - スコア送信はRankingSubmitUIから行う
///   → StateはUIの表示切替だけを担当する（責務分離）
/// </summary>
public class RankingSubmitState : IGameState
{
    private readonly GameFlowManager _gameFlowManager;

    public RankingSubmitState(GameFlowManager gameFlowManager)
    {
        _gameFlowManager = gameFlowManager;
    }

    // ==================================================
    // Enter: 名前入力パネルを表示する
    // ==================================================
    public void Enter()
    {
        Time.timeScale = 0f;
        _gameFlowManager.ShowRankingSubmitPanel(true);
    }

    // ==================================================
    // Update: 入力はRankingSubmitUIが担当するため何もしない
    // ==================================================
    public void Update()
    {
    }

    // ==================================================
    // Exit: 名前入力パネルを非表示にする
    // ==================================================
    public void Exit()
    {
        _gameFlowManager.ShowRankingSubmitPanel(false);
    }
}