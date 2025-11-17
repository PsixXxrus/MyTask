public string GetOperationResult(string operationId)
{
    var req = (HttpWebRequest)WebRequest.Create(OperationUrl + operationId);
    req.Method = "GET";
    req.Headers.Add("Authorization", $"Api-Key {_apiKey}");

    using var resp = (HttpWebResponse)req.GetResponse();
    using var reader = new StreamReader(resp.GetResponseStream());
    return reader.ReadToEnd();
}