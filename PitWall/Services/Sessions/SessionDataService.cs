using PitWall.Models;
using PitWall.Models.OpenF1Api;
using System.Diagnostics;
using System.Net;

namespace PitWall.Services;

public class SessionDataService
{
    private readonly OpenF1Client _client;

    private bool replayInitialised = false;
    private OpenF1Session? currentSession = null;
    private OpenF1Meeting? currentMeeting = null;
    private IReadOnlyList<OpenF1Driver>? currentDrivers = null;

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

    public async Task<ReplayData> LoadInitalReplayChunk(SessionKey sessionKey, TimeSpan chunkLength, CancellationToken cancellationToken)
    {
        //Non-Buffered Data

        currentSession = await GetSingleSession(sessionKey, cancellationToken);

        currentDrivers = await _client.GetDriversAsync(
           cancellationToken: cancellationToken,
           sessionKey: sessionKey);

        OpenF1Session session = currentSession
            ?? throw new InvalidOperationException("The replay has not been initialized.");

        IReadOnlyList<OpenF1Driver> drivers = currentDrivers
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

        Task<IReadOnlyList<OpenF1Location>> locationsTask = FetchDriverDataChunk(
            drivers,
            "locations",
            chunkStart,
            chunkEnd,
            (driverNumber, chunkStart, chunkEnd) => _client.GetLocationsAsync(
                cancellationToken: cancellationToken,
                sessionKey: sessionKey,
                driverNumber: driverNumber,
                extraFilters:
                [
                    Filter.GreaterThanOrEqual(LocationFields.Timestamp, chunkStart),
                    Filter.LessThan(LocationFields.Timestamp, chunkEnd)
                ]));

        Task<IReadOnlyList<OpenF1PositionUpdate>> positionUpdatesTask = FetchSessionChunks(
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

        Task<IReadOnlyList<OpenF1CarTelemetrySample>> carTelemetryTask = FetchDriverDataChunk(
            drivers,
            "car telemetry",
            chunkStart,
            chunkEnd,
            (driverNumber, chunkStart, chunkEnd) => _client.GetCarTelemetryAsync(
                cancellationToken: cancellationToken,
                sessionKey: sessionKey,
                driverNumber: driverNumber,
                extraFilters:
                [
                    Filter.GreaterThanOrEqual(CarTelemetrySampleFields.Timestamp, chunkStart),
                    Filter.LessThan(CarTelemetrySampleFields.Timestamp, chunkEnd)
                ]));

        Task<IReadOnlyList<OpenF1IntervalSample>> intervalsTask = FetchSessionChunks(
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

        currentMeeting = (await meetingTask).FirstOrDefault();

        replayInitialised = true;

        return new ReplayData(
            session,
            drivers,
            await locationsTask,
            await positionUpdatesTask,
            await carTelemetryTask,
            await intervalsTask,
            await lapsTask,
            chunkLength,
            currentMeeting);
    }

    public async Task<ReplayDataChunk> LoadReplayChunk(SessionKey sessionKey, DateTimeOffset chunkStart, TimeSpan chunkLength, CancellationToken cancellationToken = default)
    {
        if (!replayInitialised || currentSession is null || currentSession!.SessionKey != sessionKey)
        {
            throw new InvalidOperationException($"LoadInitialReplayChunk must be called before loading more replay chunks");
        }

        OpenF1Session session = currentSession
            ?? throw new InvalidOperationException("The replay has not been initialized.");

        IReadOnlyList<OpenF1Driver> drivers = currentDrivers
            ?? throw new InvalidOperationException("The replay has not been initialized.");

        DateTimeOffset chunkEnd = chunkStart + chunkLength;

        Task<IReadOnlyList<OpenF1Location>> locationsTask = FetchDriverDataChunk(
            drivers,
            "locations",
            chunkStart,
            chunkEnd,
            (driverNumber, chunkStart, chunkEnd) => _client.GetLocationsAsync(
                cancellationToken: cancellationToken,
                sessionKey: sessionKey,
                driverNumber: driverNumber,
                extraFilters:
                [
                    Filter.GreaterThanOrEqual(LocationFields.Timestamp, chunkStart),
                    Filter.LessThan(LocationFields.Timestamp, chunkEnd)
                ]));

        Task<IReadOnlyList<OpenF1PositionUpdate>> positionUpdatesTask = FetchSessionChunks(
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

        Task<IReadOnlyList<OpenF1CarTelemetrySample>> carTelemetryTask = FetchDriverDataChunk(
            drivers,
            "car telemetry",
            chunkStart,
            chunkEnd,
            (driverNumber, chunkStart, chunkEnd) => _client.GetCarTelemetryAsync(
                cancellationToken: cancellationToken,
                sessionKey: sessionKey,
                driverNumber: driverNumber,
                extraFilters:
                [
                    Filter.GreaterThanOrEqual(CarTelemetrySampleFields.Timestamp, chunkStart),
                    Filter.LessThan(CarTelemetrySampleFields.Timestamp, chunkEnd)
                ]));

        Task<IReadOnlyList<OpenF1IntervalSample>> intervalsTask = FetchSessionChunks(
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
            currentMeeting);
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

    //Used for fetching data per driver such as locations or car telementry
    private static async Task<IReadOnlyList<T>> FetchDriverDataChunk<T>(
        IReadOnlyList<OpenF1Driver> drivers,
        string streamName,
        DateTimeOffset chunkStart,
        DateTimeOffset chunkEnd,
        Func<DriverNumber, DateTimeOffset, DateTimeOffset, Task<IReadOnlyList<T>>> fetchChunkAsync)
    {
        Task<IReadOnlyList<T>>[] tasks = drivers
            .Select(driver => TryFetch(
                $"{streamName} for driver {driver.DriverNumber.Value}",
                () => fetchChunkAsync(driver.DriverNumber, chunkStart, chunkEnd)))
            .ToArray();

        IReadOnlyList<T>[] results = await Task.WhenAll(tasks);

        return results
            .SelectMany(stream => stream)
            .ToList();
    }

    //Used for session feeds such as positions, intervals, laps or race control.
    private static async Task<IReadOnlyList<T>> FetchSessionChunks<T>(
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
