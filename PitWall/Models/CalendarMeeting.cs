namespace PitWall.Models;

public record CalendarMeeting(OpenF1Meeting Meeting, IReadOnlyList<OpenF1Session> Sessions)
{
    public OpenF1Session? GetMainRaceSession() =>
        Sessions.FirstOrDefault(session => session.SessionType == SessionType.Race);

    public OpenF1Session? GetMainQualifyingSession() =>
        Sessions.FirstOrDefault(session => session.SessionType == SessionType.Qualifying);

    public IEnumerable<OpenF1Session> GetRaceSessions() =>
        Sessions.Where(session => session.SessionType == SessionType.Race || session.SessionType == SessionType.Sprint);

    public IEnumerable<OpenF1Session> GetPracticeSessions() =>
        Sessions.Where(session => session.SessionType == SessionType.Practice );

    public IEnumerable<OpenF1Session> GetSprintSessions() =>
        Sessions.Where(session => session.SessionType == SessionType.Sprint || session.SessionType == SessionType.SprintQualifying);

    public IEnumerable<OpenF1Session> GetQualifyingSessions() =>
        Sessions.Where(session => session.SessionType == SessionType.Qualifying || session.SessionType == SessionType.SprintQualifying);
    public bool IsSprintWeekend() =>
        Sessions.FirstOrDefault(session => session.SessionType == SessionType.Sprint  || session.SessionType == SessionType.SprintQualifying) is not null;


}