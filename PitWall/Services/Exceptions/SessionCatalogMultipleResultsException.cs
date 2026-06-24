using PitWall.Models;

namespace PitWall.Services.Exceptions;

public class SessionCatalogMultipleResultsException : Exception
{
    public SessionCatalogMultipleResultsException(string query, int year, int resultsAmount)
        : base($"The session catalog query: {query} for {year} returned {resultsAmount} meetings. Refine the query.")
    {
        Query = query;
        Year = year;
        ResultsAmount = resultsAmount;
    }

    string Query { get; }
    int Year { get; }
    int ResultsAmount { get; }

}