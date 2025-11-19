using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Yandex.SpeechKit.TtsV3
{
    public class YandexTtsClient
    {
        private readonly string _apiKey;
        private readonly string _folderId;

        // ❗ Правильный URL из документации
        private const string SynthesisUrl =
            "https://tts.api.cloud.yandex.net/tts/v3/utteranceSynthesis";

        public YandexTtsClient(string apiKey, string folderId)
        {
            _apiKey = apiKey;
            _folderId = folderId;
        }

        /// <summary>
        /// Синтез речи (синхронно)
        /// Возвращает (audioBytes, jsonMetadata)
        /// </summary>
        public (byte[] audioBytes, string jsonResponse) Synthesize(
            string text,
            string voice = "oksana",
            string language = "ru-RU",
            string audioFormat = "oggopus",
            bool isSsml = false)
        {
            var requestBody = new SynthesisRequest
            {
                Text = isSsml ? null : text,
                Ssml = isSsml ? text : null,
                OutputAudioSpec = new OutputAudioSpec
                {
                    ContainerAudio = new ContainerAudio
                    {
                        ContainerAudioType = audioFormat
                    }
                },
                Voice = voice,
                LanguageCode = language
            };

            string json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var req = (HttpWebRequest)WebRequest.Create(SynthesisUrl);
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Headers.Add("Authorization", $"Api-Key {_apiKey}");
            req.Headers.Add("x-folder-id", _folderId);

            // передаём тело JSON
            using (var writer = new StreamWriter(req.GetRequestStream()))
                writer.Write(json);

            byte[] audioBytes = null;
            string metaJson = null;

            try
            {
                using var resp = (HttpWebResponse)req.GetResponse();
                using var ms = new MemoryStream();

                resp.GetResponseStream().CopyTo(ms);
                audioBytes = ms.ToArray();

                // В заголовке x-audio-metadata лежат JSON-метаданные
                metaJson = resp.Headers["x-audio-metadata"];
            }
            catch (WebException ex)
            {
                using var reader = new StreamReader(ex.Response.GetResponseStream());
                metaJson = reader.ReadToEnd(); // вернём ошибку
                audioBytes = null;
            }

            return (audioBytes, metaJson);
        }

        /// <summary>
        /// Синтез речи прямо в файл
        /// </summary>
        public string SynthesizeToFile(
            string text,
            string outputPath,
            string voice = "oksana",
            string language = "ru-RU",
            string audioFormat = "oggopus",
            bool isSsml = false)
        {
            var (audio, meta) = Synthesize(text, voice, language, audioFormat, isSsml);

            if (audio != null)
                File.WriteAllBytes(outputPath, audio);

            return meta;
        }
    }

    // ------------------------------
    // Models
    // ------------------------------

    public class SynthesisRequest
    {
        [JsonPropertyName("text")] public string Text { get; set; }
        [JsonPropertyName("ssml")] public string Ssml { get; set; }

        [JsonPropertyName("output_audio_spec")]
        public OutputAudioSpec OutputAudioSpec { get; set; }

        [JsonPropertyName("voice")] public string Voice { get; set; }

        [JsonPropertyName("language_code")]
        public string LanguageCode { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }
    }

    public class OutputAudioSpec
    {
        [JsonPropertyName("containerAudio")]
        public ContainerAudio ContainerAudio { get; set; }
    }

    public class ContainerAudio
    {
        [JsonPropertyName("containerAudioType")]
        public string ContainerAudioType { get; set; }
    }
}
