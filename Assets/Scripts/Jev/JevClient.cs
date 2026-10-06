using System;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// JevClient.cs
///
/// TypeSafe AI / Jev APIとの通信を担当するクラス。
///
/// 役割：
/// - 環境変数からAPIキーを取得
/// - Jev APIへHTTPリクエストを送信
/// - Jevから返ってきたJSONを解析
/// - Jevの判断結果をJevDecisionとして返す
///
/// 現在はOtameshiでの技術検証用。
/// 実際のゲーム状態は呼び出し側から渡せるようにしている。
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
    ///
    /// state：
    ///     Jevへ渡す状態情報
    ///
    /// instructions：
    ///     Jevへの質問内容
    ///
    /// choices：
    ///     Jevが選択できる候補
    /// </summary>
    public async UniTask<JevDecision> SendChoiceRequestAsync(
        string state,
        string instructions,
        params string[] choices)
    {
        // Windowsのユーザー環境変数からAPIキーを取得
        string apiKey =
            Environment.GetEnvironmentVariable("TYPESAFE_API_KEY");

        // APIキーが取得できなかった場合
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Debug.LogError(
                "TYPESAFE_API_KEY が取得できません。"
            );

            return null;
        }

        // Choiceの候補をJSONのcriteriaとして組み立てる
        StringBuilder criteriaBuilder = new StringBuilder();

        for (int i = 0; i < choices.Length; i++)
        {
            string choice = choices[i];

            criteriaBuilder.Append(
                $"\"{EscapeJson(choice)}\":\"{EscapeJson(choice)}\""
            );

            if (i < choices.Length - 1)
            {
                criteriaBuilder.Append(",");
            }
        }

        // Jevへ送信するJSONを作成
        //
        // state：
        //     Jevが判断するための状態
        //
        // questions：
        //     今回はactionというChoice質問を1つ送る
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

        // HTTP POSTリクエストを作成
        using UnityWebRequest request =
            new UnityWebRequest(Endpoint, "POST");

        // JSONをUTF-8へ変換
        byte[] body =
            Encoding.UTF8.GetBytes(json);

        // リクエストボディを設定
        request.uploadHandler =
            new UploadHandlerRaw(body);

        // レスポンス受信用Handler
        request.downloadHandler =
            new DownloadHandlerBuffer();

        // JSONを送信することを指定
        request.SetRequestHeader(
            "Content-Type",
            "application/json"
        );

        // Bearer認証
        request.SetRequestHeader(
            "Authorization",
            $"Bearer {apiKey}"
        );

        // API通信
        await request.SendWebRequest();

        // 通信失敗
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(
                $"Jev API Error: {request.responseCode}\n" +
                $"{request.error}\n" +
                $"{request.downloadHandler.text}"
            );

            return null;
        }

        // レスポンスJSON取得
        string responseJson =
            request.downloadHandler.text;

        // デバッグ用にレスポンス全体を表示
        Debug.Log(
            $"Jev API Response:\n{responseJson}"
        );

        // JSONをC#オブジェクトへ変換
        JevApiResponse response =
            JsonUtility.FromJson<JevApiResponse>(
                responseJson
            );

        // 必要なデータが取得できなかった場合
        if (
            response == null ||
            response.answers == null ||
            response.answers.action == null
        )
        {
            Debug.LogError(
                "Jevのレスポンスを解析できませんでした。"
            );

            return null;
        }

        // 必要な情報だけを返す
        return new JevDecision
        {
            choice =
                response.answers.action.choice,

            confidence =
                response.answers.action.confidence
        };
    }

    /// <summary>
    /// JSON文字列内で特殊文字を安全に扱うための簡易エスケープ。
    /// </summary>
    private string EscapeJson(string value)
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