using PitWall.Models;
using PitWall.Models.OpenF1Api;
using System.Diagnostics;
using System.Net;

namespace PitWall.Services;

public class SessionDataService
{
    private readonly OpenF1Client _client;
    private readonly SessionCatalogService _sessionCatalog;

    public SessionDataService(OpenF1Client client, SessionCatalogService sessionCatalog)
    {
        _client = client;
        _sessionCatalog = sessionCatalog;
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

    public async Task<ReplayData> LoadReplayDataAsync(SessionKey sessionKey, CancellationToken cancellationToken = default)
    {
        OpenF1Session session = await GetSingleSession(sessionKey, cancellationToken);

        Task<IReadOnlyList<OpenF1Meeting>> meetingTask = LoadOptionalReplayStreamAsync(
            "meeting",
            () => _client.GetMeetingsAsync(
                cancellationToken: cancellationToken,
                meetingKey: session.MeetingKey));

        IReadOnlyList<OpenF1Driver> drivers = await _client.GetDriversAsync(
            cancellationToken: cancellationToken,
            sessionKey: sessionKey);

        if (drivers.Count == 0)
        {
            throw new InvalidOperationException($"No OpenF1 drivers were found for session key {sessionKey.Value}.");
        }

        Task<IReadOnlyList<OpenF1Location>> locationsTask = LoadPerDriverReplayStreamAsync(
            drivers,
            "locations",
            driverNumber => _client.GetLocationsAsync(
                cancellationToken: cancellationToken,
                sessionKey: sessionKey,
                driverNumber: driverNumber));

        Task<IReadOnlyList<OpenF1CarTelemetrySample>> carTelemetryTask = LoadPerDriverReplayStreamAsync(
            drivers,
            "car telemetry",
            driverNumber => _client.GetCarTelemetryAsync(
                cancellationToken: cancellationToken,
                sessionKey: sessionKey,
                driverNumber: driverNumber));

        Task<IReadOnlyList<OpenF1PositionUpdate>> positionUpdatesTask = LoadOptionalReplayStreamAsync(
            "positions",
            () => _client.GetPositionsAsync(
                cancellationToken: cancellationToken,
                sessionKey: sessionKey));

        Task<IReadOnlyList<OpenF1IntervalSample>> intervalsTask = LoadOptionalReplayStreamAsync(
            "intervals",
            () => _client.GetIntervalsAsync(
                cancellationToken: cancellationToken,
                sessionKey: sessionKey));

        Task<IReadOnlyList<OpenF1Lap>> lapsTask = LoadOptionalReplayStreamAsync(
            "laps",
            () => _client.GetLapsAsync(
                cancellationToken: cancellationToken,
                sessionKey: sessionKey));

        Task<IReadOnlyList<OpenF1RaceControlMessage>> raceControlMessagesTask = LoadOptionalReplayStreamAsync(
            "race control",
            () => _client.GetRaceControlAsync(
                cancellationToken: cancellationToken,
                sessionKey: sessionKey));

        await Task.WhenAll(
            meetingTask,
            locationsTask,
            positionUpdatesTask,
            carTelemetryTask,
            intervalsTask,
            lapsTask,
            raceControlMessagesTask);

        OpenF1Meeting? meeting = (await meetingTask).FirstOrDefault();

        return new ReplayData(
            session,
            drivers,
            await locationsTask,
            await positionUpdatesTask,
            await carTelemetryTask,
            await intervalsTask,
            await lapsTask,
            await raceControlMessagesTask,
            meeting);
    }

    private static async Task<IReadOnlyList<T>> LoadOptionalReplayStreamAsync<T>(
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

    private static async Task<IReadOnlyList<T>> LoadPerDriverReplayStreamAsync<T>(
        IReadOnlyList<OpenF1Driver> drivers,
        string streamName,
        Func<DriverNumber, Task<IReadOnlyList<T>>> fetchDriverDataAsync)
    {
        Task<IReadOnlyList<T>>[] tasks = drivers
            .Select(driver => LoadOptionalReplayStreamAsync(
                $"{streamName} for driver {driver.DriverNumber.Value}",
                () => fetchDriverDataAsync(driver.DriverNumber)))
            .ToArray();

        IReadOnlyList<T>[] results = await Task.WhenAll(tasks);

        return results
            .SelectMany(stream => stream)
            .ToList();
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
