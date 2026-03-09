using UnityEngine;

/// <summary>
/// DropLogicExtension.cs 完全版
/// - 設計書ルール（家系図・引数経由・直接変数操作禁止）厳守
/// - Eキー爆弾予約フロー完全版
/// </summary>
public class DropLogicExtension : MonoBehaviour
{
    [Header("References")]
    // DropPuzzleBattle の参照（ブロック管理用）
    [SerializeField] private DropPuzzleBattle dropPuzzle;

    [Header("E-Key Bomb Settings")]
    // Eキーで出す爆弾のタイプ番号
    [SerializeField] private int eKeyBombType = 11;

    // Eキーの入力キー
    [SerializeField] private KeyCode eKey = KeyCode.E;

    // 次に生成するピースがE爆弾かどうかのフラグ
    private bool nextPieceIsEKeyBomb = false;

    // 爆弾予約中フラグ（連打防止用）
    private bool eBombPending = false;

    // ==================================================
    // 毎フレーム更新
    // ==================================================
    void Update()
    {
        // Eキー押下で爆弾予約
        if (Input.GetKeyDown(eKey))
        {
            OnEKeyPressed(); // Eキー押下時処理
        }
    }

    // ==================================================
    // Eキー押下時処理
    // - 予約フラグ立て
    // - DropPuzzleBattle側に破壊通知スキップを指示
    // ==================================================
    void OnEKeyPressed()
    {
        // すでに爆弾予約中なら無視
        if (eBombPending)
            return;

        eBombPending = true;             // 爆弾予約中フラグON
        nextPieceIsEKeyBomb = true;      // 次ピースをE爆弾にする

        // DropPuzzleBattleに破壊通知スキップを指示
        if (dropPuzzle != null)
            dropPuzzle.SetSkipDestroyedNotification(true);
    }

    // ==================================================
    // DropPuzzleBattleから呼ばれる：次ピースの種類取得
    // - 次がE爆弾ならタイプ上書き
    // ==================================================
    public int GetNextPieceType(int defaultType)
    {
        if (nextPieceIsEKeyBomb)
        {
            nextPieceIsEKeyBomb = false;
            return eKeyBombType;
        }
        return defaultType;
    }

    // ==================================================
    // 爆弾処理終了時に呼ぶ
    // - 予約フラグ解除
    // - DropPuzzleBattle側のスキップ通知も解除
    // ==================================================
    public void OnEKeyBombFinished()
    {
        eBombPending = false;             // 予約中フラグOFF

        // DropPuzzleBattle側の通知スキップ解除
        if (dropPuzzle != null)
            dropPuzzle.SetSkipDestroyedNotification(false);
    }
}