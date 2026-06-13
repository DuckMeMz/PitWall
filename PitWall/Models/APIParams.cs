using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.WebUtilities;
namespace PitWall.Models;

public record APIParam
{
    public OpenF1APIEndpoint EndPoint { get; init; }
    public List<Filter> Filters { get; init; } = new();

    public APIParam(OpenF1APIEndpoint endpoint)
    {
        EndPoint = endpoint;
    }

    public APIParam WithSession(SessionKey sessionKey) =>
        WithFilter(Filter.Equal(ApiFields.SessionKey, sessionKey));

    public APIParam WithMeeting(MeetingKey meetingKey) =>
        WithFilter(Filter.Equal(ApiFields.MeetingKey, meetingKey));

    public APIParam WithDriver(DriverNumber driverNumber) =>
        WithFilter(Filter.Equal(ApiFields.DriverNumber, driverNumber));


    public APIParam WithFilter(Filter filter)
    {
        Filters.Add(filter);
        return this;
    }

    public APIParam WithFilters(IEnumerable<Filter> filters)
    {
        Filters.AddRange(filters);
        return this;
    }

    public override string ToString()
    {
        string baseEndpoint = EndPoint.ToUrlString();

        string FinalURL = $"{baseEndpoint}?{string.Join("&", Filters.Select(f => f.Expression))}";

        Debug.WriteLine($"Final URL: {FinalURL}");

        return FinalURL;
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