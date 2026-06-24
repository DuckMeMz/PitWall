namespace PitWall.Services.Exceptions;

public class SessionCatalogNoResultsException : Exception
{
    public SessionCatalogNoResultsException(string query, int year)
        : base($"The session catalog query: {query} for {year} returned no meetings. Check the query.")
    {
        Query = query;
        Year = year;
    }
    string Query { get; }
    int Year { get; }
}
