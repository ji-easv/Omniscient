using EasyNetQ;
using Microsoft.Extensions.Logging;
using Omniscient.ServiceDefaults;

namespace Omniscient.RabbitMQClient;

public class RabbitMqConnection : IDisposable
{
    private readonly ILogger<RabbitMqConnection> _logger;
    private readonly CircuitBreaker _circuitBreaker;
    private readonly Timer _reconnectTimer;
    private bool _reconnectPending;

    public event EventHandler<bool> ConnectionStateChanged;
    public bool IsConnected { get; private set; }
    public IBus Bus { get; }

    public RabbitMqConnection(IBus bus, ILogger<RabbitMqConnection> logger)
    {
        Bus = bus;
        _logger = logger;
        _reconnectTimer = new Timer(ReconnectTimerCallback, null, Timeout.Infinite, Timeout.Infinite);

        _circuitBreaker = new CircuitBreaker();
        _circuitBreaker.OnHalfOpen += (_, _) => TryReconnect();
        _circuitBreaker.OnOpen += (_, _) => SetConnectedState(false);
        _circuitBreaker.OnClosed += (_, _) => SetConnectedState(true);

        Bus.Advanced.Connected += (sender, args) =>
        {
            SetConnectedState(true);
            _circuitBreaker.ResetCircuitBreaker();
            _logger.LogInformation("RabbitMQ connection established");
        };

        Bus.Advanced.Disconnected += (sender, args) =>
        {
            SetConnectedState(false);
            _logger.LogWarning("RabbitMQ disconnected");
            TryReconnect();
        };

        // Check initial connection state
        if (Bus.Advanced.IsConnected)
            SetConnectedState(true);
        else
        {
            SetConnectedState(false);
            ScheduleReconnect(TimeSpan.FromSeconds(1));
        }
    }

    private void ScheduleReconnect(TimeSpan delay)
    {
        if (_reconnectPending)
            return;

        _reconnectPending = true;
        _logger.LogInformation("Scheduling reconnection attempt in {Delay} seconds", delay.TotalSeconds);
        _reconnectTimer.Change(delay, Timeout.InfiniteTimeSpan);
    }

    private void ReconnectTimerCallback(object state)
    {
        _reconnectPending = false;
        TryReconnect();
    }

    public void SetConnectedState(bool connected)
    {
        if (IsConnected != connected)
        {
            IsConnected = connected;
            ConnectionStateChanged?.Invoke(this, connected);
        }
    }

    private async void TryReconnect()
    {
        if (IsConnected) return;

        try
        {
            await _circuitBreaker.ExecuteAsync(async () =>
            {
                await Bus.Advanced.ExchangeDeclareAsync("health.check.exchange", "direct", false, true);
            });
        }
        catch (CircuitBreakerOpenException)
        {
            _logger.LogWarning("Circuit breaker is open, skipping reconnection attempt");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");

            // Schedule another reconnection attempt based on circuit state
            TimeSpan delay = _circuitBreaker.State switch
            {
                CircuitState.Closed => TimeSpan.FromSeconds(5),
                CircuitState.HalfOpen => TimeSpan.FromSeconds(2),
                _ => TimeSpan.FromSeconds(30)
            };

            ScheduleReconnect(delay);
        }
    }

    public void Dispose()
    {
        _reconnectTimer?.Dispose();
        _circuitBreaker?.Dispose();
    }
}