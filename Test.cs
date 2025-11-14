using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

public static class YandexTranslate
{
    public static string Translate(string text, string targetLang, string apiKey, string sourceLang = null)
    {
        const string url = "https://translate.api.cloud.yandex.net/translate/v2/translate";

        var request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "POST";
        request.ContentType = "application/json";

        // Передаём API-ключ
        request.Headers.Add("Authorization", "Api-Key " + apiKey);

        // Создаём JSON
        var payload = new
        {
            sourceLanguageCode = sourceLang,   // если null → автодетект
            targetLanguageCode = targetLang,   // например "en"
            texts = new[] { text }             // массив строк
        };

        var json = new JavaScriptSerializer().Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);

        // Записываем тело
        using (var reqStream = request.GetRequestStream())
        {
            reqStream.Write(bytes, 0, bytes.Length);
        }

        // Получаем ответ
        using (var response = (HttpWebResponse)request.GetResponse())
        using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
        {
            string resultJson = reader.ReadToEnd();

            // Пример ответа:
            // {
            //   "translations": [ { "text": "Hello", "detectedLanguageCode": "ru" } ]
            // }

            var responseObj = new JavaScriptSerializer()
                .Deserialize<TranslateResponse>(resultJson);

            return responseObj.translations[0].text;
        }
    }

    private class TranslateResponse
    {
        public Translation[] translations { get; set; }
    }

    private class Translation
    {
        public string text { get; set; }
        public string detectedLanguageCode { get; set; }
    }
}