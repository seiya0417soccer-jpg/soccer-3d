/// <summary>
/// IScoreReader.cs
/// スコアの読み取り専用インターフェース
/// 
/// - スコアを参照したいクラスはこのインターフェースを通して読む
/// - 書き換えができないので意図しない変更を防げる
/// </summary>
public interface IScoreReader
{
    // 現在のスコアを読み取る
    int Score { get; }
}