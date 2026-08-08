using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

/// <summary>
/// DockerScoreApiClient.cs
/// Docker上のAPIサーバーとの通信を担当するクラス
/// 
/// - IScoreApiClientを実装する
/// - UnityWebRequest + UniTaskで非同期通信を行う
/// - CancellationTokenでシーン遷移時に通信を安全にキャンセルできる
/// - BypassCertificateでローカルDocker環境のSSL証明書を回避する
/// - エラー種別（ネットワーク・サーバー・キャンセル）を分けて処理する
/// </summary>
public class DockerScoreApiClient : IScoreApiClient
{
    private readonly string _baseUrl;

    public DockerScoreApiClient(string baseUrl)
    {
        _baseUrl = baseUrl;
    }

    // ==================================================
    // GetRankingAsync: ランキング上位を取得する
    // GET /ranking
    // ==================================================
    public async UniTask<List<PlayerScoreData>> GetRankingAsync(CancellationToken ct = default)
    {
        using var request = UnityWebRequest.Get($"{_baseUrl}/ranking");
        request.certificateHandler = new BypassCertificate();

        try
        {
            await request.SendWebRequest().WithCancellation(ct);
        }
        catch (System.OperationCanceledException)
        {
            // キャンセルは正常系として扱う（シーン遷移時等）
            Debug.Log("GetRankingAsync: キャンセルされました");
            return new List<PlayerScoreData>();
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"GetRankingAsync: 通信失敗 → {request.error}");
            return new List<PlayerScoreData>();
        }

        var json = request.downloadHandler.text;
        Debug.Log($"GetRankingAsync: レスポンス → {json}");

        var list = JsonConvert.DeserializeObject<List<PlayerScoreData>>(json);
        return list ?? new List<PlayerScoreData>();
    }

    // ==================================================
    // PostScoreAsync: スコアを登録する
    // POST /ranking/save
    // ==================================================
    public async UniTask PostScoreAsync(PlayerScoreData score, CancellationToken ct = default)
    {
        string json = JsonConvert.SerializeObject(score);
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        using var request = new UnityWebRequest($"{_baseUrl}/ranking/save", "POST");
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.certificateHandler = new BypassCertificate();

        try
        {
            await request.SendWebRequest().WithCancellation(ct);
        }
        catch (System.OperationCanceledException)
        {
            // キャンセルは正常系として扱う
            Debug.Log("PostScoreAsync: キャンセルされました");
            return;
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"PostScoreAsync: 通信失敗 → {request.error}");
            return;
        }

        Debug.Log($"PostScoreAsync: 送信成功 → {request.downloadHandler.text}");
    }
}