using UnityEngine;
using VContainer;

/// <summary>
/// DropLogicExtension.cs
/// EKeyBomb（爆弾）発動の拡張ロジック
/// 
/// - Eキー押下で次のピースをEKeyBombに変更予約する
/// - DropPuzzleBattleに破壊通知スキップを指示する
/// - DropPuzzleBattleの参照をSerializeFieldではなくVContainer Injectで受け取る
///   → InspectorでのアサインミスをなくしDIで一元管理する（引き継ぎやすさの向上）
/// </summary>
public class DropLogicExtension : MonoBehaviour
{
    [Header("E-Key Bomb Settings")]
    // Eキーで出す爆弾のタイプ番号
    [SerializeField] private int _eKeyBombType = 11;

    // Eキーの入力キー
    [SerializeField] private KeyCode _eKey = KeyCode.E;

    // DropPuzzleBattleの参照（VContainer Injectで受け取る）
    // SerializeFieldをやめることでInspectorへの依存をなくした
    private DropPuzzleBattle _dropPuzzle;

    // 次に生成するピースがE爆弾かどうかのフラグ
    private bool _nextPieceIsEKeyBomb = false;

    // 爆弾予約中フラグ（連打防止用）
    private bool _eBombPending = false;

    // ==================================================
    // Inject: VContainerから依存を注入される
    // DropPuzzleBattleをSerializeFieldではなくDIで受け取る
    // ==================================================
    [Inject]
    public void Construct(DropPuzzleBattle dropPuzzle)
    {
        _dropPuzzle = dropPuzzle;
    }

    // ==================================================
    // Update: 毎フレーム更新
    // ==================================================
    void Update()
    {
        // Eキー押下で爆弾予約
        if (Input.GetKeyDown(_eKey))
        {
            OnEKeyPressed();
        }
    }

    // ==================================================
    // OnEKeyPressed: Eキー押下時処理
    // - 予約フラグを立てる
    // - DropPuzzleBattle側に破壊通知スキップを指示する
    // ==================================================
    void OnEKeyPressed()
    {
        // すでに爆弾予約中なら無視する（連打防止）
        if (_eBombPending) return;

        _eBombPending = true;        // 爆弾予約中フラグON
        _nextPieceIsEKeyBomb = true; // 次ピースをE爆弾にする

        // DropPuzzleBattleに破壊通知スキップを指示する
        // EKeyBomb爆発時はバフを付与しないためスキップが必要
        _dropPuzzle?.SetSkipDestroyedNotification(true);
    }

    // ==================================================
    // GetNextPieceType: DropPuzzleBattleから呼ばれる次ピースの種類取得
    // 次がE爆弾ならタイプを上書きして返す
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
    // OnEKeyBombFinished: 爆弾処理終了時に呼ぶ
    // DropPuzzleBattleから呼ばれる
    // - 予約フラグを解除する
    // - DropPuzzleBattle側のスキップ通知も解除する
    // ==================================================
    public void OnEKeyBombFinished()
    {
        _eBombPending = false; // 予約中フラグOFF

        // DropPuzzleBattle側の通知スキップを解除する
        _dropPuzzle?.SetSkipDestroyedNotification(false);
    }
}