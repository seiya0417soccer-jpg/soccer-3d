// 自動プレイで実行する移動操作
public enum PuzzlePlacementAction
{
    MoveLeft,
    MoveRight,
    MoveDown,
    RotateCounterClockwise
}

/// <summary>
/// 1つの合法なピース配置候補を表現する。
///
/// Jev専用のデータ型にはせず、配置判断方式から独立させる。
/// </summary>
public sealed class PuzzlePlacementCandidate
{
    /// 候補を識別するためのID。
    public string Id { get; }

    // 配置先までの移動手順
    private readonly PuzzlePlacementAction[] _actions;

    /// 現在の形状から反時計回りに回転させる回数。
    public int Rotation { get; }

    /// ピースが占有する最も左の列。
    public int LeftmostColumn { get; }

    /// 配置時の基準座標X。
    public int AnchorX { get; }

    /// 配置時の基準座標Y。
    public int LandingY { get; }

    /// 特殊ブロックの爆発を考慮しない簡易計算による完成行数。
    public int DirectLinesCleared { get; }

    /// 直接完成した行を除去した盤面の穴数。
    public int HolesAfterDirectClear { get; }

    /// 直接完成した行を除去した盤面の列高さ合計。
    public int AggregateHeightAfterDirectClear { get; }

    /// 直接完成した行を除去した盤面の最大列高さ。
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
        int maxHeightAfterDirectClear,
        PuzzlePlacementAction[] actions = null)
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

        // 外部から渡された配列をコピーして保持する
        _actions = actions == null
            ? new PuzzlePlacementAction[0]
            : (PuzzlePlacementAction[])actions.Clone();
    }

    // 移動手順をコピーして取得する
    public PuzzlePlacementAction[] CopyActions()
    {
        return (PuzzlePlacementAction[])_actions.Clone();
    }

    /// 判断方式へ渡す候補の説明を作成する。
    /// 候補IDと説明文は別々に保持する。
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