using System.Collections.Concurrent;
using System.Diagnostics;
using EasyNetQ;
using Microsoft.Extensions.Logging;
using Omniscient.RabbitMQClient.Interfaces;
using Omniscient.RabbitMQClient.Messages;
using Omniscient.ServiceDefaults;

namespace Omniscient.RabbitMQClient.Implementations;

public class RabbitMqPublisher : IAsyncPublisher
{
    private readonly ConcurrentQueue<RabbitMqMessage> _pendingMessages = new();
    private readonly RabbitMqConnection _connection;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly Timer _retryTimer;

    public RabbitMqPublisher(RabbitMqConnection connection, ILogger<RabbitMqPublisher> logger)
    {
        _connection = connection;
        _logger = logger;
        _retryTimer = new(TryProcessPendingMessages, null, Timeout.Infinite, Timeout.Infinite);
        _connection.ConnectionStateChanged += OnConnectionStateChanged;
    }

    public async Task PublishAsync<T>(T message, CancellationToken token = default)
    {
        if (message is not RabbitMqMessage rabbitMqMessage)
        {
            throw new ArgumentException("Message must be of type RabbitMqMessage");
        }

        using var activity = ActivitySources.OmniscientActivitySource.StartActivity(ActivityKind.Producer);
        rabbitMqMessage.PropagateContext(activity);

        try
        {
            if (_connection.IsConnected)
            {
                await _connection.GetBus().PubSub.PublishAsync(message, token);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish message, queueing for retry");
            _connection.SetConnectedState(false);
        }

        // Queue message for retry
        _pendingMessages.Enqueue(rabbitMqMessage);

        if (_pendingMessages.Count == 1)
            _retryTimer.Change(TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
    }

    private async void TryProcessPendingMessages(object? state)
    {
        if (!_connection.IsConnected || _pendingMessages.IsEmpty)
            return;

        _logger.LogInformation("Processing {Count} pending messages", _pendingMessages.Count);

        var processedCount = 0;
        var tempQueue = new List<RabbitMqMessage>();

        // Process all messages
        while (_pendingMessages.TryDequeue(out var message))
        {
            try
            {
                await _connection.GetBus().PubSub.PublishAsync(message, CancellationToken.None);
                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish message, queueing for retry");
                tempQueue.Add(message);
            }
            
            foreach (var item in tempQueue)
            {
                _pendingMessages.Enqueue(item);
            }
        }

        _logger.LogInformation("Successfully processed {Count} pending messages, {Remaining} remaining",
            processedCount, _pendingMessages.Count);

        // Schedule next retry if there are still messages
        if (!_pendingMessages.IsEmpty)
            _retryTimer.Change(TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
    }

    private void OnConnectionStateChanged(object? sender, bool isConnected)
    {
        if (isConnected && !_pendingMessages.IsEmpty)
        {
            _logger.LogInformation("Connection restored. Scheduling retry for {Count} pending messages",
                _pendingMessages.Count);
            _retryTimer.Change(TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan);
        }
    }
}