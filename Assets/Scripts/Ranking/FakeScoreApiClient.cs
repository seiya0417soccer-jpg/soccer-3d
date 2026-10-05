using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// FakeScoreApiClient.cs
/// テスト・オフライン検証用のモックAPIクライアント
/// 
/// - IScoreApiClientを実装する
/// - Dockerを起動しなくてもランキング機能の動作確認ができる
/// - VContainerの登録を切り替えるだけで本番・モックを差し替えられる
/// - 500ms待機することで実際の通信に近い挙動を再現する
/// </summary>
public class FakeScoreApiClient : IScoreApiClient
{
    // モック用のダミーデータ
    private readonly List<PlayerScoreData> _fakeData = new List<PlayerScoreData>
    {
        new PlayerScoreData("Oresama", 9999),
        new PlayerScoreData("AI_Bot", 8888),
        new PlayerScoreData("Player1", 7777),
        new PlayerScoreData("Player2", 6666),
        new PlayerScoreData("Player3", 5555),
    };

    // ==================================================
    // GetRankingAsync: ダミーのランキングデータを返す
    // 実際の通信に近い挙動を再現するため500ms待機する
    // ==================================================
    public async UniTask<List<PlayerScoreData>> GetRankingAsync(CancellationToken ct = default)
    {
        await UniTask.Delay(500, cancellationToken: ct);
        Debug.Log("FakeScoreApiClient: ダミーランキングを返します");
        return new List<PlayerScoreData>(_fakeData);
    }

    // ==================================================
    // PostScoreAsync: スコア登録のモック処理
    // 実際には保存せずダミーデータに追加する
    // ==================================================
    public async UniTask PostScoreAsync(PlayerScoreData score, CancellationToken ct = default)
    {
        await UniTask.Delay(500, cancellationToken: ct);
        _fakeData.Add(score);
        Debug.Log($"FakeScoreApiClient: スコアを受け取りました → {score.Name} : {score.Score}");
    }
}