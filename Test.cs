public (string json, byte[] file) SynthesizeStream(string text)
{
    var requestBody = new
    {
        text = text,
        output_audio_spec = new
        {
            containerAudio = new
            {
                containerAudioType = "wav"
            }
        },
        loudnessNormalizationType = "none",
        voice = "oksana",
        language_code = "ru-RU"
    };

    string jsonReq = JsonSerializer.Serialize(requestBody);

    var req = (HttpWebRequest)WebRequest.Create("https://tts.api.cloud.yandex.net/tts/v3/utteranceSynthesis");
    req.Method = "POST";
    req.ContentType = "application/json";
    req.Headers.Add("Authorization", $"Api-Key {_apiKey}");
    req.Headers.Add("x-folder-id", _folderId);

    using (var writer = new StreamWriter(req.GetRequestStream()))
        writer.Write(jsonReq);

    string fullJson = "";
    List<byte[]> chunks = new List<byte[]>();

    try
    {
        using var resp = (HttpWebResponse)req.GetResponse();
        using var reader = new StreamReader(resp.GetResponseStream());

        string raw = reader.ReadToEnd();
        fullJson = raw;

        // Разбиваем склеенные JSON-объекты
        var objects = SplitJsonObjects(raw);

        foreach (var obj in objects)
        {
            try
            {
                var part = JsonSerializer.Deserialize<TtsStreamChunk>(obj);

                string base64 = part?.AudioChunk?.Data;
                if (!string.IsNullOrEmpty(base64))
                {
                    chunks.Add(Convert.FromBase64String(base64));
                }
            }
            catch
            {
                // игнорируем битые объекты
            }
        }
    }
    catch (WebException ex)
    {
        using var reader = new StreamReader(ex.Response.GetResponseStream());
        fullJson = reader.ReadToEnd();
        return (fullJson, null);
    }

    // Склеиваем все куски
    byte[] file = CombineChunks(chunks);

    return (fullJson, file);
}


public class TtsStreamChunk
{
    [JsonPropertyName("audioChunk")]
    public AudioChunk AudioChunk { get; set; }

    [JsonPropertyName("textChunk")]
    public TextChunk TextChunk { get; set; }
}

public class AudioChunk
{
    [JsonPropertyName("data")]
    public string Data { get; set; }
}

public class TextChunk
{
    [JsonPropertyName("text")]
    public string Text { get; set; }
}


private List<string> SplitJsonObjects(string json)
{
    var list = new List<string>();
    int depth = 0;
    int start = -1;

    for (int i = 0; i < json.Length; i++)
    {
        if (json[i] == '{')
        {
            if (depth == 0)
                start = i;
            depth++;
        }
        else if (json[i] == '}')
        {
            depth--;
            if (depth == 0 && start != -1)
            {
                list.Add(json.Substring(start, i - start + 1));
                start = -1;
            }
        }
    }

    return list;
}


private byte[] CombineChunks(List<byte[]> chunks)
{
    using var ms = new MemoryStream();
    foreach (var c in chunks)
        ms.Write(c, 0, c.Length);
    return ms.ToArray();
}
