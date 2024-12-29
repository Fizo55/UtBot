using System;
using System.Security.Cryptography;
using System.Text;

namespace Discord;

public class RestAuthenticator
{
    private readonly string _apiKey;
    private readonly string _secretKey;
    private readonly string _passphrase;

    public RestAuthenticator(string apiKey, string secretKey, string passphrase)
    {
        _apiKey = apiKey;
        _secretKey = secretKey;
        _passphrase = passphrase;
    }

    public (string Timestamp, string Nonce, string Signature) GenerateSignature(string method, string requestPath, string body = "")
    {
        string timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        string nonce = Guid.NewGuid().ToString();

        string prehash = $"{requestPath}{method}{timestamp}{nonce}{body}";
        
        Console.WriteLine(prehash);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(prehash));

        string hexString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        byte[] signatureBytes = Encoding.UTF8.GetBytes(hexString);
        string signature = Convert.ToBase64String(signatureBytes);

        return (timestamp, nonce, signature);
    }

    public (string, string, string, string, string) GetHeaders(string method, string requestPath, string body = "")
    {
        var (timestamp, nonce, signature) = GenerateSignature(method, requestPath, body);

        return (
            $"{_apiKey}",
            $"{signature}",
            $"{timestamp}",
            $"{nonce}",
            $"{_passphrase}"
        );
    }
}