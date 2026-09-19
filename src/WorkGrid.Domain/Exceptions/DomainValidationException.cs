namespace WorkGrid.Domain.Exceptions;

/// <summary>
/// Exception thrown when a domain business invariant is violated.
/// </summary>
public class DomainValidationException : Exception
{
    public DomainValidationException(string message) : base(message)
    {
    }

    public DomainValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
