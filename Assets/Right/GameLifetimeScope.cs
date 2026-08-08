using VContainer;
using VContainer.Unity;

/// <summary>
/// GameLifetimeScope.cs
/// VContainerのDIコンテナ設定
/// 
/// - CoreInstaller・PuzzleInstaller・BattleInstaller・RankingInstallerに
///   責務を分割して登録する
/// - 「どこに何が登録されているか」を一目でわかるようにした
/// - VContainer標準のIInstallerを使い、RegisterInstallerで登録する
/// </summary>
public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // 全体管理系（GameFlowManager・ScoreManager・ResultManager）
        new CoreInstaller().Install(builder);

        // パズル画面関連（DropPuzzleBattle・DropLogicExtension）
        new PuzzleInstaller().Install(builder);

        // バトル画面関連（YushaBrain・BattleMainManager・EnemySpawner・GameTimer）
        new BattleInstaller().Install(builder);

        // ランキング機能関連（IScoreApiClient・ApiScoreRepository・LocalScoreRepository）
        new RankingInstaller().Install(builder);
    }
}