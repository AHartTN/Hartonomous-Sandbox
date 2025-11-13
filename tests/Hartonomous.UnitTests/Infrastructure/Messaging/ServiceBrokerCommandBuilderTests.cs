using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Hartonomous.Core.Configuration;
using Hartonomous.Infrastructure.Services.Messaging;

namespace Hartonomous.UnitTests.Infrastructure.Messaging;

public sealed class ServiceBrokerCommandBuilderTests
{
    [Fact]
    public void BuildCommands_WithValidOptions_ReturnsExpectedSqlTemplates()
    {
        var options = new MessageBrokerOptions
        {
            InitiatorServiceName = "Hartonomous.Initiator",
            TargetServiceName = "Hartonomous.Target",
            ContractName = "//Hartonomous/TestContract",
            MessageTypeName = "//Hartonomous/TestMessage",
            QueueName = "dbo.CustomQueue"
        };

        var (send, receive) = InvokeBuildCommands(options);

        Assert.Contains("FROM SERVICE [Hartonomous.Initiator]", send, StringComparison.Ordinal);
        Assert.Contains("TO SERVICE 'Hartonomous.Target'", send, StringComparison.Ordinal);
        Assert.Contains("ON CONTRACT [//Hartonomous/TestContract]", send, StringComparison.Ordinal);
        Assert.Contains("MESSAGE TYPE [//Hartonomous/TestMessage]", send, StringComparison.Ordinal);
        Assert.Contains("FROM dbo.CustomQueue", receive, StringComparison.Ordinal);
        Assert.Contains("WAITFOR (", receive, StringComparison.Ordinal);
        Assert.Contains("TIMEOUT @timeoutMs;", receive, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(GetInvalidOptions))]
    public void BuildCommands_WithMissingIdentifiers_Throws(Action<MessageBrokerOptions> mutate, string expectedMessage)
    {
        var options = new MessageBrokerOptions();
        mutate(options);

        var ex = Assert.Throws<TargetInvocationException>(() => InvokeBuildCommands(options));
        var inner = Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Contains(expectedMessage, inner.Message, StringComparison.Ordinal);
    }

    public static IEnumerable<object[]> GetInvalidOptions()
    {
        yield return new object[] { (Action<MessageBrokerOptions>)(o => o.InitiatorServiceName = " "), "Initiator" };
        yield return new object[] { (Action<MessageBrokerOptions>)(o => o.TargetServiceName = ""), "Target" };
        yield return new object[] { (Action<MessageBrokerOptions>)(o => o.ContractName = null!), "Contract" };
        yield return new object[] { (Action<MessageBrokerOptions>)(o => o.MessageTypeName = "\t"), "Message type" };
        yield return new object[] { (Action<MessageBrokerOptions>)(o => o.QueueName = ""), "Queue name" };
    }

    private static (string Send, string Receive) InvokeBuildCommands(MessageBrokerOptions options)
    {
        var builderType = Type.GetType(
            "Hartonomous.Infrastructure.Services.Messaging.ServiceBrokerCommandBuilder, Hartonomous.Infrastructure",
            throwOnError: true)!;

        var method = builderType.GetMethod(
            "BuildCommands",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        var result = method!.Invoke(null, new object[] { options })!;
        return ((string SendCommand, string ReceiveCommand))result;
    }
}
