/// <summary>
/// Получить результат распознавания по operationId (синхронно)
/// </summary>
/// <param name="operationId">ID операции распознавания</param>
/// <returns>Ответ JSON сервиса</returns>
public string GetRecognitionSync(string operationId)
{
    if (string.IsNullOrEmpty(operationId))
        throw new ArgumentException("operationId не может быть пустым", nameof(operationId));

    // URL метода
    string url = $"https://stt.api.cloud.yandex.net/stt/v3/getRecognition?operation_id={WebUtility.UrlEncode(operationId)}";

    var req = (HttpWebRequest)WebRequest.Create(url);
    req.Method = "GET";
    req.Headers.Add("Authorization", $"Api-Key {_apiKey}");
    req.Headers.Add("x-folder-id", _folderId);

    try
    {
        using var resp = (HttpWebResponse)req.GetResponse();
        using var reader = new StreamReader(resp.GetResponseStream());
        return reader.ReadToEnd();
    }
    catch (WebException ex)
    {
        using var reader = new StreamReader(ex.Response.GetResponseStream());
        string err = reader.ReadToEnd();
        throw new Exception("Yandex STT getRecognition error: " + err);
    }
}