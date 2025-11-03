using System;
using System.Text;
using Hartonomous.Core.Configuration;

namespace Hartonomous.Infrastructure.Services.Messaging;

internal static class ServiceBrokerCommandBuilder
{
    public static (string SendCommand, string ReceiveCommand) BuildCommands(MessageBrokerOptions options)
    {
        ValidateIdentifiers(options);
        return (BuildSendCommandText(options), BuildReceiveCommandText(options));
    }

    private static void ValidateIdentifiers(MessageBrokerOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.InitiatorServiceName))
        {
            throw new InvalidOperationException("Initiator service name must be provided.");
        }

        if (string.IsNullOrWhiteSpace(options.TargetServiceName))
        {
            throw new InvalidOperationException("Target service name must be provided.");
        }

        if (string.IsNullOrWhiteSpace(options.ContractName))
        {
            throw new InvalidOperationException("Contract name must be provided.");
        }

        if (string.IsNullOrWhiteSpace(options.MessageTypeName))
        {
            throw new InvalidOperationException("Message type name must be provided.");
        }

        if (string.IsNullOrWhiteSpace(options.QueueName))
        {
            throw new InvalidOperationException("Queue name must be provided.");
        }
    }

    private static string BuildSendCommandText(MessageBrokerOptions options)
    {
        var builder = new StringBuilder();
        builder.AppendLine("DECLARE @conversationHandle UNIQUEIDENTIFIER;");
        builder.AppendLine();
        builder.AppendLine("BEGIN DIALOG CONVERSATION @conversationHandle");
        builder.AppendLine($"    FROM SERVICE [{options.InitiatorServiceName}]");
        builder.AppendLine($"    TO SERVICE '{options.TargetServiceName}'");
        builder.AppendLine($"    ON CONTRACT [{options.ContractName}]");
        builder.AppendLine($"    WITH ENCRYPTION = OFF, LIFETIME = @lifetimeSeconds;");
        builder.AppendLine();
        builder.AppendLine("SEND ON CONVERSATION @conversationHandle");
        builder.AppendLine($"    MESSAGE TYPE [{options.MessageTypeName}] (@messageBody);");
        builder.AppendLine();
        builder.AppendLine("END CONVERSATION @conversationHandle;");
        return builder.ToString();
    }

    private static string BuildReceiveCommandText(MessageBrokerOptions options)
    {
        var builder = new StringBuilder();
        builder.AppendLine("WAITFOR (");
        builder.AppendLine("    RECEIVE TOP(1)");
        builder.AppendLine("        conversation_handle,");
        builder.AppendLine("        message_type_name,");
        builder.AppendLine("        CAST(message_body AS NVARCHAR(MAX)) AS message_body,");
        builder.AppendLine("        message_enqueue_time");
        builder.AppendLine($"    FROM {options.QueueName}");
        builder.AppendLine("),");
        builder.AppendLine("TIMEOUT @timeoutMs;");
        return builder.ToString();
    }
}
