public class RecognitionEnvelope
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
    string finalText = null;

    // Разделяем склеенные JSON-объекты
    var jsonParts = SplitJsonObjects(json);

    foreach (var part in jsonParts)
    {
        try
        {
            var r = JsonSerializer.Deserialize<RecognitionEnvelope>(part);
            var result = r?.Result;

            // 1) final
            var t1 = result?.Final?.Alternatives?[0]?.Text;
            if (!string.IsNullOrEmpty(t1))
                finalText = t1;

            // 2) finalRefinement
            var t2 = result?.FinalRefinement?.NormalizedText?.Alternatives?[0]?.Text;
            if (!string.IsNullOrEmpty(t2))
                finalText = t2;
        }
        catch
        {
            // игнорируем битые json-объекты
        }
    }

    return (json, finalText);
}
