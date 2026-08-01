using PitWall.Models;
using PitWall.Models.OpenF1Api;
using System.Diagnostics;
using System.Net;

namespace PitWall.Services;

public class SessionDataService
{
    private readonly OpenF1Client _client;

    private bool _replayInitialised;
    private OpenF1Session? _currentSession;
    private OpenF1Meeting? _currentMeeting;

    public SessionDataService(OpenF1Client client)
    {
        _client = client;
    }

    public async Task<SessionOverview> LoadOverviewAsync(SessionKey sessionKey, CancellationToken cancellationToken = default)
    {
        OpenF1Session session = await GetSingleSession(sessionKey, cancellationToken);

        IReadOnlyList<OpenF1Driver> drivers = await _client.GetDriversAsync(cancellationToken, sessionKey);

        IReadOnlyList<OpenF1Lap> laps = await _client.GetLapsAsync(cancellationToken, sessionKey);

        return new SessionOverview(session, drivers, laps);
    }

    public async Task<StartingGrid> LoadStartingGrid(SessionKey sessionKey, bool allowNullSessionType = false, bool allowNonRaceSession = false, CancellationToken cancellationToken = default)
    {
        OpenF1Session session = await GetSingleSession(sessionKey, cancellationToken);
        SessionType? sessionType = session.SessionType;

        if (sessionType is null)
        {
            if (!allowNullSessionType)
            {
                throw new InvalidOperationException(
                    $"Cannot load starting grid for session key {sessionKey.Value} because the session type is unknown. " +
                    $"Set {nameof(allowNullSessionType)} to true to allow this.");
            }
        }
        else if (!IsRaceType(sessionType.Value))
        {
            if (!allowNonRaceSession)
            {
                throw new InvalidOperationException(
                    $"Cannot load starting grid for session key {sessionKey.Value} because the session type is {sessionType}. " +
                    $"Expected {SessionType.Race} or {SessionType.Sprint}. " +
                    $"Set {nameof(allowNonRaceSession)} to true to allow this.");
            }
        }

        try
        {
            IReadOnlyList<OpenF1StartingGridEntry> startingGrid =
                await _client.GetStartingGridAsync(
                    cancellationToken: cancellationToken,
                    sessionKey: sessionKey);

            return new StartingGrid(session, startingGrid);
        }
        catch (OpenF1RequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return new StartingGrid(session, []);
        }
    }

