// =============================================================
//   SYNCHRONOUS VERSIONS (no async/await)
// =============================================================
public string RecognizeFromUri(string uri, string model = "general", string containerType = "WAV")
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

    return SendRequestSync(req);
}

public string RecognizeFromBase64(string base64, string model = "general", string containerType = "WAV")
{
    var req = new RecognizeFileRequest
    {
        Content = base64,
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

    return SendRequestSync(req);
}

public string RecognizeFromFile(string filePath, string model = "general", string containerType = "WAV")
{
    byte[] bytes = File.ReadAllBytes(filePath);
    string base64 = Convert.ToBase64String(bytes);

    return RecognizeFromBase64(base64, model, containerType);
}


// ==================================================================
//        INTERNAL SYNC SENDER (без async вообще)
// ==================================================================
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
        string err = reader.ReadToEnd();
        throw new Exception("Yandex STT error: " + err);
    }
}