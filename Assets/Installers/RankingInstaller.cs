using VContainer;
using VContainer.Unity;

/// <summary>
/// RankingInstaller.cs
/// ランキング機能関連クラスをDI登録する
/// 
/// - IScoreApiClient・IScoreRepository・各Repository・UIをまとめる
/// - DockerScoreApiClientとFakeScoreApiClientを切り替えるには
///   コメントアウトを変えるだけでよい（拡張性の向上）
/// - サーバーURLはここで一元管理する
/// - FallbackScoreRepositoryでAPI失敗時にLocalへフォールバックする
///   → Docker未起動時でもランキング機能が動作する
/// </summary>
public class RankingInstaller : IInstaller
{
    // DockerサーバーのURL（ローカル環境）
    private const string ServerUrl = "http://localhost:32769";

    public void Install(IContainerBuilder builder)
    {
        // 本番用（Docker起動時）
        builder.RegisterInstance<IScoreApiClient>(new DockerScoreApiClient(ServerUrl));

        // モック用（オフライン検証時はこちらをコメントイン・上をコメントアウト）
        // builder.RegisterInstance<IScoreApiClient>(new FakeScoreApiClient());

        // ApiScoreRepository（IScoreApiClient経由でAPIサーバーと通信）
        builder.Register<ApiScoreRepository>(Lifetime.Singleton);

        // LocalScoreRepository（PlayerPrefsでローカル保存）
        builder.Register<LocalScoreRepository>(Lifetime.Singleton);

        // FallbackScoreRepository（API失敗時にLocalへフォールバック）
        // IScoreRepositoryとして登録することでUI側は通信先を意識しない
        builder.Register<FallbackScoreRepository>(Lifetime.Singleton)
            .As<IScoreRepository>();

        // ランキングUI（ランキング機能の一部のためここで登録する）
        builder.RegisterComponentInHierarchy<RankingSubmitUI>();
        builder.RegisterComponentInHierarchy<RankingView>();
    }
}