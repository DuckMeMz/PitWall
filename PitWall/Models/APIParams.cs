using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.WebUtilities;
namespace PitWall.Models;

public record APIParam
{
    public OpenF1APIEndpoint EndPoint { get; init; }
    public Dictionary<string, string?> Filters { get; init; } = new();

    public APIParam(OpenF1APIEndpoint endpoint)
    {
        EndPoint = endpoint;
    }

    public APIParam WithSession(SessionKey sessionKey) => AddFilter("session_key", sessionKey.Value.ToString());
    public APIParam WithDriver(DriverNumber driverNumber) => AddFilter("driver_number", driverNumber.Value.ToString());
    public APIParam WithOperator(string filterOperator, string value) => AddFilter(filterOperator, value);


    public APIParam WithFilters(IEnumerable<Filter> filters)
    {
        foreach (var filter in filters)
        {
            AddFilter(filter.Key, filter.Value);
        }
        return this;
    }

    private APIParam AddFilter(string key, string value)
    {
        Filters[key] = value;
        return this;
    }

    public override string ToString()
    {
        string baseEndpoint = EndPoint.ToUrlString();

        string FinalURL = QueryHelpers.AddQueryString(baseEndpoint, Filters);
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