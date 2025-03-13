using EasyNetQ;
using Microsoft.Extensions.Logging;

namespace Omniscient.RabbitMQClient;

public class RabbitMqConnection : IDisposable
{
    private readonly IBus _bus;
    private readonly ILogger<RabbitMqConnection> _logger;
    private readonly Timer _reconnectTimer;
    private readonly object _stateLock = new();
    
    // Circuit breaker settings
    private readonly int _failureThreshold = 3;
    private readonly TimeSpan _circuitResetTimeout = TimeSpan.FromSeconds(30);
    private CircuitState _circuitState = CircuitState.Closed;
    private int _failureCount;
    private DateTime _circuitOpenedTime;

    public event EventHandler<bool> ConnectionStateChanged;
    public bool IsConnected { get; private set; }

    private enum CircuitState
    {
        Closed,     // Normal operation, connections allowed
        Open,       // Circuit breaker tripped, no connection attempts
        HalfOpen    // Testing if connection can be restored
    }

    public RabbitMqConnection(IBus bus, ILogger<RabbitMqConnection> logger)
    {
        _bus = bus;
        _logger = logger;
        _reconnectTimer = new Timer(TryReconnect, null, Timeout.Infinite, Timeout.Infinite);

        _bus.Advanced.Connected += (sender, args) => {
            SetConnectedState(true);
            ResetCircuitBreaker();
            _logger.LogInformation("RabbitMQ connection established");
        };

        _bus.Advanced.Disconnected += (sender, args) => {
            SetConnectedState(false);
            _logger.LogWarning("RabbitMQ disconnected");
            ScheduleReconnection();
        };

        // Check initial connection state
        if (_bus.Advanced.IsConnected)
            SetConnectedState(true);
        else
        {
            SetConnectedState(false);
            ScheduleReconnection(TimeSpan.FromSeconds(1));
        }
    }

    public void SetConnectedState(bool connected)
    {
        if (IsConnected != connected)
        {
            IsConnected = connected;
            ConnectionStateChanged?.Invoke(this, connected);
        }
    }

    private void ScheduleReconnection(TimeSpan? delay = null)
    {
        lock (_stateLock)
        {
            switch (_circuitState)
            {
                case CircuitState.Closed:
                    _reconnectTimer.Change(delay ?? TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
                    break;
                case CircuitState.HalfOpen:
                    _reconnectTimer.Change(delay ?? TimeSpan.FromSeconds(2), Timeout.InfiniteTimeSpan);
                    break;
                case CircuitState.Open:
                    // When circuit is open, only schedule after timeout period
                    var timeInOpen = DateTime.UtcNow - _circuitOpenedTime;
                    if (timeInOpen >= _circuitResetTimeout)
                    {
                        _circuitState = CircuitState.HalfOpen;
                        _logger.LogInformation("Circuit breaker state changed to Half-Open");
                        _reconnectTimer.Change(TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan);
                    }
                    else
                    {
                        var remainingTime = _circuitResetTimeout - timeInOpen;
                        _reconnectTimer.Change(remainingTime, Timeout.InfiniteTimeSpan);
                    }
                    break;
            }
        }
    }

    private void TripCircuitBreaker()
    {
        lock (_stateLock)
        {
            _circuitState = CircuitState.Open;
            _circuitOpenedTime = DateTime.UtcNow;
            _logger.LogWarning("Circuit breaker tripped open. Stopping reconnection attempts for {Timeout} seconds", 
                _circuitResetTimeout.TotalSeconds);
            ScheduleReconnection(_circuitResetTimeout);
        }
    }

    private void ResetCircuitBreaker()
    {
        lock (_stateLock)
        {
            _circuitState = CircuitState.Closed;
            _failureCount = 0;
            _logger.LogInformation("Circuit breaker reset to closed state");
        }
    }

    private async void TryReconnect(object? state)
    {
        lock (_stateLock)
        {
            if (IsConnected) return;
            
            if (_circuitState == CircuitState.Open)
            {
                ScheduleReconnection();
                return;
            }
        }

        try
        {
            _logger.LogInformation("Attempting to reconnect to RabbitMQ... (Circuit state: {State})", _circuitState);

            if (!_bus.Advanced.IsConnected)
            {
                try
                {
                    await _bus.Advanced.ExchangeDeclareAsync("health.check.exchange", "direct", false, true);
                    SetConnectedState(true);
                    ResetCircuitBreaker();
                }
                catch (Exception ex)
                {
                    lock (_stateLock)
                    {
                        _failureCount++;
                        _logger.LogWarning(ex, "Failed to reconnect to RabbitMQ (Attempt {Count}/{Threshold})", 
                            _failureCount, _failureThreshold);
                        
                        if (_failureCount >= _failureThreshold && _circuitState != CircuitState.Open)
                        {
                            TripCircuitBreaker();
                        }
                        else
                        {
                            ScheduleReconnection();
                        }
                    }
                }
            }
            else
            {
                SetConnectedState(true);
                ResetCircuitBreaker();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during reconnection attempt");
            lock (_stateLock)
            {
                _failureCount++;
                if (_failureCount >= _failureThreshold && _circuitState != CircuitState.Open)
                {
                    TripCircuitBreaker();
                }
                else
                {
                    ScheduleReconnection();
                }
            }
        }
    }

    public IBus GetBus() => _bus;

    public void Dispose()
    {
        _reconnectTimer?.Dispose();
    }
}