    public async Task<InitialReplayData> LoadInitialReplayChunk(SessionKey sessionKey, TimeSpan chunkLength, CancellationToken cancellationToken)
    {
        //Non-Buffered Data

        _currentSession = await GetSingleSession(sessionKey, cancellationToken);

        IReadOnlyList<OpenF1Driver> drivers = await _client.GetDriversAsync(
           cancellationToken: cancellationToken,
           sessionKey: sessionKey);

        OpenF1Session session = _currentSession
            ?? throw new InvalidOperationException("The replay has not been initialized.");


        if (drivers.Count == 0)
        {
            throw new InvalidOperationException($"No drivers were found for session  {sessionKey.Value}.");
        }

        Task<IReadOnlyList<OpenF1Meeting>> meetingTask = TryFetch(
            "meeting",
            () => _client.GetMeetingsAsync(
                cancellationToken: cancellationToken,
                meetingKey: session.MeetingKey));

        Task<IReadOnlyList<OpenF1Lap>> lapsTask = TryFetch(
            "laps",
            () => _client.GetLapsAsync(
                cancellationToken: cancellationToken,
                sessionKey: sessionKey)
            );

        //Buffered Data

        DateTimeOffset chunkStart = session.TimestampStart ?? 
            throw new ArgumentNullException(
                nameof(session), 
                $"Session: {sessionKey} has no TimestampStart. Unable to calculate chunkEnd");

        DateTimeOffset chunkEnd = chunkStart + chunkLength;

        Task<IReadOnlyList<OpenF1Location>> locationsTask = FetchChunks(
            "locations",
            chunkStart,
            chunkEnd,
            (chunkStart, chunkEnd) => _client.GetLocationsAsync(
                cancellationToken: cancellationToken,
                sessionKey: sessionKey,
                extraFilters:
                [
                    Filter.GreaterThanOrEqual(LocationFields.Timestamp, chunkStart),
                    Filter.LessThan(LocationFields.Timestamp, chunkEnd)
                ]));

        Task<IReadOnlyList<OpenF1PositionUpdate>> positionUpdatesTask = FetchChunks(
          "positions",
          chunkStart,
          chunkEnd,
          (chunkStart, chunkEnd) => _client.GetPositionsAsync(
              cancellationToken: cancellationToken,
              sessionKey: sessionKey,
              extraFilters:
              [
                  Filter.GreaterThanOrEqual(PositionFields.Timestamp, chunkStart),
                  Filter.LessThan(PositionFields.Timestamp, chunkEnd)
              ]));

        Task<IReadOnlyList<OpenF1CarTelemetrySample>> carTelemetryTask = FetchChunks(
            "car telemetry",
            chunkStart,
            chunkEnd,
            (chunkStart, chunkEnd) => _client.GetCarTelemetryAsync(
                cancellationToken: cancellationToken,
                sessionKey: sessionKey,
                extraFilters:
                [
                    Filter.GreaterThanOrEqual(CarTelemetrySampleFields.Timestamp, chunkStart),
                    Filter.LessThan(CarTelemetrySampleFields.Timestamp, chunkEnd)
                ]));

        Task<IReadOnlyList<OpenF1IntervalSample>> intervalsTask = FetchChunks(
           "intervals",
           chunkStart,
           chunkEnd,
           (chunkStart, chunkEnd) => _client.GetIntervalsAsync(
               cancellationToken: cancellationToken,
               sessionKey: sessionKey,
               extraFilters:
               [
                   Filter.GreaterThanOrEqual(IntervalSampleFields.Timestamp, chunkStart),
                   Filter.LessThan(IntervalSampleFields.Timestamp, chunkEnd)
               ]));

        await Task.WhenAll(
           meetingTask,
           locationsTask,
           positionUpdatesTask,
           carTelemetryTask,
           intervalsTask,
           lapsTask);

        _currentMeeting = (await meetingTask).FirstOrDefault();

        _replayInitialised = true;

        return new InitialReplayData(
            session,
            drivers,
            await locationsTask,
            await positionUpdatesTask,
            await carTelemetryTask,
            await intervalsTask,
            await lapsTask,
            chunkLength,
            _currentMeeting);
    }

    public async Task<ReplayDataChunk> LoadReplayChunk(SessionKey sessionKey, DateTimeOffset chunkStart, TimeSpan chunkLength, CancellationToken cancellationToken = default)
    {
        if (!_replayInitialised || _currentSession is null || _currentSession!.SessionKey != sessionKey)
        {
            throw new InvalidOperationException($"LoadInitialReplayChunk must be called before loading more replay chunks");
        }

        OpenF1Session session = _currentSession
            ?? throw new InvalidOperationException("The replay has not been initialized.");

        DateTimeOffset chunkEnd = chunkStart + chunkLength;

        Task<IReadOnlyList<OpenF1Location>> locationsTask = FetchChunks(
            "locations",
            chunkStart,
            chunkEnd,
            (chunkStart, chunkEnd) => _client.GetLocationsAsync(
                cancellationToken: cancellationToken,
                sessionKey: sessionKey,
                extraFilters:
                [
                    Filter.GreaterThanOrEqual(LocationFields.Timestamp, chunkStart),
                    Filter.LessThan(LocationFields.Timestamp, chunkEnd)
                ]));

        Task<IReadOnlyList<OpenF1PositionUpdate>> positionUpdatesTask = FetchChunks(
            "positions",
            chunkStart,
            chunkEnd,
            (chunkStart, chunkEnd) => _client.GetPositionsAsync(
                cancellationToken: cancellationToken,
                sessionKey: sessionKey,
                extraFilters:
                [
                    Filter.GreaterThanOrEqual(PositionFields.Timestamp, chunkStart),
                    Filter.LessThan(PositionFields.Timestamp, chunkEnd)
                ]));

        Task<IReadOnlyList<OpenF1CarTelemetrySample>> carTelemetryTask = FetchChunks(
            "car telemetry",
            chunkStart,
            chunkEnd,
            (chunkStart, chunkEnd) => _client.GetCarTelemetryAsync(
                cancellationToken: cancellationToken,
                sessionKey: sessionKey,
                extraFilters:
                [
                    Filter.GreaterThanOrEqual(CarTelemetrySampleFields.Timestamp, chunkStart),
                    Filter.LessThan(CarTelemetrySampleFields.Timestamp, chunkEnd)
                ]));

        Task<IReadOnlyList<OpenF1IntervalSample>> intervalsTask = FetchChunks(
            "intervals",
            chunkStart,
            chunkEnd,
            (chunkStart, chunkEnd) => _client.GetIntervalsAsync(
                cancellationToken: cancellationToken,
                sessionKey: sessionKey,
                extraFilters:
                [
                    Filter.GreaterThanOrEqual(IntervalSampleFields.Timestamp, chunkStart),
                    Filter.LessThan(IntervalSampleFields.Timestamp, chunkEnd)
                ]));


        await Task.WhenAll(
            locationsTask,
            positionUpdatesTask,
            carTelemetryTask,
            intervalsTask);

        return new ReplayDataChunk(
            session,
            await locationsTask,
            await positionUpdatesTask,
            await carTelemetryTask,
            await intervalsTask,
            chunkStart,
            chunkLength,
            _currentMeeting);
    }
    public async Task<IReadOnlyList<OpenF1Location>> GetLapLocationsAsync(OpenF1Lap lap, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lap);

