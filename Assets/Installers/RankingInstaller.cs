using VContainer;
using VContainer.Unity;

/// <summary>
/// RankingInstaller.cs
/// ランキング機能関連クラスをDI登録する
/// 
/// - 通信層→データ層→ViewModel層→UI層の順で登録する
///   → 依存関係の流れが一目でわかるようにした（引き継ぎやすさの向上）
/// - DockerScoreApiClientとFakeScoreApiClientを切り替えるには
///   コメントアウトを変えるだけでよい（拡張性の向上）
/// - サーバーURLはここで一元管理する
/// - 自己ベスト保存はResultManagerのPlayerPrefsに任せる
///   → オンラインランキングと自己ベストは独立した設計にした
/// - ランキング機能を別プロジェクトに移植する場合は
///   このInstallerごとコピーするだけでよい（引き継ぎやすさの向上）
/// </summary>
public class RankingInstaller : IInstaller
{
    // DockerサーバーのURL（ローカル環境）
    // 本番環境に移行する場合はここを変更するだけでよい
    private const string ServerUrl = "http://localhost:32769";

    public void Install(IContainerBuilder builder)
    {
        // 通信層：APIサーバーとの通信を担当する
        // Docker起動時は DockerScoreApiClient を使う
        // オフライン検証時は FakeScoreApiClient に切り替える
        builder.RegisterInstance<IScoreApiClient>(new DockerScoreApiClient(ServerUrl));
        // builder.RegisterInstance<IScoreApiClient>(new FakeScoreApiClient());

        // データ層：スコアの保存・取得を担当する
        // ApiScoreRepository → Docker API経由
        // 自己ベスト保存はResultManagerのPlayerPrefsに任せるため
        // LocalScoreRepository・FallbackScoreRepositoryは不使用
        builder.Register<ApiScoreRepository>(Lifetime.Singleton)
            .As<IScoreRepository>();

        // ViewModel層：状態管理・データ取得のロジックを担当する
        // RankingView・RankingSubmitUIはViewModelを通してデータにアクセスする
        builder.Register<RankingViewModel>(Lifetime.Singleton);

        // UI層：ランキング機能のUIを担当する
        // シーン上にGameObjectとしてアタッチされている必要がある
        builder.RegisterComponentInHierarchy<RankingSubmitUI>();
        builder.RegisterComponentInHierarchy<RankingView>();
    }
}