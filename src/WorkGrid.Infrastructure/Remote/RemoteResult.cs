namespace WorkGrid.Infrastructure.Remote;

/// <summary>
/// Outcome of a single remote operation.
/// Preserves enough detail for callers to distinguish transport
/// failures from one another without leaking raw exceptions.
/// </summary>
public sealed class RemoteResult
{
    public bool IsSuccess { get; }
    public RemoteErrorKind? ErrorKind { get; }
    public string? ErrorMessage { get; }
    public int? StatusCode { get; }

    private RemoteResult(
        bool isSuccess,
        RemoteErrorKind? errorKind,
        string? errorMessage,
        int? statusCode)
    {
        IsSuccess = isSuccess;
        ErrorKind = errorKind;
        ErrorMessage = errorMessage;
        StatusCode = statusCode;
    }

    public static RemoteResult Success(int? statusCode = 200)
        => new(true, null, null, statusCode);

    public static RemoteResult Failure(
        RemoteErrorKind kind,
        string? message = null,
        int? statusCode = null)
        => new(false, kind, message, statusCode);

    public override string ToString()
        => IsSuccess
            ? $"Success({StatusCode})"
            : $"Failure({ErrorKind}, {StatusCode}, {ErrorMessage})";
}
