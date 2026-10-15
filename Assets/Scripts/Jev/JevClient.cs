using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// TypeSafe AI / Jev APIとの通信を担当するクラス。
///
/// 役割：
/// - 環境変数からAPIキーを取得する
/// - Jev APIへHTTPリクエストを送信する
/// - Jevから返ってきたJSONを解析する
/// - Jevの判断結果をJevDecisionとして返す
///
/// 通信のキャンセルにも対応し、呼び出し側から処理を制御できるようにする。
/// </summary>
public class JevClient
{
    // Jev APIのエンドポイント
    private const string Endpoint =
        "https://api.typesafe.ai/v1/systemone";

    // 使用するJevモデル
    private const string Model = "jev-latest";

    /// <summary>
    /// Choice形式の質問をJevへ送信する。
    /// 既存の呼び出し元との互換性を維持する。
    /// </summary>
    public UniTask<JevDecision> SendChoiceRequestAsync(
        string state,
        string instructions,
        params string[] choices)
    {
        return SendChoiceRequestAsync(
            state,
            instructions,
            CancellationToken.None,
            choices);
    }

    /// <summary>
    /// Choice形式の質問をJevへ送信する。
    /// キャンセルトークンによって通信待ちを中断できる。
    /// </summary>
    public async UniTask<JevDecision> SendChoiceRequestAsync(
        string state,
        string instructions,
        CancellationToken cancellationToken,
        params string[] choices)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Windowsのユーザー環境変数からAPIキーを取得する
        string apiKey =
            Environment.GetEnvironmentVariable("TYPESAFE_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Debug.LogError(
                "TYPESAFE_API_KEY が取得できません。");

            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Choiceの候補をJSONのcriteriaとして組み立てる
        StringBuilder criteriaBuilder = new StringBuilder();

        for (int i = 0; i < choices.Length; i++)
        {
            string choice = choices[i];

            criteriaBuilder.Append(
                $"\"{EscapeJson(choice)}\":\"{EscapeJson(choice)}\"");

            if (i < choices.Length - 1)
            {
                criteriaBuilder.Append(",");
            }
        }

        // Jevへ送信するJSONを作成する
        string json = $@"{{
            ""state"": ""{EscapeJson(state)}"",
            ""model"": ""{Model}"",
            ""questions"": {{
                ""action"": {{
                    ""type"": ""choice"",
                    ""instructions"": ""{EscapeJson(instructions)}"",
                    ""criteria"": {{
                        {criteriaBuilder}
                    }}
                }}
            }}
        }}";

        cancellationToken.ThrowIfCancellationRequested();

        // HTTP POSTリクエストを作成する
        using UnityWebRequest request =
            new UnityWebRequest(Endpoint, "POST");

        // JSONをUTF-8へ変換する
        byte[] body = Encoding.UTF8.GetBytes(json);

        // リクエストボディを設定する
        request.uploadHandler =
            new UploadHandlerRaw(body);

        // レスポンス受信用Handler
        request.downloadHandler =
            new DownloadHandlerBuffer();

        // JSONを送信することを指定する
        request.SetRequestHeader(
            "Content-Type",
            "application/json");

        // Bearer認証
        request.SetRequestHeader(
            "Authorization",
            $"Bearer {apiKey}");

        // API通信を行い、キャンセル要求にも対応する
        await request.SendWebRequest()
            .WithCancellation(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        // 通信失敗
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(
                $"Jev API Error: {request.responseCode}\n" +
                $"{request.error}\n" +
                $"{request.downloadHandler.text}");

            return null;
        }

        // レスポンスJSONを取得する
        string responseJson =
            request.downloadHandler.text;

        // デバッグ用にレスポンス全体を表示する
        Debug.Log(
            $"Jev API Response:\n{responseJson}");

        cancellationToken.ThrowIfCancellationRequested();

        // JSONをC#オブジェクトへ変換する
        JevApiResponse response =
            JsonUtility.FromJson<JevApiResponse>(responseJson);

        // 必要なデータが取得できなかった場合
        if (response == null ||
            response.answers == null ||
            response.answers.action == null)
        {
            Debug.LogError(
                "Jevのレスポンスを解析できませんでした。");

            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // 必要な情報だけを返す
        return new JevDecision
        {
            choice = response.answers.action.choice,
            confidence = response.answers.action.confidence
        };
    }

    /// <summary>
    /// JSON文字列内の特殊文字をエスケープする。
    /// </summary>
    private static string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    /// <summary>
    /// Jev APIレスポンス全体。
    /// </summary>
    [Serializable]
    private class JevApiResponse
    {
        public Answers answers;
    }

    /// <summary>
    /// answers部分。
    /// </summary>
    [Serializable]
    private class Answers
    {
        public ActionAnswer action;
    }

    /// <summary>
    /// action部分。
    /// </summary>
    [Serializable]
    private class ActionAnswer
    {
        public string choice;
        public float confidence;
    }
}