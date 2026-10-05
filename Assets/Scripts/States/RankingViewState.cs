using UnityEngine;

/// <summary>
/// RankingViewState.cs
/// オンラインランキング表示画面の状態
/// 
/// - Enter：ランキングパネルを表示してTOP5を取得・表示する
/// - Update：Enterキーでタイトルへ戻る
/// - Exit：ランキングパネルを非表示にする
/// </summary>
public class RankingViewState : IGameState
{
    private readonly GameFlowManager _gameFlowManager;

    public RankingViewState(GameFlowManager gameFlowManager)
    {
        _gameFlowManager = gameFlowManager;
    }

    // ==================================================
    // Enter: ランキングパネルを表示する
    // データ取得はRankingViewが担当する
    // ==================================================
    public void Enter()
    {
        _gameFlowManager.ShowRankingViewPanel(true);
        _gameFlowManager.LoadRanking();
    }

    // ==================================================
    // Update: Enterキーでタイトルへ戻る
    // ==================================================
    public void Update()
    {
        if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Return) ||
            UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.KeypadEnter))
        {
            _gameFlowManager.GoToTitle();
        }
    }

    // ==================================================
    // Exit: ランキングパネルを非表示にする
    // ==================================================
    public void Exit()
    {
        _gameFlowManager.ShowRankingViewPanel(false);
    }
}