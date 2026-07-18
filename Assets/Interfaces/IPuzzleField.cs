using R3;

/// <summary>
/// IPuzzleField.cs
/// 右画面（パズル側）の抽象インターフェース
/// 
/// - 実装を差し替えることで別のパズルゲームに変更できる
/// - DropPuzzleBattleはこのInterfaceを実装する
/// - バトル側はこのInterfaceを通してパズル側を知る
/// - R3のObservableで公開（外部からOnNext()できないよう読み取り専用にする）
/// </summary>
public interface IPuzzleField
{
    // ゲームリセット時に呼ぶ
    void ResetGame();

    // ブロックが消去された時に発火するObservable（消去ブロック数を通知）
    // 読み取り専用にすることで外部から不正にOnNext()できないようにする
    Observable<int> OnBlocksDestroyed { get; }

    // EKeyBombが爆発した時に発火するObservable
    Observable<Unit> OnEKeyBombExploded { get; }

    // ゲームオーバーになった時に発火するObservable
    Observable<Unit> OnGameOver { get; }
}