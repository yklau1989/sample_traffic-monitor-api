namespace TrafficMonitor.Application.Exceptions;

public sealed class InvalidSortFieldException : Exception
{
    public InvalidSortFieldException(string field)
        : base($"Unknown sort field: {field}")
    {
    }
}
