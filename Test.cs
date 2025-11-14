using System;
using System.IO;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

public static class RsaPemLoader
{
    public static RSA LoadPrivateKey(string pem)
    {
        using (var str = new StringReader(pem))
        {
            var reader = new PemReader(str);
            object keyObject = reader.ReadObject();

            AsymmetricKeyParameter privateKey;

            // Ключ может быть как парой (public+private), так и просто private
            if (keyObject is AsymmetricCipherKeyPair keyPair)
                privateKey = keyPair.Private;
            else
                privateKey = (AsymmetricKeyParameter)keyObject;

            var rsaParams = DotNetUtilities.ToRSAParameters(
                (RsaPrivateCrtKeyParameters)privateKey);

            var rsa = RSA.Create(); // в .NET Framework вернёт RSACryptoServiceProvider/RSACng
            rsa.ImportParameters(rsaParams);
            return rsa;
        }
    }
}



using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Jose;

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

        using (RSA rsa = RsaPemLoader.LoadPrivateKey(pem))
        {
            string encodedToken = JWT.Encode(payload, rsa, JwsAlgorithm.PS256, headers);
            Console.WriteLine(encodedToken);
        }
    }
}