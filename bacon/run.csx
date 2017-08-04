#r "Newtonsoft.Json"
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

static HttpClient client = new HttpClient();
public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
{
    var url = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "url", true) == 0)
        .Value;

    var results = await Task.WhenAll(ContainsKevinBacon(url, log), ContainsBacon(url, log));
    var containsKevinBacon = results[0];
    var containsBacon = results[1];

    return new 
    { 
        contains = new
        {
            bacon = containsBacon,
            kevinBacon = containsKevinBacon
        }
    };
}

private static async Task<bool> ContainsKevinBacon(string url, TraceWriter log)
{
    var content = new StringContent(JsonConvert.SerializeObject(new { url }));
    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    content.Headers.Add("Ocp-Apim-Subscription-Key", Env("COMP_VISION_API_KEY"));

    var response = await client.PostAsync(Env("COMP_VISION_API_URL"), content);
    var resultJson = await response.Content.ReadAsStringAsync();

    return Regex.IsMatch(resultJson, @"\bkevin bacon\b", RegexOptions.IgnoreCase);
}

private static async Task<bool> ContainsBacon(string url, TraceWriter log)
{
    var content = new StringContent(JsonConvert.SerializeObject(new { Url = url }));
    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    content.Headers.Add("Prediction-Key", Env("CUSTOM_VISION_API_KEY"));

    var response = await client.PostAsync(Env("CUSTOM_VISION_API_URL"), content);
    var result = JsonConvert.DeserializeObject<CustomVisionResult>(await response.Content.ReadAsStringAsync());

    var baconPrediction = result.Predictions.FirstOrDefault(p => p.Tag == "bacon");
    var baconProbability = baconPrediction?.Probability ?? 0;
    return baconProbability > 0.7m;
}

private class Prediction
{
    public string Tag { get; set; }
    public decimal Probability { get; set; }
}

private class CustomVisionResult
{
    public Prediction[] Predictions { get; set; }
}

public static string Env(string name)
{
    return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
}