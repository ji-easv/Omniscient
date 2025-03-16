using System.Diagnostics.Metrics;

namespace Omniscient.ServiceDefaults;

public static class CustomMetrics
{
    private static readonly Meter IndexerMeter = new("Omniscient.Indexer", "1.0.0");

    public static readonly Histogram<double> IndexingDurationHistogram =
        IndexerMeter.CreateHistogram<double>("indexing.duration", "ms",
            "Duration of indexing of a batch of e-mails in milliseconds");
    
    public static readonly Histogram<double> SearchPerformanceHistogram =
        IndexerMeter.CreateHistogram<double>("search.performance", "ms",
            "Duration of searching through all e-mails in milliseconds");
}