        DateTimeOffset lapStart = lap.TimestampStart ?? throw new InvalidOperationException("The lap has no start time.");

        double lapDurationSeconds = lap.LapDuration ?? throw new InvalidOperationException("The lap has no duration.");

        DateTimeOffset lapEnd = lapStart + TimeSpan.FromSeconds(lapDurationSeconds);


        IReadOnlyList<OpenF1Location> locations = await _client.GetLocationsAsync(
            cancellationToken,
            lap.SessionKey,
            lap.DriverNumber,
            lap.MeetingKey,
            Filter.GreaterThanOrEqual(LocationFields.Timestamp, lapStart),
            Filter.LessThan(LocationFields.Timestamp, lapEnd)
            );

        return locations;
    }

    public async Task<IReadOnlyList<OpenF1Lap>> GetQualifyingLapsAsync(
        MeetingKey meetingKey,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<OpenF1Session> sessions = await _client.GetSessionsAsync(
            cancellationToken: cancellationToken,
            meetingKey: meetingKey);

        OpenF1Session? qualifyingSession = sessions.FirstOrDefault(session =>
            session.SessionType == SessionType.Qualifying &&
            session.IsCancelled is not true);

        if (qualifyingSession is null)
        {
            return [];
        }

        return await _client.GetLapsAsync(
            cancellationToken: cancellationToken,
            sessionKey: qualifyingSession.SessionKey);
    }
    private static async Task<IReadOnlyList<T>> TryFetch<T>(
        string streamName,
        Func<Task<IReadOnlyList<T>>> fetchAsync)
    {
        try
        {
            return await fetchAsync();
        }
        catch (OpenF1RequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            Debug.WriteLine($"OpenF1 returned no {streamName}: {ex.Message}");
            return [];
        }
    }

    private static async Task<IReadOnlyList<T>> FetchChunks<T>(
        string streamName,
        DateTimeOffset chunkStart,
        DateTimeOffset chunkEnd,
        Func<DateTimeOffset, DateTimeOffset, Task<IReadOnlyList<T>>> fetchChunkAsync)
    {
        return await TryFetch(
            streamName,
            () => fetchChunkAsync(chunkStart, chunkEnd)
        );
    }

    private async Task<OpenF1Session> GetSingleSession(SessionKey sessionKey, CancellationToken cancellationToken)
    {
        IReadOnlyList<OpenF1Session> sessions = await _client.GetSessionsAsync(
                    cancellationToken: cancellationToken,
                    sessionKey: sessionKey);

        if (sessions.Count == 0)
        {
            throw new InvalidOperationException($"No OpenF1 session was found for session key {sessionKey.Value}.");
        }

        if (sessions.Count > 1)
        {
            throw new InvalidOperationException($"Expected one OpenF1 session for session key {sessionKey.Value}, but got {sessions.Count}.");
        }

        OpenF1Session session = sessions[0];
        return session;
    }

    private static bool IsRaceType(SessionType sessionType)
    {
        return sessionType is SessionType.Race or SessionType.Sprint;
    }
}
