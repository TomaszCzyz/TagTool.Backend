#pragma warning disable CA1852
using System.Globalization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Exceptions;
using TagTool.Backend.Constants;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models;
using TagTool.Backend.Services;

var builder = WebApplication.CreateBuilder(args); // todo: check if this would not be enough: Host.CreateDefaultBuilder();

builder.Host.UseSerilog((_, configuration) =>
    configuration
        .Destructure.ByTransforming<TaggedItem>(
            item => new { item.ItemType, item.UniqueIdentifier, Tags = item.Tags.Select(tag => tag.Name).ToArray() })
        .MinimumLevel.Information()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithExceptionDetails()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}]{NewLine} {Message:lj}{NewLine}{Exception}",
            formatProvider: CultureInfo.CurrentCulture)
        .WriteTo.SQLite(Constants.LogsDbPath, formatProvider: CultureInfo.CurrentCulture, storeTimestampInUtc: true, batchSize: 1));

builder.WebHost.ConfigureKestrel(ConfigureOptions);

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddGrpc();

var app = builder.Build();
app.Logger.LogInformation("Application created");

app.MapGrpcService<TagService>();
app.MapGrpcService<FileActionsService>();

app.Logger.LogInformation("Executing EF migrations...");
await using (var db = new TagContext())
{
    db.Database.Migrate();
}

app.Logger.LogInformation("Launching application...");
await app.RunAsync();

void ConfigureOptions(KestrelServerOptions kestrelServerOptions)
{
    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "GrpcDevelopment")
    {
        if (File.Exists(Constants.SocketPath))
        {
            File.Delete(Constants.SocketPath);
        }

        kestrelServerOptions.ListenUnixSocket(Constants.SocketPath, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
    }
    else
    {
        kestrelServerOptions.ListenLocalhost(5280, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
    }
}
