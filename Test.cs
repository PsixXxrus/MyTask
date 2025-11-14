using System;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;

public static class YandexS3Uploader
{
    public static string UploadFile(
        string accessKeyId,
        string secretAccessKey,
        string bucketName,
        string objectKey,
        string localFilePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Yandex S3 Upload ===");
        sb.AppendLine($"Bucket: {bucketName}");
        sb.AppendLine($"Key:    {objectKey}");
        sb.AppendLine($"File:   {localFilePath}");

        // Конфиг клиента для Yandex Object Storage
        var config = new AmazonS3Config
        {
            // Рекомендованный endpoint для AWS SDK .NET
            ServiceURL = "https://s3.yandexcloud.net",
            ForcePathStyle = true // на всякий случай, чтобы не было проблем с виртуальными хостами
        };

        // Credentials – это статический ключ сервиса Yandex (key_id + secret)
        var creds = new BasicAWSCredentials(accessKeyId, secretAccessKey);

        using (var client = new AmazonS3Client(creds, config))
        {
            try
            {
                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = objectKey,
                    FilePath = localFilePath
                };

                // Синхронная обёртка вокруг async-метода
                PutObjectResponse response = client.PutObjectAsync(request)
                                                   .GetAwaiter()
                                                   .GetResult();

                sb.AppendLine("Status: SUCCESS");
                sb.AppendLine($"HTTP Status Code: {(int)response.HttpStatusCode} {response.HttpStatusCode}");
                sb.AppendLine($"ETag: {response.ETag}");
                sb.AppendLine($"RequestId: {response.ResponseMetadata?.RequestId}");
            }
            catch (AmazonS3Exception ex)
            {
                sb.AppendLine("Status: ERROR (AmazonS3Exception)");
                sb.AppendLine($"HTTP Status Code: {(int)ex.StatusCode} {ex.StatusCode}");
                sb.AppendLine($"ErrorCode: {ex.ErrorCode}");
                sb.AppendLine($"Message: {ex.Message}");
                sb.AppendLine($"RequestId: {ex.RequestId}");
            }
            catch (Exception ex)
            {
                sb.AppendLine("Status: ERROR (General)");
                sb.AppendLine(ex.ToString());
            }
        }

        return sb.ToString();
    }
}