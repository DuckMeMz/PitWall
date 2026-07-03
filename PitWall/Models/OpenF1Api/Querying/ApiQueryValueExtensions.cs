namespace PitWall.Models;

public static class ApiQueryValueExtensions
{
    public static string ToApiQueryValue(this Enum value)
    {
        var member = value
            .GetType()
            .GetMember(value.ToString())
            .FirstOrDefault();

        var attribute = member?
            .GetCustomAttributes(typeof(ApiQueryValueAttribute), false)
            .OfType<ApiQueryValueAttribute>()
            .FirstOrDefault();

        return attribute?.Value ?? value.ToString();
    }
}
