using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// JevLatencyTest.cs
/// 
/// Jev APIの検証用スクリプト（本番コードではない・検証後は削除してよい）。
/// 
/// ① レイテンシ計測
///    選択肢 3 / 10 / 52 個 × 各10回 = 30リクエスト
///    ※計測値は「HTTP通信 + ログ出力 + JSON解析」を含むE2E時間。
///      API単体の応答時間ではなく、次の1手を得るまでの実時間を見る。
///    ※選択肢は記号のみで意味を説明していないため、判断の良し悪しは評価しない。
/// 
/// ② 簡易動作確認
///    単純な盤面で、Jevが明らかにおかしな候補だけを選び続けないかを見る。
///    4択 × 10回 = 10リクエスト
///    ※10回だけなので「品質を検証した」とは言えない。あくまで簡易確認。
/// 
/// 合計 40リクエスト消費する。
/// 
/// Lの定義：ピースが占有する範囲の「左端の列番号」。
/// 今回は手で用意した有効な候補だけを使う。
/// 実ゲームでは、回転後の相対座標から最小Xを求めて正規化する必要がある（今回は対象外）。
/// </summary>
public class JevLatencyTest : MonoBehaviour
{
    // 各パターンの計測回数
    private const int TrialCount = 10;

    // レイテンシ計測で使う選択肢の数
    private static readonly int[] ChoiceCounts = { 3, 10, 52 };

    private async void Start()
    {
        JevClient client = new JevClient();

        await MeasureLatencyAsync(client);
        await CheckSimpleBoardAsync(client);

        Debug.Log("=== Jev検証 完了 ===");
    }

    // ==================================================
    // ① レイテンシ計測
    // ==================================================
    private async UniTask MeasureLatencyAsync(JevClient client)
    {
        string state =
            "テトリス型パズル。列の高さ: [3,3,4,2,0,0,1,5,5,2,1,0,0]。" +
            "現在のピース: L字型。";

        string instructions =
            "穴ができにくく、横一列を揃えやすい置き方を1つ選んでください。";

        foreach (int count in ChoiceCounts)
        {
            string[] choices = BuildChoices(count);

            List<long> times = new List<long>();
            int failCount = 0;

            for (int i = 0; i < TrialCount; i++)
            {
                try
                {
                    Stopwatch sw = Stopwatch.StartNew();

                    JevDecision decision =
                        await client.SendChoiceRequestAsync(
                            state, instructions, choices);

                    sw.Stop();

                    // 失敗（null）は時間に含めず失敗回数として数える
                    if (decision == null)
                    {
                        failCount++;
                        continue;
                    }

                    times.Add(sw.ElapsedMilliseconds);
                }
                catch (System.Exception e)
                {
                    // 例外が出ても計測全体は止めない
                    failCount++;
                    Debug.LogWarning(
                        $"[選択肢{count}個] {i + 1}回目失敗: {e.Message}");
                }
            }

            ReportLatency(count, times, failCount);
        }
    }

    // ==================================================
    // ② 簡易動作確認
    // 右端3列だけ高い盤面で、I字（横向き）を置く場面を想定する
    // ==================================================
    private async UniTask CheckSimpleBoardAsync(JevClient client)
    {
        // 左側は低く、右端3列(10,11,12)だけ高い
        string state =
            "テトリス型パズル。盤面は13列。" +
            "列の高さを左から右の順に並べると [0,0,0,0,0,0,0,0,0,0,5,5,5]。" +
            "現在のピース: I字（長さ4の棒）。";

        // 候補の意味をinstructionsで説明する
        string instructions =
            "Lはピースが占有する範囲の左端の列番号です（0始まり）。" +
            "ROT0は横向き（4列使う）、ROT1は縦向き（1列使う）です。" +
            "列の高さが低い場所に置き、穴を作らず横一列を揃えやすい置き方を1つ選んでください。";

        string[] choices = { "ROT0_L1", "ROT0_L5", "ROT0_L9", "ROT1_L3" };

        // 選択された候補ごとの回数
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (string c in choices) counts[c] = 0;

        int failCount = 0;

        for (int i = 0; i < TrialCount; i++)
        {
            try
            {
                JevDecision decision =
                    await client.SendChoiceRequestAsync(
                        state, instructions, choices);

                if (decision == null)
                {
                    failCount++;
                    continue;
                }

                if (counts.ContainsKey(decision.choice))
                    counts[decision.choice]++;
            }
            catch (System.Exception e)
            {
                failCount++;
                Debug.LogWarning($"[簡易確認] {i + 1}回目失敗: {e.Message}");
            }
        }

        // 結果の表示
        // ROT0_L9 は高い列(10〜12)に重なる悪手の見本
        Debug.Log(
            "[簡易確認] 選択回数 → " +
            $"ROT0_L1(低い・良) {counts["ROT0_L1"]} / " +
            $"ROT0_L5(低い・良) {counts["ROT0_L5"]} / " +
            $"ROT0_L9(高い・悪) {counts["ROT0_L9"]} / " +
            $"ROT1_L3(縦・中立) {counts["ROT1_L3"]} / " +
            $"失敗 {failCount}");
    }

    // 選択肢を指定数だけ生成する（回転0〜3 × 位置0〜12 の順）
    private string[] BuildChoices(int count)
    {
        string[] choices = new string[count];

        for (int i = 0; i < count; i++)
        {
            int rot = i / 13;
            int x = i % 13;
            choices[i] = $"ROT{rot}_X{x}";
        }

        return choices;
    }

    // 平均・最小・最大を表示する
    private void ReportLatency(int choiceCount, List<long> times, int failCount)
    {
        if (times.Count == 0)
        {
            Debug.LogError(
                $"[選択肢{choiceCount}個] 全て失敗（{failCount}回）");
            return;
        }

        long sum = 0;
        long min = long.MaxValue;
        long max = long.MinValue;

        foreach (long t in times)
        {
            sum += t;
            if (t < min) min = t;
            if (t > max) max = t;
        }

        Debug.Log(
            $"[選択肢{choiceCount}個] " +
            $"平均 {sum / times.Count}ms / " +
            $"最小 {min}ms / 最大 {max}ms / " +
            $"成功 {times.Count}回 / 失敗 {failCount}回");
    }
}