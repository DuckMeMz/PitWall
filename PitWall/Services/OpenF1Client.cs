using PitWall.Models;
using System.Diagnostics;

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
        List<CarTelementrySample> highSpeedData = await GetCarTelemtryAsync(
            new SessionKey(9158),
            new DriverNumber(55),
            new Filter("speed", ">=", "315"),
            new Filter("n_gear", "==", "8")
        );

        List<Driver> drivers = await GetDriversAsync(
            new SessionKey(9158)
        );
    }

    public async Task<List<CarTelementrySample>> GetCarTelemtryAsync(SessionKey sessionKey, DriverNumber? driver = null, params Filter[] extraFilters)
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

    public async Task<List<IntervalSample>> GetIntervalsAsync(SessionKey sessionKey, DriverNumber? driver = null, params Filter[] extraFilters)
    {
        APIParam query = new APIParam(OpenF1APIEndpoint.Intervals).WithSession(sessionKey);
        if (driver.HasValue) query.WithDriver(driver.Value);
        if (extraFilters.Length > 0) query.WithFilters(extraFilters);
        return await _apiService.FetchDataAsync<IntervalSample>(query);
    }

    public async Task<List<Lap>> GetLapsAsync(SessionKey sessionKey, DriverNumber? driver = null, params Filter[] extraFilters)
    {
        APIParam query = new APIParam(OpenF1APIEndpoint.Laps).WithSession(sessionKey);
        if (driver.HasValue) query.WithDriver(driver.Value);
        if (extraFilters.Length > 0) query.WithFilters(extraFilters);
        return await _apiService.FetchDataAsync<Lap>(query);
    }

    public async Task<List<Location>> GetLocationsAsync(SessionKey sessionKey, DriverNumber? driver = null, params Filter[] extraFilters)
    {
        APIParam query = new APIParam(OpenF1APIEndpoint.Location).WithSession(sessionKey);
        if (driver.HasValue) query.WithDriver(driver.Value);
        if (extraFilters.Length > 0) query.WithFilters(extraFilters);
        return await _apiService.FetchDataAsync<Location>(query);
    }

    public async Task<List<Meeting>> GetMeetingAsync(MeetingKey meetingKey, params Filter[] extraFilters)
    {

    }

    public async Task<List<Overtake>> GetOvertakesAsync(SessionKey sessionKey, DriverNumber? overtakenDriver = null, DriverNumber? overtakingDriver = null, params Filter[] extraFilters)
    {
        APIParam query = new APIParam(OpenF1APIEndpoint.Overtakes).WithSession(sessionKey);
        if (overtakenDriver.HasValue) query.WithDriver(driver.Value);
        if (extraFilters.Length > 0) query.WithFilters(extraFilters);
        return await _apiService.FetchDataAsync<Location>(query);
    }

    public async Task<List<RaceControlMessage>> GetRaceControlAsync(SessionKey session, DriverNumber? driver = null, params Filter[] extraFilters)
    {
        var query = new APIParam(OpenF1APIEndpoint.RaceControl).WithSession(session);
        if (driver.HasValue) query.WithDriver(driver.Value);
        if (extraFilters.Length > 0) query.WithFilters(extraFilters);
        return await _apiService.FetchDataAsync<RaceControlMessage>(query);
    }
}