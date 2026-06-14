using System.Net;

public class OpenF1RequestException : Exception
{
    public string Url { get; }
    public HttpStatusCode StatusCode { get; }
    public string Response { get; }

    public OpenF1RequestException(string url, HttpStatusCode statusCode, string response)
        : base($"OpenF1 API request failed: {(int)statusCode} {statusCode} for '{url}'.")
    {
        Url = url;
        StatusCode = statusCode;
        Response = response;
    }
}