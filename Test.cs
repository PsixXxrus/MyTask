public (string json, bool result) GetOperationStatusSync(string operationId)
{
    if (string.IsNullOrEmpty(operationId))
        throw new ArgumentException("operationId не может быть пустым", nameof(operationId));

    string url = OperationUrl + operationId;

    var req = (HttpWebRequest)WebRequest.Create(url);
    req.Method = "GET";
    req.Headers.Add("Authorization", $"Api-Key {_apiKey}");

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
        throw new Exception("Yandex Operation API error: " + err);
    }

    // Парсинг поля "done"
    bool done = false;

    try
    {
        var data = JsonSerializer.Deserialize<OperationStatusResponse>(json);
        done = data?.Done ?? false;
    }
    catch
    {
        // Если не удалось разобрать JSON — result остаётся false
    }

    return (json, done);
}


public class OperationStatusResponse
{
    [JsonPropertyName("done")]
    public bool Done { get; set; }
}