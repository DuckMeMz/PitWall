using PitWall.Models;
using PitWall.Models.OpenF1Api;
using System.Diagnostics;

namespace PitWall.Services;

public class OpenF1Client
{
    private readonly OpenF1APIService _apiService;

    public OpenF1Client(OpenF1APIService apiService)
    {
        _apiService = apiService;
    }

    public async Task Test()
    {
        IReadOnlyList<OpenF1Driver> allDrivers = await GetDriversAsync();

        Debug.WriteLine(string.Join("\n", allDrivers.Select(driver => driver)));

    }

    private async Task<IReadOnlyList<T>> FetchEndpointAsync<T>(
        OpenF1APIEndpoint endPoint,
        SessionKey? sessionKey = null,
        MeetingKey? meetingKey = null,
        DriverNumber? driverNumber = null,
        CancellationToken cancellationToken = default,
        params Filter[] extraFilters)
    {
        APIParam query = new(endPoint);

        if (sessionKey.HasValue)
            query.WithSession(sessionKey.Value);

        if (meetingKey.HasValue)
            query.WithMeeting(meetingKey.Value);

        if (driverNumber.HasValue)
            query.WithDriver(driverNumber.Value);

        if (extraFilters.Length > 0)
            query.WithFilters(extraFilters);

        return await _apiService.FetchDataAsync<T>(query, cancellationToken);
    }

    private static SessionKey? DefaultLatestSession(SessionKey? sessionKey, MeetingKey? meetingKey)
    {
        return sessionKey ?? (meetingKey.HasValue ? null : SessionKey.Latest);
    }

