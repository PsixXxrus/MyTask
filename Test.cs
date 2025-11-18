public (string json, string text) GetRecognitionSync(string operationId)
{
    if (string.IsNullOrEmpty(operationId))
        throw new ArgumentException("operationId не может быть пустым", nameof(operationId));

    string url = $"https://stt.api.cloud.yandex.net/stt/v3/getRecognition?operation_id={WebUtility.UrlEncode(operationId)}";

    var req = (HttpWebRequest)WebRequest.Create(url);
    req.Method = "GET";
    req.Headers.Add("Authorization", $"Api-Key {_apiKey}");
    req.Headers.Add("x-folder-id", _folderId);

    string json;

    try
    {
        using var resp = (HttpWebResponse)req.GetResponse();
        using var reader = new StreamReader(resp.GetResponseStream());
        json = reader.ReadToEnd();
    }
    catch (WebException ex)
    {
        using var reader = new StreamReader(ex.Response.GetResponseStream());
        string err = reader.ReadToEnd();
        throw new Exception("Yandex STT getRecognition error: " + err);
    }

    // Парсим текст
    string text = null;

    try
    {
        var result = JsonSerializer.Deserialize<RecognitionResponse>(json);
        text = result?.Result?.Alternatives?[0]?.Text;
    }
    catch
    {
        // Если JSON не соответствует ожиданию — text останется null
    }

    return (json, text);
}


public class RecognitionResponse
{
    [JsonPropertyName("result")]
    public RecognitionResult Result { get; set; }
}

public class RecognitionResult
{
    [JsonPropertyName("alternatives")]
    public List<RecognitionAlternative> Alternatives { get; set; }
}

public class RecognitionAlternative
{
    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("confidence")]
    public double? Confidence { get; set; }
}