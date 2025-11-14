У меня есть код для NET 5.0+, но я использую C# NET 4.8. Вот код для NET 5.0:
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

	using (var rsa = RSA.Create())
	{
		rsa.ImportFromPem(File.ReadAllText("<файл_закрытого_ключа>").ToCharArray());
		string encodedToken = Jose.JWT.Encode(payload, rsa, JwsAlgorithm.PS256, headers);
	}
}

В NET 4.8 у меня возникает ошибка CS1061: "RSA" не содержит определения "ImportFromPem", и не удалось найти доступный метод расширения "ImportFromPem", принимающий тип "RSA" в качестве первого аргумента (возможно, пропущена директива using или ссылка на сборку) в строке rsa.ImportFromPem(File.ReadAllText("<файл_закрытого_ключа>").ToCharArray());
Нужно исправить, чтобы я мог использовать этот метод в 4.8
