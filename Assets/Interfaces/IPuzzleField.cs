using R3;

/// <summary>
/// IPuzzleField.cs
/// 右画面（パズル側）の抽象インターフェース
/// 
/// - 実装を差し替えることで別のパズルゲームに変更できる
/// - DropPuzzleBattleはこのInterfaceを実装する
/// - バトル側はこのInterfaceを通してパズル側を知る
/// - R3のSubjectでイベントを発火する（疎結合）
/// </summary>
public interface IPuzzleField
{
    // ゲームリセット時に呼ぶ
    void ResetGame();

    // ブロックが消去された時に発火するSubject（消去ブロック数を通知）
    Subject<int> OnBlocksDestroyed { get; }

    // EKeyBombが爆発した時に発火するSubject
    Subject<Unit> OnEKeyBombExploded { get; }

    // ゲームオーバーになった時に発火するSubject
    Subject<Unit> OnGameOver { get; }
}