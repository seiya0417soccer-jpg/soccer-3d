using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// ApiScoreRepository.cs
/// IScoreApiClientを使ってAPIサーバーとやり取りするRepositoryクラス
/// 
/// - IScoreRepositoryを実装する
/// - IScoreApiClient経由で通信するため
///   DockerとFakeの差し替えはここでは意識しない（拡張性の向上）
/// - 通信の詳細はIScoreApiClientに隠蔽されている
///   → Repositoryはデータの取得・保存の窓口に徹する
/// </summary>
public class ApiScoreRepository : IScoreRepository
{
    private readonly IScoreApiClient _apiClient;

    public ApiScoreRepository(IScoreApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    // ==================================================
    // GetRankingAsync: ランキングを取得する
    // IScoreApiClient経由でAPIサーバーから取得する
    // ==================================================
    public async UniTask<List<PlayerScoreData>> GetRankingAsync(CancellationToken ct = default)
    {
        return await _apiClient.GetRankingAsync(ct);
    }

    // ==================================================
    // SaveScoreAsync: スコアを保存する
    // IScoreApiClient経由でAPIサーバーに送信する
    // ==================================================
    public async UniTask SaveScoreAsync(PlayerScoreData score, CancellationToken ct = default)
    {
        await _apiClient.PostScoreAsync(score, ct);
    }
}