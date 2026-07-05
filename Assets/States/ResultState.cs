/// <summary>
/// ResultState.cs
/// リザルト画面の状態
/// 
/// - Enter：リザルト画面を表示
/// - Update：Enter→もう一度・Backspace→タイトルへ
/// - Exit：リザルト画面を非表示
/// </summary>
public class ResultState : IGameState
{
    // GameFlowManagerへの参照
    private readonly GameFlowManager _gameFlowManager;

    public ResultState(GameFlowManager gameFlowManager)
    {
        _gameFlowManager = gameFlowManager;
    }

    // ==================================================
    // Enter: リザルト画面に入った時の処理
    // ==================================================
    public void Enter()
    {
        ResultManager.Instance.ShowResult();
    }

    // ==================================================
    // Update: キー入力でリザルト操作
    // ==================================================
    public void Update()
    {
        // Enterともう一度→カウントダウンへ
        if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Return) ||
            UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.KeypadEnter))
        {
            _gameFlowManager.RestartFromCountdown();
        }
        // Backspaceでタイトルへ
        if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Backspace))
        {
            // GoToTitle()を通してリセット処理を行う
            _gameFlowManager.GoToTitle();
        }
    }

    // ==================================================
    // Exit: リザルト画面を非表示
    // ==================================================
    public void Exit()
    {
        ResultManager.Instance.HideResult();
    }
}