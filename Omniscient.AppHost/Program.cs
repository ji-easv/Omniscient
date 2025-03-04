using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Omniscient_Web>("OmniscientWeb", launchProfileName: "https");

builder.AddProject<Omniscient_Cleaner>("OmniscientCleaner", launchProfileName: "https");

builder.AddProject<Omniscient_Indexer>("OmniscientIndexer", launchProfileName: "https");

builder.Build().Run();