using VContainer;
using VContainer.Unity;

/// <summary>
/// GameLifetimeScope.cs
/// VContainerのDIコンテナ設定
/// 
/// - このクラスでどのクラスをDIするかを定義する
/// - RegisterはC#クラス、RegisterComponentはMonoBehaviourに使う
/// </summary>
public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<GameFlowManager>();
        builder.RegisterComponentInHierarchy<BattleMainManager>();
        builder.RegisterComponentInHierarchy<ResultManager>();
        builder.RegisterComponentInHierarchy<GameTimer>();
        builder.RegisterComponentInHierarchy<YushaBrain>();
        builder.RegisterComponentInHierarchy<EnemySpawner>();
        builder.RegisterComponentInHierarchy<DropPuzzleBattle>().AsSelf().As<IPuzzleField>();
        builder.RegisterComponentInHierarchy<ScoreManager>().AsSelf().As<IScoreWriter>().As<IScoreReader>();
    }
}