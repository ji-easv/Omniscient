using EasyNetQ;
using Microsoft.EntityFrameworkCore;
using Omniscient.Indexer.Infrastructure;
using Omniscient.RabbitMQClient.Messages;
using Omniscient.Shared.Entities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.AddServiceDefaults();

builder.Services.AddEasyNetQ("host=localhost").UseSystemTextJson();

using var provider = builder.Services.BuildServiceProvider();
var bus = provider.GetRequiredService<IBus>();

var input = string.Empty;
Console.WriteLine("Enter a message. 'Quit' to quit.");
while ((input = Console.ReadLine()) != "Quit")
{
    await bus.PubSub.PublishAsync(new EmailMessage { Email = new Email
    {
        Content = "This is an email",
        FileName = "email.txt",
        Id = Guid.NewGuid(),
    }}, "EmailMessage");
    Console.WriteLine("Message published!");
}

var app = builder.Build();

// Apply any pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
