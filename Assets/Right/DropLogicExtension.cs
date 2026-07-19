using UnityEngine;

/// <summary>
/// DropLogicExtension.cs
/// EKeyBomb（爆弾）発動の拡張ロジック
/// 
/// - Eキー押下で次のピースをEKeyBombに変更予約する
/// - DropPuzzleBattleに破壊通知スキップを指示する
/// </summary>
public class DropLogicExtension : MonoBehaviour
{
    [Header("References")]
    // DropPuzzleBattleの参照（ブロック管理用）
    [SerializeField] private DropPuzzleBattle _dropPuzzle;

    [Header("E-Key Bomb Settings")]
    // Eキーで出す爆弾のタイプ番号
    [SerializeField] private int _eKeyBombType = 11;

    // Eキーの入力キー
    [SerializeField] private KeyCode _eKey = KeyCode.E;

    // 次に生成するピースがE爆弾かどうかのフラグ
    private bool _nextPieceIsEKeyBomb = false;

    // 爆弾予約中フラグ（連打防止用）
    private bool _eBombPending = false;

    // ==================================================
    // Update: 毎フレーム更新
    // ==================================================
    void Update()
    {
        // Eキー押下で爆弾予約
        if (Input.GetKeyDown(_eKey))
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
        if (_eBombPending)
            return;

        _eBombPending = true;             // 爆弾予約中フラグON
        _nextPieceIsEKeyBomb = true;      // 次ピースをE爆弾にする

        // DropPuzzleBattleに破壊通知スキップを指示
        if (_dropPuzzle != null)
            _dropPuzzle.SetSkipDestroyedNotification(true);
    }

    // ==================================================
    // DropPuzzleBattleから呼ばれる：次ピースの種類取得
    // - 次がE爆弾ならタイプ上書き
    // ==================================================
    public int GetNextPieceType(int defaultType)
    {
        if (_nextPieceIsEKeyBomb)
        {
            _nextPieceIsEKeyBomb = false;
            return _eKeyBombType;
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
        _eBombPending = false;             // 予約中フラグOFF

        // DropPuzzleBattle側の通知スキップ解除
        if (_dropPuzzle != null)
            _dropPuzzle.SetSkipDestroyedNotification(false);
    }
}