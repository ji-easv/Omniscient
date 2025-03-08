using Omniscient.ServiceDefaults;

using Omniscient.Cleaner.Infrastructure;
using Omniscient.Cleaner.Infrastructure.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.AddServiceDefaults();
builder.Services.AddTransient<IFileSystemRepository, FileSystemRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (args.Contains("init") || args.Contains("--init"))
{
    using (var scope = app.Services.CreateScope())
    {
        var fileSystemRepository = scope.ServiceProvider.GetService<IFileSystemRepository>();

        if (fileSystemRepository != null)
        {
            var fileContent = await fileSystemRepository.GetFiles(
                "/Users/mazur/Projects/School/PBSf25/1stSemester/DevelopmentOfLargeSystems/Week10-11/Omniscient/.enron-files/maildir");
        }
    }
}



app.UseHttpsRedirection();

app.Run();
