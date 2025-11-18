public class RecognitionResponse
{
    [JsonPropertyName("result")]
    public RecognitionResult Result { get; set; }
}

public class RecognitionResult
{
    [JsonPropertyName("final")]
    public RecognitionFinal Final { get; set; }

    [JsonPropertyName("finalRefinement")]
    public FinalRefinement FinalRefinement { get; set; }
}

public class RecognitionFinal
{
    [JsonPropertyName("alternatives")]
    public List<RecognitionAlternative> Alternatives { get; set; }
}

public class FinalRefinement
{
    [JsonPropertyName("normalizedText")]
    public NormalizedText NormalizedText { get; set; }
}

public class NormalizedText
{
    [JsonPropertyName("alternatives")]
    public List<RecognitionAlternative> Alternatives { get; set; }
}

public class RecognitionAlternative
{
    [JsonPropertyName("text")]
    public string Text { get; set; }
}


public (string json, string text) GetRecognitionSync(string operationId)
{
    string json = GetRecognitionSyncJson(operationId);

    string text = null;

    try
    {
        var r = JsonSerializer.Deserialize<RecognitionResponse>(json);

        // 1️⃣ обычный финальный текст
        text = r?.Result?.Final?.Alternatives?[0]?.Text;

        if (!string.IsNullOrEmpty(text))
            return (json, text);

        // 2️⃣ нормализованный текст
        text = r?.Result?.FinalRefinement?.NormalizedText?.Alternatives?[0]?.Text;

        if (!string.IsNullOrEmpty(text))
            return (json, text);

        // 3️⃣ если текст отсутсвует (eouUpdate и пр.)
        return (json, null);
    }
    catch
    {
        return (json, null);
    }
}
