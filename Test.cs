using System;
using System.IO;
using System.Net;
using System.Text;

public static class YandexObjectStorage
{
    public static string UploadFile(
        string apiKey,
        string bucketName,
        string objectPath,
        string localFilePath)
    {
        string url = $"https://storage.yandexcloud.net/{bucketName}/{objectPath}";
        var sb = new StringBuilder();

        try
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "PUT";
            request.ContentType = "application/octet-stream";

            // Авторизация через API-ключ
            request.Headers.Add("Authorization", "Api-Key " + apiKey);

            // Хост
            request.Host = "storage.yandexcloud.net";

            // Загружаем файл
            byte[] fileBytes = File.ReadAllBytes(localFilePath);
            request.ContentLength = fileBytes.Length;

            using (Stream reqStream = request.GetRequestStream())
                reqStream.Write(fileBytes, 0, fileBytes.Length);

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                string body = reader.ReadToEnd();

                sb.AppendLine("=== SUCCESS RESPONSE ===");
                sb.AppendLine($"URL: {url}");
                sb.AppendLine($"Status: {(int)response.StatusCode} {response.StatusCode}");
                sb.AppendLine("Headers:");

                foreach (string header in response.Headers.AllKeys)
                    sb.AppendLine($"  {header}: {response.Headers[header]}");

                sb.AppendLine("Body:");
                sb.AppendLine(body);
            }
        }
        catch (WebException ex)
        {
            sb.AppendLine("=== ERROR RESPONSE ===");
            sb.AppendLine($"URL: {url}");

            if (ex.Response is HttpWebResponse errorResponse)
            {
                sb.AppendLine($"Status: {(int)errorResponse.StatusCode} {errorResponse.StatusCode}");
                sb.AppendLine("Headers:");

                foreach (string header in errorResponse.Headers.AllKeys)
                    sb.AppendLine($"  {header}: {errorResponse.Headers[header]}");

                using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                {
                    string body = reader.ReadToEnd();
                    sb.AppendLine("Body:");
                    sb.AppendLine(body);
                }
            }
            else
            {
                sb.AppendLine("No HTTP response available.");
                sb.AppendLine(ex.ToString());
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine("=== UNKNOWN ERROR ===");
            sb.AppendLine(ex.ToString());
        }

        return sb.ToString();
    }
}