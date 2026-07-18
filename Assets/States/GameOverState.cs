/// <summary>
/// GameOverState.cs
/// ゲームオーバー画面の状態
/// 
/// - Enter：ゲームオーバーパネルを表示・TimeScale=0
/// - Update：3秒経過またはエンターキーでリザルトへ遷移
/// - Exit：ゲームオーバーパネルを非表示
/// </summary>
public class GameOverState : IGameState
{
    // GameFlowManagerへの参照
    private readonly GameFlowManager _gameFlowManager;

    // 経過時間（3秒で自動遷移）
    private float _elapsed = 0f;
    private const float AutoTransitionTime = 3f;

    public GameOverState(GameFlowManager gameFlowManager)
    {
        _gameFlowManager = gameFlowManager;
    }

    // ==================================================
    // Enter: ゲームオーバー画面に入った時の処理
    // ==================================================
    public void Enter()
    {
        UnityEngine.Time.timeScale = 0f;
        _elapsed = 0f;
        _gameFlowManager.ShowGameOverPanel(true);
        _gameFlowManager.ShowInGameUI(false); // キルカウント・タイマーを非表示

        // シェイク中だった場合に揺れっぱなしにならないよう停止する
        _gameFlowManager.StopYushaCameraShake();
    }

    // ==================================================
    // Update: 3秒待機またはエンターキーでリザルトへ
    // ==================================================
    public void Update()
    {
        // エンターキーで即リザルトへ
        if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Return) ||
            UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.KeypadEnter))
        {
            GoToResult();
            return;
        }

        // 3秒経過で自動遷移（timeScale=0でも動くunscaledDeltaTimeを使用）
        _elapsed += UnityEngine.Time.unscaledDeltaTime;
        if (_elapsed >= AutoTransitionTime)
            GoToResult();
    }

    // ==================================================
    // Exit: ゲームオーバーパネルを非表示
    // ==================================================
    public void Exit()
    {
        _gameFlowManager.ShowGameOverPanel(false);
    }

    // ==================================================
    // リザルト状態へ遷移
    // ==================================================
    private void GoToResult()
    {
        _gameFlowManager.ChangeState(new ResultState(_gameFlowManager));
    }
}