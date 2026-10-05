/// <summary>
/// IScoreWriter.cs
/// スコアの書き込み専用インターフェース
/// 
/// - スコアを変更したいクラスはこのインターフェースを通して書く
/// - 読み取りができないので責務が明確になる
/// </summary>
public interface IScoreWriter
{
    // スコアを加算する
    void AddScore(int amount);

    // スコアをリセットする
    void ResetScore();
}