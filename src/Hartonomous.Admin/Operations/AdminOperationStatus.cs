namespace Hartonomous.Admin.Operations;

public enum AdminOperationState
{
    Queued,
    Running,
    Succeeded,
    Failed
}

public sealed record AdminOperationStatus(
    Guid OperationId,
    string OperationType,
    string Description,
    AdminOperationState State,
    DateTimeOffset EnqueuedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? Detail,
    string? Error)
{
    public AdminOperationStatus WithState(AdminOperationState state, string? detail = null, string? error = null)
    {
        return this with
        {
            State = state,
            Detail = detail ?? Detail,
            Error = error
        };
    }
}

public sealed record AdminOperationOutcome(bool Success, string Message, string? Error = null)
{
    public static AdminOperationOutcome Succeeded(string message) => new(true, message);
    public static AdminOperationOutcome Failed(string message, string? error = null) => new(false, message, error);
}
