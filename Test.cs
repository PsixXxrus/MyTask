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


public (string json, string text) GetRecognitionSync(string operationId)
{
    string json = GetRecognitionSyncJson(operationId);
    string finalText = null;

    var objects = SplitJsonObjects(json);

    foreach (var obj in objects)
    {
        try
        {
            var r = JsonSerializer.Deserialize<RecognitionResponse>(obj);

            // финальный текст
            var t1 = r?.Final?.Alternatives?[0]?.Text;
            if (!string.IsNullOrEmpty(t1))
                finalText = t1;

            // нормализованный финальный текст
            var t2 = r?.FinalRefinement?.NormalizedText?.Alternatives?[0]?.Text;
            if (!string.IsNullOrEmpty(t2))
                finalText = t2;
        }
        catch
        {
            // пропускаем некорректный json
        }
    }

    return (json, finalText);
}


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

