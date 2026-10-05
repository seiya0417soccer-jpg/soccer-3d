using VContainer;
using VContainer.Unity;

/// <summary>
/// CoreInstaller.cs
/// ゲーム全体の管理系クラスをDI登録する
/// 
/// - パズル・バトル以外の全体管理を担うクラスをまとめる
/// - GameFlowManager・ScoreManager・ResultManager等
/// </summary>
public class CoreInstaller : IInstaller
{
    public void Install(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<GameFlowManager>();
        builder.RegisterComponentInHierarchy<ResultManager>();
        builder.RegisterComponentInHierarchy<ScoreManager>().AsSelf().As<IScoreWriter>().As<IScoreReader>();
    }
}