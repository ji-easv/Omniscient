using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Omniscient.RabbitMQClient.Messages;

public abstract class RabbitMQMessage
{
    private readonly Dictionary<string, object> _headers = new();
    public ActivityContext ActivityContext { get; set; }

    public void PropagateContext(Activity? activity)
    {
        var activityContext = activity?.Context ?? Activity.Current?.Context ?? default;
        var propagationContext = new PropagationContext(activityContext, Baggage.Current);
        var propagator = new TraceContextPropagator();
        propagator.Inject(propagationContext, this, (req, key, value) => req._headers.Add(key, value));
    }

    public void ExtractPropagatedContext()
    {
        var propagator = new TraceContextPropagator();
        var parentContext = propagator.Extract(default, this, (req, key) =>
        {
            return new List<string>([req._headers.ContainsKey(key) ? req._headers[key].ToString() : string.Empty]);
        });
        Baggage.Current = parentContext.Baggage;
        ActivityContext = parentContext.ActivityContext;
    }
}