using UnityEngine;
using TMPro;

/// <summary>
/// ScoreManager.cs
/// キル数（スコア）の管理とUI表示
/// 
/// - IScoreReaderで読み取り・IScoreWriterで書き込みを分離することで
///   「読む権限」と「書く権限」を使う側に応じて制限している
/// - scoreはprivateでカプセル化し、外部から直接書き換えできない設計にした
/// - VContainerでDI管理するためSingletonは不要と判断して削除した
///   （シーンが1つのため重複インスタンスの心配もない）
/// - スコアが変わった時だけUIを更新することで毎フレームの無駄な描画を避けている
/// </summary>
public class ScoreManager : MonoBehaviour, IScoreReader, IScoreWriter
{
    // スコアはprivateで管理（外部から直接書き換えできない）
    private int _score = 0;

    // IScoreReader：外部からは読み取りのみ可能（書き換え不可）
    public int Score => _score;

    // TMPコンポーネントをキャッシュしておく（毎フレームGetComponentしない）
    [SerializeField] private TextMeshProUGUI _scoreText;

    // ==================================================
    // Start: UI初期化
    // 起動時にスコア表示を初期状態に更新する
    // ==================================================
    void Start()
    {
        UpdateUI();
    }

    // ==================================================
    // IScoreWriter：スコア加算
    // 外部からはこのメソッドを通してスコアを変更する
    // YushaBrainが敵を倒した時に呼ぶ
    // ==================================================
    public void AddScore(int amount)
    {
        _score += amount;
        // スコアが変わった時だけUIを更新する（毎フレーム更新しない）
        UpdateUI();
    }

    // ==================================================
    // IScoreWriter：スコアリセット
    // もう一度プレイ・タイトル戻り時にGameFlowManagerから呼ぶ
    // ==================================================
    public void ResetScore()
    {
        _score = 0;
        // リセット後にUIを即時更新する
        UpdateUI();
    }

    // ==================================================
    // UI更新（内部処理）
    // スコアが変わった時だけ呼ぶことで無駄な描画を避ける
    // ==================================================
    private void UpdateUI()
    {
        if (_scoreText != null)
            _scoreText.text = "KILLS: " + _score;
    }
}