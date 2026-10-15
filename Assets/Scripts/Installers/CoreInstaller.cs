using VContainer;
using VContainer.Unity;

/// <summary>
/// ゲーム全体の管理系クラスをDI登録する。
/// </summary>
public class CoreInstaller : IInstaller
{
    public void Install(IContainerBuilder builder)
    {
        // ゲームフロー管理とライフサイクル通知を登録する
        builder.RegisterComponentInHierarchy<GameFlowManager>()
            .AsSelf()
            .As<IGameLifecycleEvents>();

        builder.RegisterComponentInHierarchy<ResultManager>();

        builder.RegisterComponentInHierarchy<ScoreManager>()
            .AsSelf()
            .As<IScoreWriter>()
            .As<IScoreReader>();
    }
}