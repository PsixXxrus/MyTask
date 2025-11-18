public (string json, string operationId) RecognizeFromUri(
    string uri,
    string model = "general",
    string containerType = "WAV")
{
    var req = new RecognizeFileRequest
    {
        Uri = uri,
        RecognitionModel = new RecognitionModel
        {
            Model = model,
            AudioFormat = new AudioFormat
            {
                ContainerAudio = new ContainerAudio
                {
                    ContainerAudioType = containerType
                }
            }
        }
    };

    string json = SendRequestSync(req);

    string opId = null;

    try
    {
        var parsed = JsonSerializer.Deserialize<RecognitionInitResponse>(json);
        opId = parsed?.Id; // будет null, если ошибка
    }
    catch
    {
        opId = null;
    }

    return (json, opId);
}




public class RecognitionInitResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("done")]
    public bool? Done { get; set; }
}



private string SendRequestSync(object requestObj)
{
    string json = JsonSerializer.Serialize(requestObj, new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    });

    var req = (HttpWebRequest)WebRequest.Create(RecognizeUrl);
    req.Method = "POST";
    req.ContentType = "application/json";
    req.Headers.Add("Authorization", $"Api-Key {_apiKey}");
    req.Headers.Add("x-folder-id", _folderId);

    using (var writer = new StreamWriter(req.GetRequestStream()))
        writer.Write(json);

    try
    {
        using var resp = (HttpWebResponse)req.GetResponse();
        using var reader = new StreamReader(resp.GetResponseStream());
        return reader.ReadToEnd();
    }
    catch (WebException ex)
    {
        using var reader = new StreamReader(ex.Response.GetResponseStream());
        return reader.ReadToEnd(); // Возвращаем ошибочный JSON тоже
    }
}