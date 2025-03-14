#pragma warning disable CA1848
using System.Globalization;
using System.Text.Json;
using Coravel;
using Coravel.Scheduling.Schedule.Interfaces;
using FluentValidation;
using MediatR.NotificationPublishers;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Extensions.Logging;
using Serilog.Formatting.Compact;
using TagTool.BackendNew;
using TagTool.BackendNew.Broadcasting;
using TagTool.BackendNew.Broadcasting.Listeners;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.DbContexts;
using TagTool.BackendNew.Entities;
using TagTool.BackendNew.Invocables;
using TagTool.BackendNew.Options;
using TagTool.BackendNew.Services;
using TagTool.BackendNew.Services.Grpc;
using TagTool.BackendNew.Services.HostedServices;
using TagTool.BackendNew.TaggableFile;

// todo: check if this would not be enough: Host.CreateDefaultBuilder() (or Slim version of builder);
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((_, configuration) =>
    configuration
        .Destructure.With<TagBaseDeconstructPolicy>()
        .Destructure.ByTransforming<TaggableItem>(item => new
        {
            ItemId = item.Id, Tags = item.Tags.Select(tag => tag.Text).ToArray()
        })
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
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <{SourceContext}>{NewLine}{Exception}",
            formatProvider: CultureInfo.CurrentCulture));

builder.WebHost.ConfigureKestrel(ConfigureOptions);

var path = Constants.BasePath;
if (!Directory.Exists(path))
{
    Directory.CreateDirectory(path);
}

builder.Services.AddSingleton<UserConfiguration>(
    provider =>
    {
        var appOptions = provider.GetRequiredService<IOptions<AppOptions>>();

        if (File.Exists(appOptions.Value.UserConfigFilePath))
        {
            var userConfiguration = JsonSerializer.Deserialize<UserConfiguration>(File.OpenRead(appOptions.Value.UserConfigFilePath));

            if (userConfiguration is not null)
            {
                return userConfiguration;
            }

            Log.Warning("Could not load user configuration file");
        }

        return new UserConfiguration();
    });
builder.Services.AddSingleton<UserConfigurationWatcher>();
builder.Services.AddSingleton<IOperationManger, OperationManger>();
builder.Services.AddSingleton<ITaggableItemManager<TaggableItem>, TaggableItemManagerDispatcher>();
builder.Services.AddSingleton<TaggableItemMapper>();

builder.Services.AddScoped<TaggableFileManager>();
builder.Services.AddScoped<InvocablesManager>();
builder.Services.AddTransient<ItemTagsChangedEventListener>();

// cron triggered jobs
builder.Services.AddScoped<CronMoveToCommonStorage>();
builder.Services.AddScoped<IQueuingHandler<CronMoveToCommonStorage, CronMoveToCommonStoragePayload>, CronMoveToCommonStorageQueuingHandler>();

// event triggered jobs
builder.Services.AddScoped<MoveToCommonStorage>();
builder.Services.AddScoped<IQueuingHandler<MoveToCommonStorage, MoveToCommonStoragePayload>, MoveToCommonStorageQueuingHandler>();

builder.Services.AddHostedService<NewFilesTagger>();

// builder.Services.AddSingleton<ICommandsHistory, CommandsHistory>();
// builder.Services.AddSingleton<ICustomFileSystemEnumerableFactory, CustomFileSystemEnumerableFactory>();
// builder.Services.AddSingleton<ITagNameProvider, TagNameProvider>();
builder.Services.AddScheduler();
builder.Services.AddQueue();
builder.Services.AddEvents();
builder.Services.AddGrpc(options => options.EnableDetailedErrors = true);
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddMediatR(
    cfg =>
    {
        cfg.RegisterServicesFromAssemblyContaining<Program>();
        cfg.NotificationPublisher = new TaskWhenAllPublisher();
    });
builder.Services.AddDbContext<ITagToolDbContext, TagToolDbContext>(options
    => options
        .UseSqlite($"Data Source={Constants.DbPath}")
        .UseLoggerFactory(new SerilogLoggerFactory())
        .EnableDetailedErrors()
        .EnableSensitiveDataLogging());

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddGrpcReflection();
}

var app = builder.Build();
app.Logger.LogInformation("Application created");

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapGrpcService<TagsGrpcService>();
app.MapGrpcService<InvocablesGrpcService>();

var eventRegistration = app.Services.ConfigureEvents();
eventRegistration
    .Register<ItemTagsChangedEvent>()
    .Subscribe<ItemTagsChangedEventListener>();

app.Services
    .UseScheduler(_ => { })
    .OnError(exception => Log.Error(exception, "Error in scheduler"));

using (var scope = app.Services.CreateScope())
{
    await using (var db = scope.ServiceProvider.GetRequiredService<ITagToolDbContext>())
    {
        app.Logger.LogInformation("Executing migrations...");
        // it freezes with .NET 9 nuget versions, use cli for now
        // await db.Database.MigrateAsync();

        app.Logger.LogInformation("Scheduling cron invocables...");
        var scheduler = scope.ServiceProvider.GetRequiredService<IScheduler>();
        await foreach (var invocableInfo in db.CronTriggeredInvocableInfos)
        {
            app.Logger.LogInformation("Scheduling cron invocable {@InvocableInfo}", invocableInfo);
            scheduler
                .ScheduleWithParams<CronInvocableQueuingHandler>(invocableInfo.Id)
                .Cron(invocableInfo.CronExpression);
        }
    }
}

app.Logger.LogInformation("Launching application...");
await app.RunAsync();

Log.CloseAndFlush();
return;


void ConfigureOptions(KestrelServerOptions kestrelServerOptions)
{
    if (Environment.GetEnvironmentVariable("USE_GRPC_VIA_HTTP") != "true")
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
