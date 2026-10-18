using EnergySim.Core.Weather;

namespace EnergySim.Weather;

/// <summary>In-process IWeatherClient on top of WeatherModel. A weather service client can replace it later.</summary>
public sealed class LocalWeatherClient : IWeatherClient
{
    private readonly WeatherModel _model;

    public LocalWeatherClient(WeatherModel model)
    {
        if (model == null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        _model = model;
    }

    public Task<WeatherResult> getWeatherAsync(string locationId, DateTimeOffset virtualTimeUtc, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        WeatherResult result = new WeatherResult();
        try
        {
            result.Weather = _model.getWeather(locationId, virtualTimeUtc);
        }
        catch (UnknownLocationException exception)
        {
            result.ErrorMessage = exception.Message; // loop shows it, device gets no weather
        }
        catch (WeatherDataException exception)
        {
            result.ErrorMessage = exception.Message;
        }

        return Task.FromResult(result);
    }
}
