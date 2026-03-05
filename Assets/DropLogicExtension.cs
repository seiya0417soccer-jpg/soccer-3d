using UnityEngine;

public class DropLogicExtension : MonoBehaviour
{
    [Header("References")]
    // DropPuzzleBattle の参照（ブロック管理用）
    [SerializeField] private DropPuzzleBattle dropPuzzle;

    [Header("E-Key Bomb Settings")]
    // Eキーで出す爆弾のタイプ番号
    [SerializeField] private int eKeyBombType = 9;

    // Eキーの入力キー
    [SerializeField] private KeyCode eKey = KeyCode.E;

    // 次に生成するピースがE爆弾かどうかのフラグ
    private bool nextPieceIsEKeyBomb = false;

    // 爆弾予約中フラグ（連打防止用）
    private bool eBombPending = false;

    void Update()
    {
        // Eキー押下で爆弾予約
        if (Input.GetKeyDown(eKey))
        {
            OnEKeyPressed();
        }
    }

    // Eキー押下時の処理
    void OnEKeyPressed()
    {
        // すでに爆弾予約中なら無視
        if (eBombPending)
            return;

        // 爆弾予約フラグを立てる
        eBombPending = true;
        nextPieceIsEKeyBomb = true;

        // DropPuzzleBattle側に通知して、通常の破壊通知をスキップ
        if (dropPuzzle != null)
            dropPuzzle.SetSkipDestroyedNotification(true);
    }

    // DropPuzzleBattle から呼ばれる：次のピースの種類を取得
    public int GetNextPieceType(int defaultType)
    {
        // 次がE爆弾ならタイプを上書き
        if (nextPieceIsEKeyBomb)
        {
            nextPieceIsEKeyBomb = false;
            return eKeyBombType;
        }

        // 通常のピースタイプを返す
        return defaultType;
    }

    // 爆弾処理終了時に呼ぶ：予約フラグ解除
    public void OnEKeyBombFinished()
    {
        eBombPending = false;

        // DropPuzzleBattle 側のスキップ通知も解除
        if (dropPuzzle != null)
            dropPuzzle.SetSkipDestroyedNotification(false);
    }
}