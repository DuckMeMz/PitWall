using PitWall.Models;
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

        IReadOnlyList<Driver> drivers = await _client.GetDriversAsync(cancellationToken, sessionKey);

        IReadOnlyList<Lap> laps = await _client.GetLapsAsync(cancellationToken, sessionKey);

        return new SessionOverview(session, drivers, laps);
    }

    public async Task<StartingGrid> LoadStartingGrid(SessionKey sessionKey, bool allowNullSessionType = false, bool allowNonRaceSession = false, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<OpenF1Session> sessions = await _client.GetSessionsAsync(
            cancellationToken: cancellationToken,
            sessionKey: sessionKey);

        if(sessions.Count == 0)
        {
            throw new InvalidOperationException(
                $"Cannot load starting grid because no OpenF1 session was found for session key {sessionKey.Value}.");
        }
        
        if(sessions.Count > 1)
        {
            throw new InvalidOperationException(
                $"Cannot load starting grid because session key {sessionKey.Value} returned {sessions.Count} sessions. Expected exactly one session.");
        }

        OpenF1Session session = sessions[0];
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

    private static bool IsRaceType(SessionType sessionType)
    {
        return sessionType is SessionType.Race or SessionType.Sprint;
    }
}
 