using System;
using Hartonomous.Core.Resilience;
using Microsoft.Data.SqlClient;

namespace Hartonomous.Infrastructure.Services.Messaging;

public sealed class SqlServerTransientErrorDetector : ITransientErrorDetector
{
    private static readonly int[] TransientErrorNumbers =
    {
        4060, 10928, 10929, 40197, 40501, 40613, 49918, 49919, 49920,
        1205, 233, 10053, 10054, 10060, 10061, 11001, 17142
    };

    public bool IsTransient(Exception exception)
    {
        if (exception is SqlException sqlException)
        {
            foreach (SqlError error in sqlException.Errors)
            {
                if (Array.IndexOf(TransientErrorNumbers, error.Number) >= 0)
                {
                    return true;
                }
            }

            return sqlException.Class >= 20;
        }

        if (exception is TimeoutException)
        {
            return true;
        }

        if (exception is InvalidOperationException invalidOperationException &&
            invalidOperationException.Message.Contains("The connection does not support MultipleActiveResultSets", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (exception.InnerException != null)
        {
            return IsTransient(exception.InnerException);
        }

        return false;
    }
}
