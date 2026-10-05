/// <summary>
/// PlayingState.cs
/// ゲームプレイ中の状態
/// 
/// - Enter：ゲームを開始する（TimeScale=1）
/// - Update：特に何もしない（各クラスが自律的に動く）
/// - Exit：特に何もしない
/// </summary>
public class PlayingState : IGameState
{
    // GameFlowManagerへの参照
    private readonly GameFlowManager _gameFlowManager;

    public PlayingState(GameFlowManager gameFlowManager)
    {
        _gameFlowManager = gameFlowManager;
    }

    // ==================================================
    // Enter: ゲーム開始
    // ==================================================
    public void Enter()
    {
        UnityEngine.Time.timeScale = 1f; // ゲーム開始
        _gameFlowManager.StartGameTimer(); // タイマー開始
    }

    // ==================================================
    // Update: ゲーム中は各クラスが自律的に動くため何もしない
    // ==================================================
    public void Update()
    {
        // GameTimer・YushaBrain・DropPuzzleBattleが
        // それぞれのUpdateで動くため、ここでは何もしない
    }

    // ==================================================
    // Exit: 特に何もしない
    // ==================================================
    public void Exit()
    {
    }
}