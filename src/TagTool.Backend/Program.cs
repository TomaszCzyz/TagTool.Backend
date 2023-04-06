#pragma warning disable CA1852
using System.Globalization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Extensions.Logging;
using TagTool.Backend.Constants;
using TagTool.Backend.DbContext;
using TagTool.Backend.Extensions;
using TagTool.Backend.Models;
using TagTool.Backend.Services;
using TagTool.Backend.Services.Grpc;

var builder = WebApplication.CreateBuilder(args); // todo: check if this would not be enough: Host.CreateDefaultBuilder();

builder.Host.UseSerilog((_, configuration) =>
    configuration
        .Destructure.ByTransforming<TaggedItem>(item
            => new
            {
                item.ItemType,
                item.UniqueIdentifier,
                Tags = item.Tags.Names()
            })
        .Destructure.ByTransforming<Tag>(tag => new { tag.Name, TaggedItem = tag.TaggedItems.Select(item => item.UniqueIdentifier).ToArray() })
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithExceptionDetails()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}]{NewLine} {Message:lj}{NewLine}{Exception}",
            formatProvider: CultureInfo.CurrentCulture)
        .WriteTo.SQLite(Constants.LogsDbPath, formatProvider: CultureInfo.CurrentCulture, storeTimestampInUtc: true, batchSize: 20));

builder.WebHost.ConfigureKestrel(ConfigureOptions);

var path = Constants.BasePath;
if (!Directory.Exists(path))
{
    Directory.CreateDirectory(path);
}

builder.Services.AddSingleton<ICommandsHistory, CommandsHistory>();
builder.Services.AddScoped<ICommonStoragePathProvider, CommonStoragePathProvider>();
builder.Services.AddGrpc();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddDbContext<TagToolDbContext>(options
    => options
        .UseSqlite($"Data Source={Constants.DbPath}")
        .UseLoggerFactory(new SerilogLoggerFactory())
        .EnableDetailedErrors()
        .EnableSensitiveDataLogging());

var app = builder.Build();
app.Logger.LogInformation("Application created");

app.MapGrpcService<TagService>();
app.MapGrpcService<FileActionsService>();
app.MapGrpcService<FolderActionsService>();

using var scope = app.Services.CreateScope();
await using (var db = scope.ServiceProvider.GetRequiredService<TagToolDbContext>())
{
    app.Logger.LogInformation("Executing EF migrations...");
    db.Database.Migrate();
}

app.Logger.LogInformation("Launching application...");
await app.RunAsync();

Log.CloseAndFlush();

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
