using PitWall.Models;
using PitWall.Services;
using PitWall.Services.Exceptions;
using System.Diagnostics;

namespace PitWall.Services;

public class SessionCatalogService
{
    private readonly OpenF1Client _client;
    public SessionCatalogService(OpenF1Client openF1Client)
    {
        _client = openF1Client;
    }

    public async Task<SeasonCalendar> GetCalendarAsync(int year, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<OpenF1Meeting> meetings = await _client.GetMeetingsAsync(
            cancellationToken: cancellationToken,
            year: year);

        IReadOnlyList<OpenF1Session> sessions = await _client.GetSessionsAsync(
            cancellationToken: cancellationToken,
            year: year
            );

        Dictionary<MeetingKey, List<OpenF1Session>> sessionByMeeting = sessions
            .GroupBy(session => session.MeetingKey)
            .ToDictionary(group => group.Key, group => group.OrderBy(session => session.TimestampStart)
            .ToList());

        List<CalendarMeeting> loadedMeetings = meetings
            .OrderBy(meeting => meeting.TimestampStart)
            .Select(meeting =>
            {
                sessionByMeeting.TryGetValue(meeting.MeetingKey, out List<OpenF1Session>? meetingSession);

                return new CalendarMeeting(
                    Meeting: meeting,
                    Sessions: meetingSession ?? []);
            }).ToList();

        return new SeasonCalendar(year, loadedMeetings);
    }

    public async Task<CalendarMeeting> GetCalendarMeetingAsync(MeetingKey meetingKey, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<OpenF1Meeting> meeting = await _client.GetMeetingsAsync(
            cancellationToken: cancellationToken,
            meetingKey: meetingKey);

        IReadOnlyList<OpenF1Session> sessions = await _client.GetSessionsAsync(
            cancellationToken: cancellationToken,
            meetingKey: meetingKey);

        return new CalendarMeeting(
            Meeting: meeting[0],
            Sessions: sessions);
    }

    public async Task<CalendarMeeting> GetCalendarMeetingAsync(int year, string meetingName, CancellationToken cancellationToken = default)
    {
        MeetingKey meetingKey = await SearchMeetingsAsync(year, meetingName);
        return await GetCalendarMeetingAsync(meetingKey, cancellationToken);
    }

    public async Task<OpenF1Session> GetSessionAsync(SessionKey sessionKey, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<OpenF1Session> sessions = await _client.GetSessionsAsync(
        cancellationToken: cancellationToken,
        sessionKey: sessionKey);

        if (sessions.Count == 0)
        {
            throw new InvalidOperationException($"No session found for session key {sessionKey.Value}.");
        }

        if (sessions.Count > 1)
        {
            throw new InvalidOperationException($"Expected one session for session key {sessionKey.Value}, but got {sessions.Count}.");
        }

        return sessions[0];
    }

    public async Task<SessionType?> GetSessionTypeAsync(SessionKey sessionKey, CancellationToken cancellationToken = default)
    {
        OpenF1Session session = await GetSessionAsync(sessionKey, cancellationToken);
        return session.SessionType;
    }

    private async Task<MeetingKey> SearchMeetingsAsync(int year, string query, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<OpenF1Meeting> meetings = await _client.GetMeetingsAsync(
            cancellationToken: cancellationToken,
            year: year);

        query = query.Trim().ToLowerInvariant();

        IReadOnlyList<OpenF1Meeting> filteredMeetings = 
            meetings.Where(meeting =>
            meeting.CircuitShortName?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
            meeting.CountryCode?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
            meeting.CountryName?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
            meeting.Location?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
            meeting.MeetingName?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
            meeting.OfficialName?.Contains(query, StringComparison.OrdinalIgnoreCase) == true).ToList();

        if (filteredMeetings.Count == 0)
        {
            throw new SessionCatalogNoResultsException(query, year);
        }

        if (filteredMeetings.Count > 1)
        {
            throw new SessionCatalogMultipleResultsException(query, year, filteredMeetings.Count);
        }

        return filteredMeetings[0].MeetingKey;
    }
}