using EasyNetQ;
using Microsoft.Extensions.Logging;

namespace Omniscient.RabbitMQClient;

public class RabbitMqConnection : IDisposable
{
    private readonly IBus _bus;
    private readonly ILogger<RabbitMqConnection> _logger;
    private readonly Timer _reconnectTimer;
    private readonly object _reconnectLock = new();
    private bool _isReconnecting;
    
    // Event to notify subscribers when connection state changes
    public event EventHandler<bool> ConnectionStateChanged;

    public bool IsConnected { get; private set; }

    public RabbitMqConnection(IBus bus, ILogger<RabbitMqConnection> logger)
    {
        _bus = bus;
        _logger = logger;
        
        _reconnectTimer = new Timer(TryReconnect, null, Timeout.Infinite, Timeout.Infinite);
        
        // Subscribe to connection events
        _bus.Advanced.Connected += (sender, args) => {
            SetConnectedState(true);
            _logger.LogInformation("RabbitMQ connection established");
        };
        
        _bus.Advanced.Disconnected += (sender, args) => {
            SetConnectedState(false);
            _logger.LogWarning("RabbitMQ disconnected");
            _reconnectTimer.Change(TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
        };
        
        // Check initial connection state
        if (_bus.Advanced.IsConnected)
            SetConnectedState(true);
        else
        {
            SetConnectedState(false);
            _reconnectTimer.Change(TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan);
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

    private async void TryReconnect(object? state)
    {
        lock (_reconnectLock)
        {
            if (_isReconnecting || IsConnected) return;
            _isReconnecting = true;
        }

        try
        {
            _logger.LogInformation("Attempting to reconnect to RabbitMQ...");

            // Force reconnection by accessing the connection
            var connected = _bus.Advanced.IsConnected;
            if (!connected)
            {
                try
                {
                    // This is a lightweight operation that will force a connection attempt
                    await _bus.Advanced.ExchangeDeclareAsync("health.check.exchange", "direct", false, true);
                    SetConnectedState(true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to reconnect to RabbitMQ");
                    _reconnectTimer.Change(TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
                }
            }
            else
            {
                SetConnectedState(true);
            }
        }
        finally
        {
            lock (_reconnectLock)
            {
                _isReconnecting = false;
            }
        }
    }

    public IBus GetBus() => _bus;

    public void Dispose()
    {
        _reconnectTimer?.Dispose();
    }
}