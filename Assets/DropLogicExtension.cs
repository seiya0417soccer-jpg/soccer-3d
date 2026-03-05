using UnityEngine;

public class DropLogicExtension : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DropPuzzleBattle dropPuzzle;

    [Header("E-Key Bomb Settings")]
    [SerializeField] private int eKeyBombType = 9;
    [SerializeField] private KeyCode eKey = KeyCode.E;

    // 次のピースをE爆弾にするか
    private bool nextPieceIsEKeyBomb = false;

    // 爆弾待機中フラグ（連打防止）
    private bool eBombPending = false;

    void Update()
    {
        if (Input.GetKeyDown(eKey))
        {
            OnEKeyPressed();
        }
    }

    void OnEKeyPressed()
    {
        // すでに爆弾予約されているなら無視
        if (eBombPending)
            return;

        eBombPending = true;
        nextPieceIsEKeyBomb = true;

        if (dropPuzzle != null)
            dropPuzzle.SetSkipDestroyedNotification(true);
    }

    // DropPuzzleBattle から呼ばれる
    public int GetNextPieceType(int defaultType)
    {
        if (nextPieceIsEKeyBomb)
        {
            nextPieceIsEKeyBomb = false;
            return eKeyBombType;
        }

        return defaultType;
    }

    // 爆弾処理終了時に呼ぶ
    public void OnEKeyBombFinished()
    {
        eBombPending = false;

        if (dropPuzzle != null)
            dropPuzzle.SetSkipDestroyedNotification(false);
    }
}