using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Yandex.SpeechKit.SttV3
{
    // =============================
    //        CLIENT (HttpWebRequest)
    // =============================
    public class YandexSttClient
    {
        private readonly string _apiKey;
        private readonly string _folderId;

        public YandexSttClient(string apiKey, string folderId)
        {
            _apiKey = apiKey;
            _folderId = folderId;
        }

        private async Task<string> SendRequestAsync(RecognizeFileRequest request)
        {
            string json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var url = "https://stt.api.cloud.yandex.net/speech/v3/recognizers:recognizeFile";

            var http = (HttpWebRequest)WebRequest.Create(url);
            http.Method = "POST";
            http.ContentType = "application/json";
            http.Headers.Add("Authorization", $"Api-Key {_apiKey}");
            http.Headers.Add("x-folder-id", _folderId);

            using (var stream = new StreamWriter(await http.GetRequestStreamAsync()))
            {
                await stream.WriteAsync(json);
            }

            try
            {
                using var response = (HttpWebResponse)await http.GetResponseAsync();
                using var reader = new StreamReader(response.GetResponseStream());
                return await reader.ReadToEndAsync();
            }
            catch (WebException ex)
            {
                using var reader = new StreamReader(ex.Response.GetResponseStream());
                string error = await reader.ReadToEndAsync();
                throw new Exception($"Yandex STT error: {error}");
            }
        }

        // =============================
        //       URI (ссылка)
        // =============================
        public Task<string> RecognizeFromUriAsync(
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

            return SendRequestAsync(req);
        }

        // =============================
        //       BASE64 (контент)
        // =============================
        public Task<string> RecognizeFromBase64Async(
            string base64,
            string model = "general",
            string containerType = "WAV")
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

            return SendRequestAsync(req);
        }

        // =============================
        //       LOCAL FILE → BASE64
        // =============================
        public Task<string> RecognizeFromFileAsync(
            string filePath,
            string model = "general",
            string containerType = "WAV")
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            string base64 = Convert.ToBase64String(bytes);

            return RecognizeFromBase64Async(base64, model, containerType);
        }
    }

    // =============================
    //          MODELS
    // =============================

    public class RecognizeFileRequest
    {
        [JsonPropertyName("content")] public string Content { get; set; }
        [JsonPropertyName("uri")] public string Uri { get; set; }

        [JsonPropertyName("recognitionModel")]
        public RecognitionModel RecognitionModel { get; set; }

        [JsonPropertyName("recognitionClassifier")]
        public RecognitionClassifier RecognitionClassifier { get; set; }

        [JsonPropertyName("speechAnalysis")]
        public SpeechAnalysis SpeechAnalysis { get; set; }

        [JsonPropertyName("speakerLabeling")]
        public SpeakerLabeling SpeakerLabeling { get; set; }

        [JsonPropertyName("summarization")]
        public Summarization Summarization { get; set; }
    }

    public class RecognitionModel
    {
        [JsonPropertyName("model")] public string Model { get; set; }

        [JsonPropertyName("audioFormat")] public AudioFormat AudioFormat { get; set; }

        [JsonPropertyName("textNormalization")]
        public TextNormalization TextNormalization { get; set; }

        [JsonPropertyName("languageRestriction")]
        public LanguageRestriction LanguageRestriction { get; set; }

        [JsonPropertyName("audioProcessingType")]
        public string AudioProcessingType { get; set; }
    }

    public class AudioFormat
    {
        [JsonPropertyName("rawAudio")] public RawAudio RawAudio { get; set; }
        [JsonPropertyName("containerAudio")] public ContainerAudio ContainerAudio { get; set; }
    }

    public class RawAudio
    {
        [JsonPropertyName("audioEncoding")] public string AudioEncoding { get; set; }
        [JsonPropertyName("sampleRateHertz")] public string SampleRateHertz { get; set; }
        [JsonPropertyName("audioChannelCount")] public string AudioChannelCount { get; set; }
    }

    public class ContainerAudio
    {
        [JsonPropertyName("containerAudioType")] public string ContainerAudioType { get; set; }
    }

    public class TextNormalization
    {
        [JsonPropertyName("textNormalization")] public string Mode { get; set; }
        [JsonPropertyName("profanityFilter")] public bool? ProfanityFilter { get; set; }
        [JsonPropertyName("literatureText")] public bool? LiteratureText { get; set; }
        [JsonPropertyName("phoneFormattingMode")] public string PhoneFormattingMode { get; set; }
    }

    public class LanguageRestriction
    {
        [JsonPropertyName("restrictionType")] public string RestrictionType { get; set; }
        [JsonPropertyName("languageCode")] public List<string> LanguageCode { get; set; }
    }

    public class RecognitionClassifier
    {
        [JsonPropertyName("classifiers")] public List<ClassifierItem> Classifiers { get; set; }
    }

    public class ClassifierItem
    {
        [JsonPropertyName("classifier")] public string Classifier { get; set; }
        [JsonPropertyName("triggers")] public List<string> Triggers { get; set; }
    }

    public class SpeechAnalysis
    {
        [JsonPropertyName("enableSpeakerAnalysis")] public bool? EnableSpeakerAnalysis { get; set; }
        [JsonPropertyName("enableConversationAnalysis")] public bool? EnableConversationAnalysis { get; set; }

        [JsonPropertyName("descriptiveStatisticsQuantiles")]
        public List<string> DescriptiveStatisticsQuantiles { get; set; }
    }

    public class SpeakerLabeling
    {
        [JsonPropertyName("speakerLabeling")] public string Mode { get; set; }
    }

    public class Summarization
    {
        [JsonPropertyName("modelUri")] public string ModelUri { get; set; }
        [JsonPropertyName("properties")] public List<SummarizationProperty> Properties { get; set; }
    }

    public class SummarizationProperty
    {
        [JsonPropertyName("instruction")] public string Instruction { get; set; }
        [JsonPropertyName("jsonObject")] public bool? JsonObject { get; set; }
        [JsonPropertyName("jsonSchema")] public JsonSchemaWrapper JsonSchema { get; set; }
    }

    public class JsonSchemaWrapper
    {
        [JsonPropertyName("schema")] public object Schema { get; set; }
    }
}