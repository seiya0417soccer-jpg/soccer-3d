using VContainer;
using VContainer.Unity;

/// <summary>
/// PuzzleInstaller.cs
/// パズル画面（右画面）関連クラスをDI登録する
/// 
/// - DropPuzzleBattle・DropLogicExtension等パズルロジックをまとめる
/// </summary>
public class PuzzleInstaller : IInstaller
{
    public void Install(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<DropPuzzleBattle>().AsSelf().As<IPuzzleField>();
        builder.RegisterComponentInHierarchy<DropLogicExtension>();
    }
}