using System;
using System.IO;
using System.Net;
using System.Text;

public static class YandexIamClient
{
    public static string RequestIamToken(string jwt, string proxyHost, int proxyPort, string proxyUser = null, string proxyPass = null)
    {
        string url = "https://iam.api.cloud.yandex.net/iam/v1/tokens";

        // Прокси
        var proxy = new WebProxy(proxyHost, proxyPort);

        if (!string.IsNullOrEmpty(proxyUser))
        {
            proxy.Credentials = new NetworkCredential(proxyUser, proxyPass);
        }

        // Создаём запрос
        var request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "POST";
        request.ContentType = "application/json";
        request.Proxy = proxy;

        string data = "{\"jwt\":\"" + jwt + "\"}";
        byte[] bytes = Encoding.UTF8.GetBytes(data);

        // Пишем тело запроса
        using (var reqStream = request.GetRequestStream())
        {
            reqStream.Write(bytes, 0, bytes.Length);
        }

        // Читаем ответ
        using (var response = (HttpWebResponse)request.GetResponse())
        using (var sr = new StreamReader(response.GetResponseStream()))
        {
            return sr.ReadToEnd();
        }
    }
}