using PitWall.Models;
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
        IReadOnlyList<Driver> allDrivers = await GetDriversAsync();

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

    public Task<IReadOnlyList<CarTelemetrySample>> GetCarTelemetryAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        params Filter[] extraFilters)
    {
        return GetCarTelemetryAsync(CancellationToken.None, sessionKey, driverNumber, extraFilters);
    }

    public async Task<IReadOnlyList<CarTelemetrySample>> GetCarTelemetryAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<CarTelemetrySample>(
           endPoint: OpenF1APIEndpoint.CarData,
           sessionKey: sessionKey ?? SessionKey.Latest,
           driverNumber: driverNumber,
           cancellationToken: cancellationToken,
           extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<Driver>> GetDriversAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetDriversAsync(CancellationToken.None, sessionKey, driverNumber, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<Driver>> GetDriversAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<Driver>(
            endPoint: OpenF1APIEndpoint.Drivers,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            driverNumber: driverNumber,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<IntervalSample>> GetIntervalsAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetIntervalsAsync(CancellationToken.None, sessionKey, driverNumber, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<IntervalSample>> GetIntervalsAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<IntervalSample>(
            endPoint: OpenF1APIEndpoint.Intervals,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            driverNumber: driverNumber,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<Lap>> GetLapsAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetLapsAsync(CancellationToken.None, sessionKey, driverNumber, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<Lap>> GetLapsAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<Lap>(
            endPoint: OpenF1APIEndpoint.Laps,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            driverNumber: driverNumber,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<Location>> GetLocationsAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetLocationsAsync(CancellationToken.None, sessionKey, driverNumber, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<Location>> GetLocationsAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<Location>(
            endPoint: OpenF1APIEndpoint.Location,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            driverNumber: driverNumber,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<Meeting>> GetMeetingsAsync(
        int? year = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetMeetingsAsync(CancellationToken.None, year, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<Meeting>> GetMeetingsAsync(
        CancellationToken cancellationToken,
        int? year = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        List<Filter> filters = [.. extraFilters];

        if (year.HasValue)
            filters.Add(Filter.Equal(MeetingFields.Year, year));

        return await FetchEndpointAsync<Meeting>(
            endPoint: OpenF1APIEndpoint.Meetings,
            meetingKey: meetingKey,
            cancellationToken: cancellationToken,
            extraFilters: [.. filters]);
    }

    public Task<IReadOnlyList<Overtake>> GetOvertakesAsync(
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

    public async Task<IReadOnlyList<Overtake>> GetOvertakesAsync(
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

        return await FetchEndpointAsync<Overtake>(
            endPoint: OpenF1APIEndpoint.Overtakes,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            cancellationToken: cancellationToken,
            extraFilters: [.. filters]);
    }

    public Task<IReadOnlyList<PitInfo>> GetPitAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetPitAsync(CancellationToken.None, sessionKey, driverNumber, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<PitInfo>> GetPitAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<PitInfo>(
            endPoint: OpenF1APIEndpoint.Pit,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            driverNumber: driverNumber,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<PositionUpdate>> GetPositionsAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetPositionsAsync(CancellationToken.None, sessionKey, driverNumber, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<PositionUpdate>> GetPositionsAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<PositionUpdate>(
            endPoint: OpenF1APIEndpoint.Position,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            driverNumber: driverNumber,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<RaceControlMessage>> GetRaceControlAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetRaceControlAsync(CancellationToken.None, sessionKey, driverNumber, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<RaceControlMessage>> GetRaceControlAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<RaceControlMessage>(
            endPoint: OpenF1APIEndpoint.RaceControl,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            driverNumber: driverNumber,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<Session>> GetSessionsAsync(
        int? year = null,
        MeetingKey? meetingKey = null,
        SessionKey? sessionKey = null,
        params Filter[] extraFilters)
    {
        return GetSessionsAsync(CancellationToken.None, year, meetingKey, sessionKey, extraFilters);
    }

    public async Task<IReadOnlyList<Session>> GetSessionsAsync(
        CancellationToken cancellationToken,
        int? year = null,
        MeetingKey? meetingKey = null,
        SessionKey? sessionKey = null,
        params Filter[] extraFilters)
    {
        List<Filter> filters = [.. extraFilters];

        if (year.HasValue)
            filters.Add(Filter.Equal(SessionFields.Year, year));

        return await FetchEndpointAsync<Session>(
            endPoint: OpenF1APIEndpoint.Sessions,
            sessionKey: sessionKey,
            meetingKey: meetingKey,
            cancellationToken: cancellationToken,
            extraFilters: [.. filters]);
    }

    public Task<IReadOnlyList<RaceResult>> GetRaceResultsAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetRaceResultsAsync(CancellationToken.None, sessionKey, driverNumber, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<RaceResult>> GetRaceResultsAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<RaceResult>(
            endPoint: OpenF1APIEndpoint.SessionResult,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            driverNumber: driverNumber,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<QualifyingResult>> GetQualifyingResultsAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetQualifyingResultsAsync(CancellationToken.None, sessionKey, driverNumber, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<QualifyingResult>> GetQualifyingResultsAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<QualifyingResult>(
            endPoint: OpenF1APIEndpoint.SessionResult,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            driverNumber: driverNumber,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<StartingGrid>> GetStartingGridAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetStartingGridAsync(CancellationToken.None, sessionKey, driverNumber, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<StartingGrid>> GetStartingGridAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<StartingGrid>(
            endPoint: OpenF1APIEndpoint.StartingGrid,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            driverNumber: driverNumber,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<Stint>> GetStintsAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetStintsAsync(CancellationToken.None, sessionKey, driverNumber, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<Stint>> GetStintsAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<Stint>(
            endPoint: OpenF1APIEndpoint.Stints,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            driverNumber: driverNumber,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<TeamRadio>> GetTeamRadioAsync(
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetTeamRadioAsync(CancellationToken.None, sessionKey, driverNumber, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<TeamRadio>> GetTeamRadioAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        DriverNumber? driverNumber = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<TeamRadio>(
            endPoint: OpenF1APIEndpoint.TeamRadio,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            driverNumber: driverNumber,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }

    public Task<IReadOnlyList<Weather>> GetWeatherAsync(
        SessionKey? sessionKey = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return GetWeatherAsync(CancellationToken.None, sessionKey, meetingKey, extraFilters);
    }

    public async Task<IReadOnlyList<Weather>> GetWeatherAsync(
        CancellationToken cancellationToken,
        SessionKey? sessionKey = null,
        MeetingKey? meetingKey = null,
        params Filter[] extraFilters)
    {
        return await FetchEndpointAsync<Weather>(
            endPoint: OpenF1APIEndpoint.Weather,
            sessionKey: DefaultLatestSession(sessionKey, meetingKey),
            meetingKey: meetingKey,
            cancellationToken: cancellationToken,
            extraFilters: extraFilters);
    }
}
