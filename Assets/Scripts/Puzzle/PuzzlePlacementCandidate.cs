/// <summary>
/// PuzzlePlacementCandidate.cs
///
/// 1つの合法なピース配置候補を表現する。
///
/// Jev専用のデータ型にはせず、配置判断方式から独立させる。
/// </summary>
public sealed class PuzzlePlacementCandidate
{
    /// <summary>
    /// 候補を識別するためのID。
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// 現在の形状から反時計回りに回転させる回数。
    /// </summary>
    public int Rotation { get; }

    /// <summary>
    /// ピースが占有する最も左の列。
    /// </summary>
    public int LeftmostColumn { get; }

    /// <summary>
    /// 配置時の基準座標X。
    /// </summary>
    public int AnchorX { get; }

    /// <summary>
    /// 配置時の基準座標Y。
    /// </summary>
    public int LandingY { get; }

    /// <summary>
    /// 特殊ブロックの爆発を考慮しない簡易計算による完成行数。
    /// </summary>
    public int DirectLinesCleared { get; }

    /// <summary>
    /// 直接完成した行を除去した盤面の穴数。
    /// </summary>
    public int HolesAfterDirectClear { get; }

    /// <summary>
    /// 直接完成した行を除去した盤面の列高さ合計。
    /// </summary>
    public int AggregateHeightAfterDirectClear { get; }

    /// <summary>
    /// 直接完成した行を除去した盤面の最大列高さ。
    /// </summary>
    public int MaxHeightAfterDirectClear { get; }

    public PuzzlePlacementCandidate(
        string id,
        int rotation,
        int leftmostColumn,
        int anchorX,
        int landingY,
        int directLinesCleared,
        int holesAfterDirectClear,
        int aggregateHeightAfterDirectClear,
        int maxHeightAfterDirectClear)
    {
        Id = id;
        Rotation = rotation;
        LeftmostColumn = leftmostColumn;
        AnchorX = anchorX;
        LandingY = landingY;

        DirectLinesCleared = directLinesCleared;
        HolesAfterDirectClear = holesAfterDirectClear;
        AggregateHeightAfterDirectClear =
            aggregateHeightAfterDirectClear;
        MaxHeightAfterDirectClear =
            maxHeightAfterDirectClear;
    }

    /// <summary>
    /// 判断方式へ渡す候補の説明を作成する。
    /// 候補IDと説明文は別々に保持する。
    /// </summary>
    public string ToChoiceDescription()
    {
        return
            $"{Id}: " +
            $"回転={Rotation}回、" +
            $"左端列={LeftmostColumn}、" +
            $"完成行数={DirectLinesCleared}、" +
            $"穴数={HolesAfterDirectClear}、" +
            $"列高さ合計={AggregateHeightAfterDirectClear}、" +
            $"最大高さ={MaxHeightAfterDirectClear}";
    }
}