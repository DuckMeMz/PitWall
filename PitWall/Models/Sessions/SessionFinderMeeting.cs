using Microsoft.Windows.Themes;
using PitWall.Models;
using System.Configuration;

namespace PitWall.Models;

public record SessionFinderMeeting(
    MeetingKey MeetingKey,
    string MeetingName,
    string CircuitShortName,
    int Year,
    bool IsSprintWeekend,
    CircuitType CircuitType,
    IReadOnlyList<SessionFinderSession> Sessions)
{
    public SessionFinderMeeting(CalendarMeeting calendarMeeting)
         : this(
             calendarMeeting.Meeting.MeetingKey,
             calendarMeeting.Meeting.MeetingName ?? "Unknown Grand Prix",
             calendarMeeting.Meeting.CircuitShortName ?? "Unknown circuit",
             calendarMeeting.Meeting.Year ?? 0,
             calendarMeeting.IsSprintWeekend(),
             calendarMeeting.Meeting.CircuitType ?? CircuitType.Unknown,
             calendarMeeting.Sessions.Select(session => new SessionFinderSession(session)).ToList())
    {
        SearchableText = string.Join(
            ' ',
            calendarMeeting.Meeting.MeetingKey.Value,
            calendarMeeting.Meeting.CircuitKey.Value,
            calendarMeeting.Meeting.CountryKey?.Value,
            calendarMeeting.Meeting.Year,
            calendarMeeting.Meeting.MeetingName,
            calendarMeeting.Meeting.OfficialName,
            calendarMeeting.Meeting.CircuitShortName,
            calendarMeeting.Meeting.Location,
            calendarMeeting.Meeting.CountryName,
            calendarMeeting.Meeting.CountryCode,
            calendarMeeting.Meeting.CircuitType,
            string.Join(' ', calendarMeeting.Sessions.Select(session =>
                $"{session.SessionKey.Value} {session.SessionName} {session.SessionType}")));
    }

    public string SearchableText { get; } = string.Empty;

    public string CircuitTypeDisplayName => CircuitType.ToApiQueryValue();

    public IReadOnlyList<SessionFinderSession> PracticeSessions =>
        Sessions.Where(session => session.SessionType == SessionType.Practice).OrderBy(session => session.StartsAt).ToList();

    public IReadOnlyList<SessionFinderSession> SprintSessions =>
        Sessions.Where(session => session.SessionType is SessionType.Sprint or SessionType.SprintQualifying).OrderBy(session => session.StartsAt).ToList();

    public IReadOnlyList<SessionFinderSession> QualifyingSessions =>
        Sessions.Where(session => session.SessionType == SessionType.Qualifying).OrderBy(session => session.StartsAt).ToList();

    public IReadOnlyList<SessionFinderSession> RaceSessions =>
        Sessions.Where(session => session.SessionType == SessionType.Race).OrderBy(session => session.StartsAt).ToList();
}

public record SessionFinderSession(
    SessionKey SessionKey,
    string SessionName,
    SessionType? SessionType,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    bool IsCancelled,
    string? GmtOffset)
{
    public string ScheduleText
    {
        get
        {
            if (StartsAt.HasValue)
            {
                DateTimeOffset start = StartsAt.Value.ToOffset(DisplayOffset);
                return $"{start:ddd d MMM · HH:mm} {TimeZoneLabel}";
            }

            return "Schedule unavailable";
        }
    }

    private TimeSpan DisplayOffset
    {
        get
        {
            if(TimeSpan.TryParse(GmtOffset, out TimeSpan offset))
            {
                return offset;
            }

            return StartsAt?.Offset ?? TimeSpan.Zero;
        }
    }

    private string TimeZoneLabel => DisplayOffset == TimeSpan.Zero
        ? "UTC"
        : $"UTC{(DisplayOffset < TimeSpan.Zero ? "-" : "+")}{DisplayOffset.Duration():hh\\:mm}";

    public SessionFinderSession(OpenF1Session session)
        : this(
             session.SessionKey,
             session.SessionName ?? "Unknown Session",
             session.SessionType,
             session.TimestampStart,
             session.TimestampEnd,
             session.IsCancelled ?? true,
             session.GmtOffset
             )
    {
    }
}