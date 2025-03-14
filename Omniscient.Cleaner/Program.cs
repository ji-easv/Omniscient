using Omniscient.Cleaner.Domain.Services;
using Omniscient.RabbitMQClient;
using Omniscient.ServiceDefaults;
using Omniscient.Cleaner.Infrastructure;
using Omniscient.Cleaner.Infrastructure.Interfaces;
using Omniscient.Cleaner.Utilities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.AddRabbitMqDependencies();
builder.AddServiceDefaults();

builder.Services.AddTransient<IFileSystemRepository, FileSystemRepository>();

builder.Services.AddHostedService(sp => new FileSystemService(
    sp.GetRequiredService<IFileSystemRepository>(),
    args
));

builder.Services.Configure<FileSystemOptions>(options =>
{
    options.LimitFiles = args.All(arg => arg != "--no-limit");
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();