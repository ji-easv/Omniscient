using System.Diagnostics;
using System.Reflection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Omniscient.ServiceDefaults;

public static class Monitoring
{
    public static readonly ActivitySource ActivitySource = new("Omniscient", "1.0.0");
    private static TracerProvider _tracerProvider;
    
    static Monitoring()
    {
        // Configure tracing
        var serviceName = Assembly.GetExecutingAssembly().GetName().Name;
        var version = "1.0.0";

        _tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddZipkinExporter()
            .AddSource(ActivitySource.Name)
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName: serviceName, serviceVersion: version))
            .Build();
    }
}