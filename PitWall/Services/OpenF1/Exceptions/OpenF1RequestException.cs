using System.Net;

public class OpenF1RequestException : Exception
{
    public string Url { get; }
    public HttpStatusCode StatusCode { get; }
    public string Response { get; }

    public OpenF1RequestException(string url, HttpStatusCode statusCode, string response)
        : base($"OpenF1 API request failed: {(int)statusCode} {statusCode} for '{url}'. Response: {FormatResponse(response)}")
    {
        Url = url;
        StatusCode = statusCode;
        Response = response;
    }

    private static string FormatResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return "<empty>";
        }

        const int maxLength = 500;
        string trimmed = response.Trim();

        return trimmed.Length <= maxLength
            ? trimmed
            : $"{trimmed[..maxLength]}...";
    }
}
