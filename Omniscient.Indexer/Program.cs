using Microsoft.EntityFrameworkCore;
using Omniscient.Indexer.Infrastructure;
using Omniscient.RabbitMQClient;
using Omniscient.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.AddServiceDefaults();

builder.Services.AddControllers();

builder.Services.AddRabbitMqDependencies();

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
