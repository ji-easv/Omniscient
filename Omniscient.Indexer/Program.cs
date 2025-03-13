using Microsoft.EntityFrameworkCore;
using Omniscient.Indexer.Domain.Services;
using Omniscient.Indexer.Infrastructure;
using Omniscient.Indexer.Infrastructure.Repository;
using Omniscient.RabbitMQClient;
using Omniscient.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = EnvironmentHelper.GetValue("POSTGRES_CONNECTION_STRING", builder.Configuration, "DefaultConnection");
    options.UseNpgsql(connectionString);
});

// Add services to the container.
builder.Services.AddScoped<IIndexerRepository, IndexerRepository>();
builder.Services.AddScoped<IIndexerService, IndexerService>();
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();

builder.AddServiceDefaults();
builder.AddRabbitMqDependencies();

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
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.MapControllers();

app.UseHttpsRedirection();

app.Run();
