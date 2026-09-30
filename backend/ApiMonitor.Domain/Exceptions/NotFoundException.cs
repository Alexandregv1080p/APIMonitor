namespace ApiMonitor.Domain.Exceptions;

public class NotFoundException(string title, string detail) : Exception(detail)
{
    public string Title { get; } = title;
}
