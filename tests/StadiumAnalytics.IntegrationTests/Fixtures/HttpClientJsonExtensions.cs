using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StadiumAnalytics.IntegrationTests.Fixtures;

internal static class HttpClientJsonExtensions
{
    public static readonly JsonSerializerOptions JsonOptions = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var o = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        o.Converters.Add(new JsonStringEnumConverter());
        return o;
    }

    public static Task<HttpResponseMessage> PostJsonAsync<TBody>(
        this HttpClient client, string url, TBody body, CancellationToken ct = default)
        => client.PostAsJsonAsync(url, body, JsonOptions, ct);

    public static Task<HttpResponseMessage> PostRawAsync(
        this HttpClient client, string url, string rawBody,
        string contentType = "application/json", CancellationToken ct = default)
        => client.PostAsync(url, new StringContent(rawBody, Encoding.UTF8, contentType), ct);

    public static Task<TValue?> ReadJsonAsync<TValue>(
        this HttpContent content, CancellationToken ct = default)
        => content.ReadFromJsonAsync<TValue>(JsonOptions, ct);
}
