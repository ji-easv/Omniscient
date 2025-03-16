using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
namespace Omniscient.ServiceDefaults;

// Adds common .NET Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    private static string GetInstanceIdentifier()
    {
        // First check if we have a custom service instance identifier set
        var serviceInstance = Environment.GetEnvironmentVariable("SERVICE_INSTANCE_ID");
        if (!string.IsNullOrEmpty(serviceInstance))
        {
            return serviceInstance;
        }

        // Otherwise use container ID (hostname)
        var containerId = Environment.GetEnvironmentVariable("HOSTNAME") ?? Environment.MachineName;

        // Try to get service name from environment
        var serviceName = Environment.GetEnvironmentVariable("SERVICE_NAME");
        if (!string.IsNullOrEmpty(serviceName))
        {
            return $"{serviceName}.{containerId[..8]}";
        }

        return containerId;
    }

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureSerilog();

        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var otelEndpoint = EnvironmentHelper.GetValue("OTEL_EXPORTER_OTLP_ENDPOINT", builder.Configuration);

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        var instanceId = builder.Properties.TryGetValue("InstanceId", out var id)
            ? id.ToString()
            : GetInstanceIdentifier();

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                // Add service name and instance ID to resource attributes for easier trace filtering
                resource.AddAttributes(new[]
                {
                    new KeyValuePair<string, object>("service.replica.id", instanceId)
                });

                resource.AddService(serviceName: builder.Environment.ApplicationName);
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddPrometheusExporter()
                    .AddMeter("Omniscient.Indexer")
                    // Metrics provides by ASP.NET Core in .NET 8
                    .AddMeter("Microsoft.AspNetCore.Hosting")
                    .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                    // Metrics provided by System.Net libraries
                    .AddMeter("System.Net.Http")
                    .AddMeter("System.Net.NameResolution")
                    .AddPrometheusExporter();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(ActivitySources.OmniscientActivitySource.Name)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddZipkinExporter(); // configured through OTEL_EXPORTER_ZIPKIN_ENDPOINT
            })
            .UseOtlpExporter(OtlpExportProtocol.Grpc, new Uri(otelEndpoint));

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks("/health");

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }


        return app;
    }

    private static TBuilder ConfigureSerilog<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.ClearProviders();

        Log.Logger = new LoggerConfiguration()
            // Set the default minimum level as Debug
            .MinimumLevel.Is(LogEventLevel.Debug)
            // Override specific categories:
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            // Enrich log events with additional context information
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .Enrich.WithMachineName()
            // Write logs to console with a custom output template
            .WriteTo.Console(
                outputTemplate:
                "{Timestamp:yyyy-MM-dd HH:mm:ss}|{MachineName}|{ThreadId}|{RequestId}|{Level:u3}|{Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        builder.Logging.AddSerilog();

        return builder;
    }
}

public static class ActivitySources
{
    public static readonly ActivitySource OmniscientActivitySource = new("Omniscient", "1.0.0");
}