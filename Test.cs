using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

class Program
{
    static void Main(string[] args)
    {
        var serviceAccountId = "<идентификатор_сервисного_аккаунта>";
        var keyId = "<идентификатор_открытого_ключа>";
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var headers = new Dictionary<string, object>()
        {
            { "kid", keyId }
        };

        var payload = new Dictionary<string, object>()
        {
            { "aud", "https://iam.api.cloud.yandex.net/iam/v1/tokens" },
            { "iss", serviceAccountId },
            { "iat", now },
            { "exp", now + 3600 }
        };

        string pem = File.ReadAllText("<файл_закрытого_ключа>");

        using (RSA rsa = CreateRsaFromPem(pem))
        {
            string encodedToken = Jose.JWT.Encode(payload, rsa, JwsAlgorithm.PS256, headers);
            Console.WriteLine(encodedToken);
        }
    }

    public static RSA CreateRsaFromPem(string pem)
    {
        pem = pem.Trim();

        var rsa = RSA.Create();

        if (pem.Contains("BEGIN PRIVATE KEY")) // PKCS#8
        {
            byte[] keyData = GetBytesFromPem(pem, "PRIVATE KEY");
            rsa.ImportPkcs8PrivateKey(keyData, out _);
        }
        else if (pem.Contains("BEGIN RSA PRIVATE KEY")) // PKCS#1
        {
            byte[] keyData = GetBytesFromPem(pem, "RSA PRIVATE KEY");
            rsa.ImportRSAPrivateKey(keyData, out _);
        }
        else
        {
            throw new Exception("Неизвестный формат PEM ключа");
        }

        return rsa;
    }

    private static byte[] GetBytesFromPem(string pem, string section)
    {
        string header = $"-----BEGIN {section}-----";
        string footer = $"-----END {section}-----";

        var lines = pem
            .Replace(header, "")
            .Replace(footer, "")
            .Replace("\r", "")
            .Replace("\n", "");

        return Convert.FromBase64String(lines);
    }
}