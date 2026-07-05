/// <summary>
/// TitleState.cs
/// タイトル画面の状態
/// 
/// - Enter：タイトルパネルを表示
/// - Update：エンターキーで操作説明へ遷移
/// - Exit：タイトルパネルを非表示
/// </summary>
public class TitleState : IGameState
{
    // GameFlowManagerへの参照（パネル操作・状態遷移に使う）
    private readonly GameFlowManager _gameFlowManager;

    public TitleState(GameFlowManager gameFlowManager)
    {
        _gameFlowManager = gameFlowManager;
    }

    // ==================================================
    // Enter: タイトル画面に入った時の処理
    // ==================================================
    public void Enter()
    {
        _gameFlowManager.ShowTitlePanel(true);
        UnityEngine.Time.timeScale = 0f; // タイトル中はゲームを止める
    }

    // ==================================================
    // Update: 毎フレームの入力受付
    // ==================================================
    public void Update()
    {
        if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Return) ||
            UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.KeypadEnter))
        {
            // 操作説明状態へ遷移
            _gameFlowManager.ChangeState(new ManualState(_gameFlowManager));
        }
    }

    // ==================================================
    // Exit: タイトル画面から出る時の処理
    // ==================================================
    public void Exit()
    {
        _gameFlowManager.ShowTitlePanel(false);
    }
}