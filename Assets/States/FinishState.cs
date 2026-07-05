/// <summary>
/// FinishState.cs
/// フィニッシュ（時間切れ）画面の状態
/// 
/// - Enter：フィニッシュパネルを表示・TimeScale=0
/// - Update：エンターキーでリザルトへ遷移
/// - Exit：フィニッシュパネルを非表示
/// </summary>
public class FinishState : IGameState
{
    // GameFlowManagerへの参照
    private readonly GameFlowManager _gameFlowManager;

    public FinishState(GameFlowManager gameFlowManager)
    {
        _gameFlowManager = gameFlowManager;
    }

    // ==================================================
    // Enter: フィニッシュ画面に入った時の処理
    // ==================================================
    public void Enter()
    {
        UnityEngine.Time.timeScale = 0f;
        _gameFlowManager.ShowFinishPanel(true);
        _gameFlowManager.ShowInGameUI(false); // キルカウント・タイマーを非表示
    }

    // ==================================================
    // Update: エンターキー待ち
    // ==================================================
    public void Update()
    {
        if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Return) ||
            UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.KeypadEnter))
        {
            // リザルト状態へ遷移
            _gameFlowManager.ChangeState(new ResultState(_gameFlowManager));
        }
    }

    // ==================================================
    // Exit: フィニッシュパネルを非表示
    // ==================================================
    public void Exit()
    {
        _gameFlowManager.ShowFinishPanel(false);
    }
}