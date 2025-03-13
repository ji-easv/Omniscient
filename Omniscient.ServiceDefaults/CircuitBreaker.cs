using Serilog;

namespace Omniscient.ServiceDefaults;

public class CircuitBreaker : IDisposable
{
    private const int FailureThreshold = 3;
    private readonly TimeSpan _circuitResetTimeout = TimeSpan.FromSeconds(30);
    private CircuitState _circuitState = CircuitState.Closed;
    private int _failureCount;
    private readonly object _stateLock = new();
    private readonly Timer _stateTransitionTimer;
    
    public event EventHandler? OnClosed;
    public event EventHandler? OnOpen;
    public event EventHandler? OnHalfOpen;
    
    public CircuitBreaker()
    {
        _stateTransitionTimer = new Timer(StateTransitionCallback, null, Timeout.Infinite, Timeout.Infinite);
    }
    
    public CircuitState State 
    {
        get 
        {
            lock(_stateLock) 
            {
                return _circuitState;
            }
        }
    }
    
    public void ResetCircuitBreaker()
    {
        lock (_stateLock)
        {
            var previousState = _circuitState;
            _circuitState = CircuitState.Closed;
            _failureCount = 0;
            Log.Information("Circuit breaker reset to closed state");
            
            if (previousState != CircuitState.Closed)
            {
                OnClosed?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void TripCircuitBreaker()
    {
        lock (_stateLock)
        {
            if (_circuitState == CircuitState.Open)
                return;
                
            _circuitState = CircuitState.Open;
            Log.Warning("Circuit breaker tripped open. Stopping attempts for {Timeout} seconds",
                _circuitResetTimeout.TotalSeconds);
                
            OnOpen?.Invoke(this, EventArgs.Empty);
            
            // Schedule transition to half-open after timeout
            _stateTransitionTimer.Change(_circuitResetTimeout, Timeout.InfiniteTimeSpan);
        }
    }
    
    public void RecordFailure()
    {
        lock (_stateLock)
        {
            _failureCount++;
            Log.Warning("Failure recorded (Count: {Count}/{Threshold})", _failureCount, FailureThreshold);

            if (_failureCount >= FailureThreshold && _circuitState != CircuitState.Open)
            {
                TripCircuitBreaker();
            }
        }
    }
    
    public void RecordSuccess()
    {
        lock (_stateLock)
        {
            if (_circuitState == CircuitState.HalfOpen)
            {
                ResetCircuitBreaker();
            }
        }
    }
    
    public async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action)
    {
        lock (_stateLock)
        {
            if (_circuitState == CircuitState.Open)
            {
                throw new CircuitBreakerOpenException("Circuit breaker is open");
            }
        }

        try
        {
            var result = await action();
            RecordSuccess();
            return result;
        }
        catch (Exception)
        {
            RecordFailure();
            throw;
        }
    }

    public async Task ExecuteAsync(Func<Task> action)
    {
        lock (_stateLock)
        {
            if (_circuitState == CircuitState.Open)
            {
                throw new CircuitBreakerOpenException("Circuit breaker is open");
            }
        }

        try
        {
            await action();
            RecordSuccess();
        }
        catch (Exception)
        {
            RecordFailure();
            throw;
        }
    }
    
    private void StateTransitionCallback(object? state)
    {
        lock (_stateLock)
        {
            if (_circuitState == CircuitState.Open)
            {
                _circuitState = CircuitState.HalfOpen;
                Log.Information("Circuit breaker state changed to Half-Open");
                OnHalfOpen?.Invoke(this, EventArgs.Empty);
            }
        }
    }
    
    public void Dispose()
    {
        _stateTransitionTimer.Dispose();
    }
}

public enum CircuitState
{
    Closed,     // Normal operation, connections allowed
    Open,       // Circuit breaker tripped, no connection attempts
    HalfOpen    // Testing if connection can be restored
}

public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message) { }
}