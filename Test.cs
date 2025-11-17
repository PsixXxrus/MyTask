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
    // ====================================
    //   Client for REST STT v3 (Async)
    // ====================================
    public class YandexSttClient
    {
        private readonly string _apiKey;
        private readonly string _folderId;

        private const string RecognizeUrl =
            "https://stt.api.cloud.yandex.net/stt/v3/recognizeFileAsync";

        private const string OperationUrl =
            "https://operation.api.cloud.yandex.net/operations/";


        public YandexSttClient(string apiKey, string folderId)
        {
            _apiKey = apiKey;
            _folderId = folderId;
        }

        // -----------------------------
        //    INTERNAL REQUEST CALL
        // -----------------------------
        private async Task<string> SendRequestAsync(object requestObj)
        {
            var json = JsonSerializer.Serialize(requestObj, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            });

            var req = (HttpWebRequest)WebRequest.Create(RecognizeUrl);
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Headers.Add("Authorization", $"Api-Key {_apiKey}");
            req.Headers.Add("x-folder-id", _folderId);

            using (var stream = new StreamWriter(await req.GetRequestStreamAsync()))
                await stream.WriteAsync(json);

            try
            {
                var res = (HttpWebResponse)await req.GetResponseAsync();
                using var reader = new StreamReader(res.GetResponseStream());
                return await reader.ReadToEndAsync();
            }
            catch (WebException ex)
            {
                using var reader = new StreamReader(ex.Response.GetResponseStream());
                string error = await reader.ReadToEndAsync();
                throw new Exception($"Yandex STT error: {error}");
            }
        }


        // ====================================
        //     Public methods
        // ====================================

        /// <summary>
        /// Распознать аудио по URI
        /// </summary>
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


        /// <summary>
        /// Распознать Base64 контент
        /// </summary>
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


        /// <summary>
        /// Распознать локальный файл
        /// </summary>
        public Task<string> RecognizeFromFileAsync(
            string filePath,
            string model = "general",
            string containerType = "WAV")
        {
            var bytes = File.ReadAllBytes(filePath);
            var base64 = Convert.ToBase64String(bytes);

            return RecognizeFromBase64Async(base64, model, containerType);
        }


        /// <summary>
        /// Получить результат операции
        /// </summary>
        public async Task<string> GetOperationResultAsync(string operationId)
        {
            var url = OperationUrl + operationId;

            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.Headers.Add("Authorization", $"Api-Key {_apiKey}");

            using var response = (HttpWebResponse)await req.GetResponseAsync();
            using var reader = new StreamReader(response.GetResponseStream());
            return await reader.ReadToEndAsync();
        }
    }


    // ====================================
    //     MODELS (snake_case!)
    // ====================================

    public class RecognizeFileRequest
    {
        [JsonPropertyName("content")] public string Content { get; set; }
        [JsonPropertyName("uri")] public string Uri { get; set; }

        [JsonPropertyName("recognition_model")]
        public RecognitionModel RecognitionModel { get; set; }

        [JsonPropertyName("recognition_classifier")]
        public RecognitionClassifier RecognitionClassifier { get; set; }

        [JsonPropertyName("speech_analysis")]
        public SpeechAnalysis SpeechAnalysis { get; set; }

        [JsonPropertyName("speaker_labeling")]
        public SpeakerLabeling SpeakerLabeling { get; set; }

        [JsonPropertyName("summarization")]
        public Summarization Summarization { get; set; }
    }


    public class RecognitionModel
    {
        [JsonPropertyName("model")] public string Model { get; set; }

        [JsonPropertyName("audio_format")]
        public AudioFormat AudioFormat { get; set; }

        [JsonPropertyName("text_normalization")]
        public TextNormalization TextNormalization { get; set; }

        [JsonPropertyName("language_restriction")]
        public LanguageRestriction LanguageRestriction { get; set; }

        [JsonPropertyName("audio_processing_type")]
        public string AudioProcessingType { get; set; }
    }


    public class AudioFormat
    {
        [JsonPropertyName("raw_audio")] public RawAudio RawAudio { get; set; }
        [JsonPropertyName("container_audio")] public ContainerAudio ContainerAudio { get; set; }
    }


    public class RawAudio
    {
        [JsonPropertyName("audio_encoding")] public string AudioEncoding { get; set; }
        [JsonPropertyName("sample_rate_hertz")] public string SampleRateHertz { get; set; }
        [JsonPropertyName("audio_channel_count")] public string AudioChannelCount { get; set; }
    }


    public class ContainerAudio
    {
        [JsonPropertyName("container_audio_type")] public string ContainerAudioType { get; set; }
    }


    public class TextNormalization
    {
        [JsonPropertyName("text_normalization")] public string Mode { get; set; }
        [JsonPropertyName("profanity_filter")] public bool? ProfanityFilter { get; set; }
        [JsonPropertyName("literature_text")] public bool? LiteratureText { get; set; }
        [JsonPropertyName("phone_formatting_mode")] public string PhoneFormattingMode { get; set; }
    }


    public class LanguageRestriction
    {
        [JsonPropertyName("restriction_type")] public string RestrictionType { get; set; }
        [JsonPropertyName("language_code")] public List<string> LanguageCode { get; set; }
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
        [JsonPropertyName("enable_speaker_analysis")] public bool? EnableSpeakerAnalysis { get; set; }
        [JsonPropertyName("enable_conversation_analysis")] public bool? EnableConversationAnalysis { get; set; }

        [JsonPropertyName("descriptive_statistics_quantiles")]
        public List<string> DescriptiveStatisticsQuantiles { get; set; }
    }


    public class SpeakerLabeling
    {
        [JsonPropertyName("speaker_labeling")] public string Mode { get; set; }
    }


    public class Summarization
    {
        [JsonPropertyName("model_uri")] public string ModelUri { get; set; }
        [JsonPropertyName("properties")] public List<SummarizationProperty> Properties { get; set; }
    }


    public class SummarizationProperty
    {
        [JsonPropertyName("instruction")] public string Instruction { get; set; }
        [JsonPropertyName("json_object")] public bool? JsonObject { get; set; }
        [JsonPropertyName("json_schema")] public JsonSchemaWrapper JsonSchema { get; set; }
    }


    public class JsonSchemaWrapper
    {
        [JsonPropertyName("schema")] public object Schema { get; set; }
    }
}