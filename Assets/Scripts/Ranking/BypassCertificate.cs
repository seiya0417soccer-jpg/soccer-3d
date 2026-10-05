using UnityEngine.Networking;

/// <summary>
/// BypassCertificate.cs
/// ローカルDocker環境のSSL証明書を回避するクラス
/// 
/// - ローカル開発環境ではSSL証明書が正規のものではないため
///   証明書チェックをスキップする必要がある
/// - 本番環境では使用しない
/// </summary>
public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        // ローカルDocker環境のため証明書チェックをスキップする
        return true;
    }
}