using Newtonsoft.Json;

/// <summary>
/// PlayerScoreData.cs
/// ランキングのスコアデータモデル
/// 
/// - APIのJSONレスポンスをデシリアライズするためのクラス
/// - JsonPropertyでAPIのキー名とC#のプロパティ名を対応させる
/// - privateセッターで外部からの書き換えを防ぐ
/// </summary>
public class PlayerScoreData
{
    [JsonProperty("name")]
    public string Name { get; private set; }

    [JsonProperty("score")]
    public int Score { get; private set; }

    public PlayerScoreData(string name, int score)
    {
        Name = name;
        Score = score;
    }
}