using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class YandexSpeechKitClient
{
    private readonly HttpClient _httpClient;

    public YandexSpeechKitClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Делает запрос к utteranceSynthesis и возвращает сырой JSON + байты аудио.
    /// </summary>
    public async Task<(string json, byte[] file)> SynthesizeAsync(
        string text,
        string voice,
        string iamToken,
        string folderId,
        CancellationToken cancellationToken = default)
    {
        // URL из доки utteranceSynthesis REST v3
        var url = "https://tts.api.cloud.yandex.net/tts/v3/utteranceSynthesis";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);

        // Авторизация: либо через IAM-токен в Authorization, либо через X-YaCloud-SubjectToken.
        // Оставь тот вариант, который ты уже используешь.
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", iamToken);
        request.Headers.Add("x-folder-id", folderId);

        // Тело строго по доке (camelCase имена полей!):
        var bodyObject = new
        {
            // model можно не указывать, если не используешь кастомные голоса
            // model = "some-model-name",

            text = text,

            hints = new[]
            {
                new
                {
                    voice = voice
                    // Можно добавить speed, volume, role и т.д., но только ОДНО из них одновременно с voice
                }
            },

            outputAudioSpec = new
            {
                // Пример: WAV
                containerAudio = new
                {
                    containerAudioType = "WAV"
                    // варианты: "WAV", "OGG_OPUS", "MP3"
                }
                // либо rawAudio = new { audioEncoding = "LINEAR16_PCM", sampleRateHertz = "48000" }
            },

            // loudnessNormalizationType = "LUFS", // по умолчанию LUFS
            unsafeMode = false
        };

        var jsonBody = JsonSerializer.Serialize(bodyObject);
        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // Здесь ты сразу увидишь, что именно не нравится API (message, details и т.п.)
            throw new InvalidOperationException(
                $"TTS request failed with status {(int)response.StatusCode} ({response.StatusCode}). " +
                $"Body: {responseJson}");
        }

        // Парсим JSON и достаем audioChunk.data (base64)
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        byte[] audioBytes = Array.Empty<byte>();

        if (root.TryGetProperty("audioChunk", out var audioChunkElement) &&
            audioChunkElement.TryGetProperty("data", out var dataElement))
        {
            var base64 = dataElement.GetString();
            if (!string.IsNullOrEmpty(base64))
            {
                audioBytes = Convert.FromBase64String(base64);
            }
        }

        // Возвращаем и сырой JSON, и байты файла
        return (responseJson, audioBytes);
    }
}
