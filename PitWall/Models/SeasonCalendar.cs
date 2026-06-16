namespace PitWall.Models;

public record SeasonCalendar(int Year, IReadOnlyList<CalendarMeeting> Meetings)
{
    public CalendarMeeting? FindMeeting(MeetingKey meetingKey)
    {
        return Meetings.FirstOrDefault(loadedMeeting => loadedMeeting.Meeting.MeetingKey == meetingKey);
    }

    public OpenF1Session? FindSession(SessionKey sessionKey)
    {
        return Meetings.SelectMany(loadedMeeting => loadedMeeting.Sessions).FirstOrDefault(session => session.SessionKey == sessionKey);
    }
    public IReadOnlyList<OpenF1Session> GetSessionsForMeeting(MeetingKey meetingKey)
    {
        return FindMeeting(meetingKey)?.Sessions ?? [];
    }

    public IEnumerable<OpenF1Session> AllSessions() =>
        Meetings.SelectMany(loadedMeeting => loadedMeeting.Sessions);

}