using System.ComponentModel;

using System.Reflection;
namespace PitWall.Models;

public record ApiParams
{
    public OpenF1APIEndpoint EndPoint { get; init; }
    public List<Filter> Filters { get; init; } = new();

    public ApiParams(OpenF1APIEndpoint endpoint)
    {
        EndPoint = endpoint;
    }

    public ApiParams WithSession(SessionKey sessionKey) => WithFilter(Filter.Equal(ApiFields.SessionKey, sessionKey));

    public ApiParams WithMeeting(MeetingKey meetingKey) => WithFilter(Filter.Equal(ApiFields.MeetingKey, meetingKey));

    public ApiParams WithDriver(DriverNumber driverNumber) => WithFilter(Filter.Equal(ApiFields.DriverNumber, driverNumber));


    public ApiParams WithFilter(Filter filter)
    {
        Filters.Add(filter);
        return this;
    }

    public ApiParams WithFilters(IEnumerable<Filter> filters)
    {
        Filters.AddRange(filters);
        return this;
    }

    public string GetRelativeUrl()
    {
        string baseEndpoint = EndPoint.ToUrlString();

        string RelativeUrl = Filters.Count == 0
            ? baseEndpoint
            : $"{baseEndpoint}?{string.Join("&", Filters.Select(f => f.Expression))}";



        return RelativeUrl;
    }

    public override string ToString()
    {
        var filtersText = Filters.Count > 0 ?
            string.Join("\n", Filters.Select(fiter => $"  - {fiter.Expression}"))
            : "  None";

        return $"EndPoint: {EndPoint}{"\n"}Filters:{"\n"}{filtersText}";
    }
}

public static class EndpointExtensions
{
    public static string ToUrlString(this OpenF1APIEndpoint endpoint)
    {
        FieldInfo? field = endpoint.GetType().GetField(endpoint.ToString());

        if(field != null)
        {
            var attribute = field.GetCustomAttribute<DescriptionAttribute>();
            if (attribute != null)
            {
                return attribute.Description;
            }
        }

        return endpoint.ToString().ToLower();
    }
}
