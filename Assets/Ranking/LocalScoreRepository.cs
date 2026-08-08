using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// LocalScoreRepository.cs
/// PlayerPrefsを使ってローカルにTOP5スコアを保存・取得するRepositoryクラス
/// 
/// - IScoreRepositoryを実装する
/// - オフライン時やDocker未起動時でもローカルのTOP5を表示できる
/// - スコアはJSON形式でPlayerPrefsに保存する
///   → List<PlayerScoreData>をそのまま保存・復元できる
/// - TOP5を超えた場合は下位スコアを削除して常に上位5件を維持する
/// </summary>
public class LocalScoreRepository : IScoreRepository
{
    // PlayerPrefsのキー（GameConstantsで管理してもよいが
    // ランキング機能内で完結するためここで定義する）
    private const string RankingKey = "LocalRanking";

    // ローカルに保存する最大件数
    private const int MaxEntries = 5;

    // ==================================================
    // GetRankingAsync: ローカルのTOP5を取得する
    // PlayerPrefsからJSONを読み込んでデシリアライズする
    // ==================================================
    public UniTask<List<PlayerScoreData>> GetRankingAsync(CancellationToken ct = default)
    {
        string json = PlayerPrefs.GetString(RankingKey, "[]");
        var list = JsonConvert.DeserializeObject<List<PlayerScoreData>>(json)
                   ?? new List<PlayerScoreData>();
        return UniTask.FromResult(list);
    }

    // ==================================================
    // SaveScoreAsync: スコアをローカルに保存する
    // 既存のTOP5に追加してスコア降順で並び替え・5件を超えたら削除する
    // ==================================================
    public UniTask SaveScoreAsync(PlayerScoreData score, CancellationToken ct = default)
    {
        string json = PlayerPrefs.GetString(RankingKey, "[]");
        var list = JsonConvert.DeserializeObject<List<PlayerScoreData>>(json)
                   ?? new List<PlayerScoreData>();

        // 新しいスコアを追加してスコア降順で並び替える
        list.Add(score);
        list.Sort((a, b) => b.Score.CompareTo(a.Score));

        // MAX件数を超えたら下位を削除する
        if (list.Count > MaxEntries)
            list.RemoveRange(MaxEntries, list.Count - MaxEntries);

        // JSONに変換してPlayerPrefsに保存する
        PlayerPrefs.SetString(RankingKey, JsonConvert.SerializeObject(list));
        PlayerPrefs.Save();

        return UniTask.CompletedTask;
    }
}