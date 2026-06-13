using PitWall.Models;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PitWall.Services;

public class OpenF1Client
{
    private readonly OpenF1APIService _apiService = new(new System.Net.Http.HttpClient());

    public OpenF1Client(OpenF1APIService apiService)
    {
        _apiService = apiService;
    }

    public async Task Test()
    {
        List<CarTelementrySample> sainzCar = await GetCarTelementryAsync(
            new SessionKey(9158), 
            new DriverNumber(55), 
            Filter.GreaterThanOrEqual(CarTelementrySampleFields.Speed, 315)
        );
        System.Diagnostics.Debug.WriteLine($"sainzCar count: {sainzCar.Count}");

        List<Driver> allDrivers = await GetDriversAsync(
            new SessionKey(9158)
        );
        System.Diagnostics.Debug.WriteLine($"allDrivers count: {allDrivers.Count}");

        List<Driver> sainz = await GetDriversAsync(
            new SessionKey(9158),
            new DriverNumber(55)
        );
        System.Diagnostics.Debug.WriteLine($"sainz count: {sainz.Count}");
    }

    public async Task<List<CarTelementrySample>> GetCarTelementryAsync(SessionKey sessionKey, DriverNumber? driver = null, params Filter[] extraFilters)
    {
        APIParam query = new APIParam(OpenF1APIEndpoint.CarData).WithSession(sessionKey);

        if (driver.HasValue) query.WithDriver(driver.Value);
        if (extraFilters.Length > 0) query.WithFilters(extraFilters);

        return await _apiService.FetchDataAsync<CarTelementrySample>(query);
    }

    public async Task<List<Driver>> GetDriversAsync(SessionKey sessionKey, DriverNumber? driver = null, params Filter[] extraFilters)
    {
        APIParam query = new APIParam(OpenF1APIEndpoint.Drivers).WithSession(sessionKey);

        if (driver.HasValue) query.WithDriver(driver.Value);
        if (extraFilters.Length > 0) query.WithFilters(extraFilters);

        return await _apiService.FetchDataAsync<Driver>(query);
    }
}