using System;

/// <summary>
/// JevDecision.cs
/// 
/// Jevが返した「判断結果」を保持するデータクラス。
/// 
/// 今回は最小PoCなので、
/// - 選択結果
/// - confidence
/// の2つだけを扱う。
/// </summary>
[Serializable]
public class JevDecision
{
    /// <summary>
    /// Jevが選択した行動。
    /// 
    /// 例：
    /// "LEFT"
    /// "RIGHT"
    /// "ROTATE"
    /// </summary>
    public string choice;

    /// <summary>
    /// Jevの判断に対するconfidence。
    /// 
    /// 例：
    /// 0.26
    /// </summary>
    public float confidence;
}