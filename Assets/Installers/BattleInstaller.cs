using VContainer;
using VContainer.Unity;

/// <summary>
/// BattleInstaller.cs
/// バトル画面（左画面）関連クラスをDI登録する
/// 
/// - YushaBrain・BattleMainManager・EnemySpawner・GameTimer等をまとめる
/// </summary>
public class BattleInstaller : IInstaller
{
    public void Install(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<YushaBrain>();
        builder.RegisterComponentInHierarchy<BattleMainManager>();
        builder.RegisterComponentInHierarchy<EnemySpawner>();
        builder.RegisterComponentInHierarchy<GameTimer>();
    }
}