using System;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Messaging;
using Hartonomous.Core.Serialization;

namespace Hartonomous.UnitTests.Core.Messaging;

public sealed class BrokeredMessageTests
{
    [Fact]
    public async Task CompleteAsync_InvokesDelegateOnlyOnce()
    {
        var completeCount = 0;
        var abandonCount = 0;

        var message = CreateMessage(
            _ =>
            {
                completeCount++;
                return Task.CompletedTask;
            },
            _ =>
            {
                abandonCount++;
                return Task.CompletedTask;
            });

        await message.CompleteAsync();
        await message.CompleteAsync();

        Assert.Equal(1, completeCount);
        Assert.Equal(0, abandonCount);

        await message.DisposeAsync();
        Assert.Equal(1, completeCount);
        Assert.Equal(0, abandonCount);
    }

    [Fact]
    public async Task AbandonAsync_InvokesDelegateOnlyOnce()
    {
        var completeCount = 0;
        var abandonCount = 0;

        var message = CreateMessage(
            _ =>
            {
                completeCount++;
                return Task.CompletedTask;
            },
            _ =>
            {
                abandonCount++;
                return Task.CompletedTask;
            });

        await message.AbandonAsync();
        await message.AbandonAsync();

        Assert.Equal(0, completeCount);
        Assert.Equal(1, abandonCount);
    }

    [Fact]
    public async Task DisposeAsync_AbandonsWhenStillPending()
    {
        var completeCount = 0;
        var abandonCount = 0;

        var message = CreateMessage(
            _ =>
            {
                completeCount++;
                return Task.CompletedTask;
            },
            _ =>
            {
                abandonCount++;
                return Task.CompletedTask;
            });

        await message.DisposeAsync();

        Assert.Equal(0, completeCount);
        Assert.Equal(1, abandonCount);
    }

    [Fact]
    public async Task DisposeAsync_DoesNotAbandonAfterCompletion()
    {
        var completeCount = 0;
        var abandonCount = 0;

        var message = CreateMessage(
            _ =>
            {
                completeCount++;
                return Task.CompletedTask;
            },
            _ =>
            {
                abandonCount++;
                return Task.CompletedTask;
            });

        await message.CompleteAsync();
        await message.DisposeAsync();

        Assert.Equal(1, completeCount);
        Assert.Equal(0, abandonCount);
    }

    [Fact]
    public void Deserialize_UsesProvidedSerializer()
    {
        var serializer = new FakeSerializer(new Payload { Value = 7 });
        var message = new BrokeredMessage(
            Guid.NewGuid(),
            "Hartonomous.Message",
            "{\"value\":7}",
            DateTimeOffset.UtcNow,
            _ => Task.CompletedTask,
            _ => Task.CompletedTask,
            serializer);

        var payload = message.Deserialize<Payload>();

        Assert.Equal("{\"value\":7}", serializer.CapturedJson);
        Assert.NotNull(payload);
        Assert.Equal(7, payload!.Value);
    }

    private static BrokeredMessage CreateMessage(
        Func<CancellationToken, Task> completeAsync,
        Func<CancellationToken, Task> abandonAsync)
    {
        return new BrokeredMessage(
            Guid.NewGuid(),
            "Hartonomous.Message",
            "{}",
            DateTimeOffset.UtcNow,
            completeAsync,
            abandonAsync);
    }

    private sealed class FakeSerializer : IJsonSerializer
    {
        private readonly object? _result;

        public FakeSerializer(object? result)
        {
            _result = result;
        }

        public string CapturedJson { get; private set; } = string.Empty;

        public string Serialize<T>(T value) => throw new NotSupportedException();

        public T? Deserialize<T>(string json)
        {
            CapturedJson = json;
            return (T?)_result;
        }
    }

    private sealed class Payload
    {
        public int Value { get; set; }
    }
}
