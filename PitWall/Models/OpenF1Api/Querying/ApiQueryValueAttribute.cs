namespace PitWall.Models;

[AttributeUsage(AttributeTargets.Field)]
public class ApiQueryValueAttribute : Attribute
{
    public ApiQueryValueAttribute(string value)
    {
        Value = value;
    }

    public string Value { get; }
}
