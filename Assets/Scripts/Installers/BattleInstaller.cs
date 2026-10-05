using VContainer;
using VContainer.Unity;

/// <summary>
/// BattleInstaller.cs
/// バトル画面（左画面）関連クラスをDI登録する
/// 
/// - YushaBrain・BattleMainManager・EnemySpawner・GameTimer等をまとめる
/// - YushaBrainをIBattleField・ICameraShakeableとしても登録する
///   → BattleMainManagerがIBattleField経由で受け取れるようにする
///   → GameFlowManagerがICameraShakeable経由でカメラ操作できるようにする
///   → AsSelf()を残すことでYushaBrain具体型での解決も可能にする
/// </summary>
public class BattleInstaller : IInstaller
{
    public void Install(IContainerBuilder builder)
    {
        // AsSelf()でYushaBrain型
        // As<IBattleField>()でバトル側Interface
        // As<ICameraShakeable>()でカメラ演出Interface
        // 3つ全てで解決できるように登録する
        builder.RegisterComponentInHierarchy<YushaBrain>()
            .AsSelf()
            .As<IBattleField>()
            .As<ICameraShakeable>();

        builder.RegisterComponentInHierarchy<BattleMainManager>();
        builder.RegisterComponentInHierarchy<EnemySpawner>();
        builder.RegisterComponentInHierarchy<GameTimer>();
    }
}