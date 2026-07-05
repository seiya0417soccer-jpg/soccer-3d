/// <summary>
/// IGameState.cs
/// ゲームの状態を表すインターフェース
/// 
/// - 各状態クラスはこのインターフェースを実装する
/// - GameFlowManagerはIGameStateを通して状態を管理する
/// - 状態ごとにEnter・Update・Exitを持つ
/// </summary>
public interface IGameState
{
    // 状態に入った時に呼ばれる
    void Enter();

    // 毎フレーム呼ばれる
    void Update();

    // 状態から出る時に呼ばれる
    void Exit();
}