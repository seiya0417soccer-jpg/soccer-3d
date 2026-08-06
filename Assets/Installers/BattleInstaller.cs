using VContainer;
using VContainer.Unity;

/// <summary>
/// BattleInstaller.cs
/// バトル画面（左画面）関連クラスをDI登録する
/// 
/// - YushaBrain・BattleMainManager・EnemySpawner・GameTimer等をまとめる
/// - YushaBrainをIBattleFieldとしても登録する
///   → BattleMainManagerがIBattleField経由で受け取れるようにする
///   → AsSelf()を残すことでYushaBrain具体型での解決も可能にする
/// </summary>
public class BattleInstaller : IInstaller
{
    public void Install(IContainerBuilder builder)
    {
        // AsSelf()でYushaBrain型・As<IBattleField>()でIBattleField型の両方で解決できる
        builder.RegisterComponentInHierarchy<YushaBrain>().AsSelf().As<IBattleField>();
        builder.RegisterComponentInHierarchy<BattleMainManager>();
        builder.RegisterComponentInHierarchy<EnemySpawner>();
        builder.RegisterComponentInHierarchy<GameTimer>();
    }
}