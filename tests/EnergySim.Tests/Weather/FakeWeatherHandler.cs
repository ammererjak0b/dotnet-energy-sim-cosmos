using System.Net;
using System.Text.Json;
using EnergySim.Core.Weather;

namespace EnergySim.Tests.Weather;

/// <summary>Hand-written HTTP fake: answers with a fixed WeatherDto, or fails like a dead weather process.</summary>
internal sealed class FakeWeatherHandler : HttpMessageHandler
{
    public WeatherDto? Answer { get; set; }

    public bool IsDown { get; set; }

    public List<string> RequestedUrls { get; } = new List<string>();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestedUrls.Add(request.RequestUri!.ToString());
        if (IsDown)
        {
            throw new HttpRequestException("connection refused");
        }

        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent(JsonSerializer.Serialize(Answer), System.Text.Encoding.UTF8, "application/json");
        return Task.FromResult(response);
    }
}
