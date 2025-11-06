using Hartonomous.Shared.Contracts.Errors;

namespace Hartonomous.Api.Common;

public static class ErrorHelpers
{
    public static ErrorDetail DatabaseError(string message) =>
        ErrorDetailFactory.Create("DatabaseError", message, null, new Dictionary<string, object?>
        {
            ["category"] = "Database"
        });

    public static ErrorDetail TransactionError(string message) =>
        ErrorDetailFactory.Create("TransactionError", message, null, new Dictionary<string, object?>
        {
            ["category"] = "Transaction"
        });
}
