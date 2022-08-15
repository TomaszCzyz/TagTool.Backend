using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Exceptions;
using TagTool.Backend.Commands;
using TagTool.Backend.Constants;
using TagTool.Backend.DbContext;
using TagTool.Backend.Services;

var builder = WebApplication.CreateBuilder(args); // todo: check if this would not be enough: Host.CreateDefaultBuilder();

builder.Host.UseSerilog((_, configuration) =>
    configuration
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithExceptionDetails()
        .WriteTo.Console()
        .WriteTo.SQLite(Constants.LogsDbPath, storeTimestampInUtc: true, batchSize: 1));

builder.WebHost.ConfigureKestrel(ConfigureOptions);

builder.Services.AddSingleton<ICommandInvoker, CommandInvoker>();
builder.Services.AddGrpc();

var app = builder.Build();
app.Logger.LogInformation("Application created");

app.MapGrpcService<TagToolService>();
app.MapGrpcService<TagSearchService>();

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
