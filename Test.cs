public class RecognitionResponse
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

        // попытка 1: final
        text = r?.Final?.Alternatives?[0]?.Text;
        if (!string.IsNullOrEmpty(text))
            return (json, text);

        // попытка 2: finalRefinement.normalizedText
        text = r?.FinalRefinement?.NormalizedText?.Alternatives?[0]?.Text;
        if (!string.IsNullOrEmpty(text))
            return (json, text);

        // если ни одного — остаётся null
        return (json, null);
    }
    catch
    {
        return (json, null);
    }
}
