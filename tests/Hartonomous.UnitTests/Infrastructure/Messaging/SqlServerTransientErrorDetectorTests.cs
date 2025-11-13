using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Hartonomous.Infrastructure.Services.Messaging;
using Microsoft.Data.SqlClient;

#pragma warning disable

namespace Hartonomous.UnitTests.Infrastructure.Messaging;

using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Infrastructure.Messaging;

public class SqlServerTransientErrorDetectorTests
{
    private readonly SqlServerTransientErrorDetector _detector = new();

    [Fact]
    public void IsTransient_ReturnsTrue_ForKnownSqlErrorNumber()
    {
        var exception = CreateSqlException(number: 4060);
        Assert.True(_detector.IsTransient(exception));
    }

    [Fact]
    public void IsTransient_ReturnsTrue_ForHighSeveritySqlError()
    {
        var exception = CreateSqlException(number: 50000, errorClass: 25);
        Assert.True(_detector.IsTransient(exception));
    }

    [Fact]
    public void IsTransient_ReturnsTrue_ForTimeoutException()
    {
        Assert.True(_detector.IsTransient(new TimeoutException()));
    }

    [Fact]
    public void IsTransient_ReturnsTrue_ForInvalidOperationWithMarsMessage()
    {
        var exception = new InvalidOperationException("The connection does not support MultipleActiveResultSets");
        Assert.True(_detector.IsTransient(exception));
    }

    [Fact]
    public void IsTransient_ReturnsTrue_WhenInnerExceptionIsTransient()
    {
        var inner = CreateSqlException(number: 10928);
        var wrapper = new Exception("outer", inner);
        Assert.True(_detector.IsTransient(wrapper));
    }

    [Fact]
    public void IsTransient_ReturnsFalse_ForNonTransientException()
    {
        Assert.False(_detector.IsTransient(new InvalidOperationException("non-transient")));
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(SqlError))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicMethods, typeof(SqlErrorCollection))]
    [DynamicDependency("CreateException", typeof(SqlException))]
    private static SqlException CreateSqlException(int number, byte errorClass = 0)
    {
        var error = CreateSqlError(number, errorClass);
        var collection = CreateSqlErrorCollection(error);
        var createException = typeof(SqlException).GetMethod(
            "CreateException",
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            new[] { typeof(SqlErrorCollection), typeof(string) },
            modifiers: null);

        if (createException is null)
        {
            throw new InvalidOperationException("Unable to access SqlException.CreateException via reflection.");
        }

        return (SqlException)createException.Invoke(null, new object[] { collection, "11.0.0" })!;
    }

    private static SqlError CreateSqlError(int number, byte errorClass)
    {
        var constructor = typeof(SqlError).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .First(ctor =>
            {
                var parameters = ctor.GetParameters();
                return parameters.Length == 9 &&
                       parameters[0].ParameterType == typeof(int) &&
                       parameters[1].ParameterType == typeof(byte) &&
                       parameters[2].ParameterType == typeof(byte) &&
                       parameters[3].ParameterType == typeof(string) &&
                       parameters[4].ParameterType == typeof(string) &&
                       parameters[5].ParameterType == typeof(string) &&
                       parameters[6].ParameterType == typeof(int) &&
                       parameters[7].ParameterType == typeof(int) &&
                       typeof(Exception).IsAssignableFrom(parameters[8].ParameterType);
            });

        return (SqlError)constructor.Invoke(new object[]
        {
            number,
            (byte)0,
            errorClass,
            "server",
            "error",
            "procedure",
            0,
            0,
            null!
        });
    }

    private static SqlErrorCollection CreateSqlErrorCollection(SqlError error)
    {
        var collection = (SqlErrorCollection)Activator.CreateInstance(
            typeof(SqlErrorCollection),
            nonPublic: true)!;

        var addMethod = typeof(SqlErrorCollection).GetMethod(
            "Add",
            BindingFlags.NonPublic | BindingFlags.Instance);

        addMethod!.Invoke(collection, new object[] { error });
        return collection;
    }
}
#pragma warning restore
