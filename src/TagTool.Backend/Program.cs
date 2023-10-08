#pragma warning disable CA1852
using System.Globalization;
using Hangfire;
using Hangfire.Storage.SQLite;
using MediatR.NotificationPublishers;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Extensions.Logging;
using Serilog.Formatting.Compact;
using TagTool.Backend.Constants;
using TagTool.Backend.DbContext;
using TagTool.Backend.Extensions;
using TagTool.Backend.Jobs;
using TagTool.Backend.Mappers;
using TagTool.Backend.Models;
using TagTool.Backend.Models.Tags;
using TagTool.Backend.Services;
using TagTool.Backend.Services.Grpc;

// todo: check if this would not be enough: Host.CreateDefaultBuilder() (or Slim version of builder);
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((_, configuration) =>
    configuration
        .Destructure.ByTransforming<TextTag>(tag
            => new
            {
                tag.Id,
                tag.Text,
                TaggedItemCount = tag.TaggedItems.Count
            }) // order 'Destructure' matters; derived tags -> tagBase
        .Destructure.With<TagBaseDeconstructPolicy>()
        .Destructure.ByTransforming<TaggableItem>(item => new { ItemId = item.Id, Tags = item.Tags.Names() })
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Warning)
        .MinimumLevel.Override("Hangfire.Storage.SQLite.ExpirationManager", LogEventLevel.Warning)
        .Filter.ByExcluding(c =>
            c.Properties.TryGetValue("EndpointName", out var endpointName)
            && endpointName.ToString() == "\"gRPC - /TagToolBackend.TagService/GetItem\"")
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProcessId()
        .Enrich.WithProcessName()
        .Enrich.WithExceptionDetails()
        .WriteTo.Seq("http://localhost:5341")
        .WriteTo.File(
            new CompactJsonFormatter(),
            $"{Constants.BasePath}/Logs/logs.json",
            rollingInterval: RollingInterval.Day)
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}]{NewLine} {Message:lj}{NewLine}{Exception}",
            formatProvider: CultureInfo.CurrentCulture));

builder.WebHost.ConfigureKestrel(ConfigureOptions);

var path = Constants.BasePath;
if (!Directory.Exists(path))
{
    Directory.CreateDirectory(path);
}

builder.Services.AddTagDtoMappers(typeof(Program));
builder.Services.AddSingleton<ITagMapper, TagMapper>();
builder.Services.AddSingleton<ICommandsHistory, CommandsHistory>();
builder.Services.AddScoped<IImplicitTagsProvider, ImplicitTagsProvider>();
builder.Services.AddScoped<ITagsRelationsManager, TagsRelationsManager>();
builder.Services.AddSingleton<ICustomFileSystemEnumerableFactory, CustomFileSystemEnumerableFactory>();
builder.Services.AddSingleton<ITagNameProvider, TagNameProvider>();
builder.Services.AddScoped<ICommonStoragePathProvider, CommonStoragePathProvider>();
builder.Services.AddScoped<ICommonStorage, CommonStorage>();
builder.Services.AddGrpc(options => options.EnableDetailedErrors = true);
builder.Services.AddMediatR(
    cfg =>
    {
        cfg.RegisterServicesFromAssemblyContaining<Program>();
        cfg.NotificationPublisher = new TaskWhenAllPublisher();
    });
builder.Services.AddDbContext<TagToolDbContext>(options
    => options
        .UseSqlite($"Data Source={Constants.DbPath}")
        .UseLoggerFactory(new SerilogLoggerFactory())
        .EnableDetailedErrors()
        .EnableSensitiveDataLogging());

builder.Services.AddHangfire(configuration => configuration.SetDataCompatibilityLevel(CompatibilityLevel.Version_180));

GlobalConfiguration.Configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSQLiteStorage(
        $"{Constants.BasePath}/hangfire.db") // the other matters!!! because during the registration the serializer settings are overwritten
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSerilogLogProvider();

builder.Services.AddHangfireServer();
builder.Services.AddJobs(typeof(Program));
builder.Services.AddSingleton<IJobFactory, JobFactory>();

var app = builder.Build();
app.Logger.LogInformation("Application created");

app.MapGrpcService<TagService>();
app.MapGrpcService<FileActionsService>();
app.MapGrpcService<FolderActionsService>();
app.MapGrpcService<FileSystemSearcher>();

using var scope = app.Services.CreateScope();
await using (var db = scope.ServiceProvider.GetRequiredService<TagToolDbContext>())
{
    app.Logger.LogInformation("Executing EF migrations...");
    db.Database.EnsureCreated();
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
