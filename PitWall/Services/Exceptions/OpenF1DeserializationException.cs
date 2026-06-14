using System.Net;

namespace PitWall.Services.Exceptions;

public class OpenF1DeserializeException : Exception
{
    public string Url { get; }
    public Type TargetType{ get; }

    public OpenF1DeserializeException(string url, Type targetType, Exception innerException)
        : base($"Failed to deserialize OpenF1 from {url} as {targetType.Name}.", innerException)
    {
        Url = url;
        TargetType = targetType;
    }
}