    public Task<IReadOnlyList<OpenF1CarTelemetrySample>> GetCarTelemetryAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        params Filter[] extraFilters)
    {
        return GetCarTelemetryAsync(CancellationToken.None, sessionKey, driverNumber, extraFilters);
    }

    public async Task<IReadOnlyList<OpenF1CarTelemetrySample>> GetCarTelemetryAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<OpenF1CarTelemetrySample>(
           endPoint: OpenF1APIEndpoint.CarData,
           sessionKey: sessionKey ?? SessionKey.Latest,
           driverNumber: driverNumber,
           cancellationToken: cancellationToken,
           extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<OpenF1Driver>> GetDriversAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetDriversAsync(CancellationToken.None, sessionKey, driverNumber, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<OpenF1Driver>> GetDriversAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<OpenF1Driver>(
            endPoint: OpenF1APIEndpoint.Drivers,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            driverNumber: driverNumber,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<OpenF1IntervalSample>> GetIntervalsAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetIntervalsAsync(CancellationToken.None, sessionKey, driverNumber, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<OpenF1IntervalSample>> GetIntervalsAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<OpenF1IntervalSample>(
            endPoint: OpenF1APIEndpoint.Intervals,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            driverNumber: driverNumber,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<OpenF1Lap>> GetLapsAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetLapsAsync(CancellationToken.None, sessionKey, driverNumber, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<OpenF1Lap>> GetLapsAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<OpenF1Lap>(
            endPoint: OpenF1APIEndpoint.Laps,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            driverNumber: driverNumber,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<OpenF1Location>> GetLocationsAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetLocationsAsync(CancellationToken.None, sessionKey, driverNumber, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<OpenF1Location>> GetLocationsAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<OpenF1Location>(
            endPoint: OpenF1APIEndpoint.Location,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            driverNumber: driverNumber,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<OpenF1Meeting>> GetMeetingsAsync(
        int? year = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetMeetingsAsync(CancellationToken.None, year, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<OpenF1Meeting>> GetMeetingsAsync(
        CancellationToken cancellationToken,
        int? year = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        List<Filter> filters = [.. extraFilters];

        if (year.HasValue)
            filters.Add(Filter.Equal(MeetingFields.Year, year));

        return await FetchEndpointAsync<OpenF1Meeting>(
            endPoint: OpenF1APIEndpoint.Meetings,
            meetingKey: meetingKey,
            cancellationToken: cancellationToken,
            extraFilters: [.. filters]);
    }

    public Task<IReadOnlyList<OpenF1Overtake>> GetOvertakesAsync(
        SessionKey? sessionKey = null,
        MeetingKey? meetingKey = null,
        DriverNumber? overtakingDriverNumber = null,
        DriverNumber? overtakenDriverNumber = null,
        params Filter[] extraFilters)
    {
        return GetOvertakesAsync(
            CancellationToken.None,
            sessionKey,
            meetingKey,
            overtakingDriverNumber,
            overtakenDriverNumber,
            extraFilters);
    }

    public async Task<IReadOnlyList<OpenF1Overtake>> GetOvertakesAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        MeetingKey? meetingKey = null,
        DriverNumber? overtakingDriverNumber = null,
        DriverNumber? overtakenDriverNumber = null,
        params Filter[] extraFilters)
    {
        List<Filter> filters = [.. extraFilters];

        if (overtakingDriverNumber.HasValue)
            filters.Add(Filter.Equal(OvertakeFields.OvertakingDriverNumber, overtakingDriverNumber.Value));

        if (overtakenDriverNumber.HasValue)
            filters.Add(Filter.Equal(OvertakeFields.OvertakenDriverNumber, overtakenDriverNumber.Value));

        return await FetchEndpointAsync<OpenF1Overtake>(
            endPoint: OpenF1APIEndpoint.Overtakes,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            cancellationToken: cancellationToken,
            extraFilters: [.. filters]);
    }

    public Task<IReadOnlyList<OpenF1PitInfo>> GetPitAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetPitAsync(CancellationToken.None, sessionKey, driverNumber, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<OpenF1PitInfo>> GetPitAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<OpenF1PitInfo>(
            endPoint: OpenF1APIEndpoint.Pit,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            driverNumber: driverNumber,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<OpenF1PositionUpdate>> GetPositionsAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetPositionsAsync(CancellationToken.None, sessionKey, driverNumber, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<OpenF1PositionUpdate>> GetPositionsAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<OpenF1PositionUpdate>(
            endPoint: OpenF1APIEndpoint.Position,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            driverNumber: driverNumber,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<OpenF1RaceControlMessage>> GetRaceControlAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetRaceControlAsync(CancellationToken.None, sessionKey, driverNumber, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<OpenF1RaceControlMessage>> GetRaceControlAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<OpenF1RaceControlMessage>(
            endPoint: OpenF1APIEndpoint.RaceControl,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            driverNumber: driverNumber,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<OpenF1Session>> GetSessionsAsync(
        int? year = null,
        MeetingKey? meetingKey = null,
        SessionKey? sessionKey = null,
        params Filter[] extraFilters)
    {
        return GetSessionsAsync(CancellationToken.None, year, meetingKey, sessionKey, extraFilters);
    }

    public async Task<IReadOnlyList<OpenF1Session>> GetSessionsAsync(
        CancellationToken cancellationToken,
        int? year = null,
        MeetingKey? meetingKey = null,
        SessionKey? sessionKey = null,
        params Filter[] extraFilters)
    {
        List<Filter> filters = [.. extraFilters];

        if (year.HasValue)
            filters.Add(Filter.Equal(SessionFields.Year, year));

        return await FetchEndpointAsync<OpenF1Session>(
            endPoint: OpenF1APIEndpoint.Sessions,
            sessionKey: sessionKey,
            meetingKey: meetingKey,
            cancellationToken: cancellationToken,
            extraFilters: [.. filters]);
    }

    public Task<IReadOnlyList<OpenF1RaceResult>> GetRaceResultsAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetRaceResultsAsync(CancellationToken.None, sessionKey, driverNumber, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<OpenF1RaceResult>> GetRaceResultsAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<OpenF1RaceResult>(
            endPoint: OpenF1APIEndpoint.SessionResult,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            driverNumber: driverNumber,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<OpenF1QualifyingResult>> GetQualifyingResultsAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetQualifyingResultsAsync(CancellationToken.None, sessionKey, driverNumber, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<OpenF1QualifyingResult>> GetQualifyingResultsAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<OpenF1QualifyingResult>(
            endPoint: OpenF1APIEndpoint.SessionResult,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            driverNumber: driverNumber,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<OpenF1StartingGridEntry>> GetStartingGridAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetStartingGridAsync(CancellationToken.None, sessionKey, driverNumber, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<OpenF1StartingGridEntry>> GetStartingGridAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<OpenF1StartingGridEntry>(
            endPoint: OpenF1APIEndpoint.StartingGrid,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            driverNumber: driverNumber,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<OpenF1Stint>> GetStintsAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetStintsAsync(CancellationToken.None, sessionKey, driverNumber, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<OpenF1Stint>> GetStintsAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<OpenF1Stint>(
            endPoint: OpenF1APIEndpoint.Stints,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            driverNumber: driverNumber,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<OpenF1TeamRadio>> GetTeamRadioAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetTeamRadioAsync(CancellationToken.None, sessionKey, driverNumber, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<OpenF1TeamRadio>> GetTeamRadioAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<OpenF1TeamRadio>(
            endPoint: OpenF1APIEndpoint.TeamRadio,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            driverNumber: driverNumber,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<OpenF1Weather>> GetWeatherAsync(
        SessionKey? sessionKey = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetWeatherAsync(CancellationToken.None, sessionKey, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<OpenF1Weather>> GetWeatherAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<OpenF1Weather>(
            endPoint: OpenF1APIEndpoint.Weather,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }
}
