/// <summary>
/// ManualState.cs
/// 操作説明画面の状態
/// 
/// - Enter：操作説明パネルを表示
/// - Update：エンターキーでカウントダウンへ遷移
/// - Exit：操作説明パネルを非表示
/// </summary>
public class ManualState : IGameState
{
    // GameFlowManagerへの参照（パネル操作・状態遷移に使う）
    private readonly GameFlowManager _gameFlowManager;

    public ManualState(GameFlowManager gameFlowManager)
    {
        _gameFlowManager = gameFlowManager;
    }

    // ==================================================
    // Enter: 操作説明画面に入った時の処理
    // ==================================================
    public void Enter()
    {
        _gameFlowManager.ShowManualPanel(true);
    }

    // ==================================================
    // Update: 毎フレームの入力受付
    // ==================================================
    public void Update()
    {
        if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Return) ||
            UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.KeypadEnter))
        {
            // カウントダウン状態へ遷移
            _gameFlowManager.ChangeState(new CountdownState(_gameFlowManager));
        }
    }

    // ==================================================
    // Exit: 操作説明画面から出る時の処理
    // ==================================================
    public void Exit()
    {
        _gameFlowManager.ShowManualPanel(false);
    }
}