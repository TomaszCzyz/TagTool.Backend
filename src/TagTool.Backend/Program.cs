﻿#pragma warning disable CA1852
using System.Globalization;
using MediatR;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;
using Serilog.Exceptions;
using TagTool.Backend.Commands;
using TagTool.Backend.Constants;
using TagTool.Backend.Repositories;
using TagTool.Backend.Services;

var builder = WebApplication.CreateBuilder(args); // todo: check if this would not be enough: Host.CreateDefaultBuilder();

builder.Host.UseSerilog((_, configuration) =>
    configuration
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithExceptionDetails()
        .WriteTo.Console(formatProvider: CultureInfo.CurrentCulture)
        .WriteTo.SQLite(Constants.LogsDbPath, storeTimestampInUtc: true, batchSize: 1, formatProvider: CultureInfo.CurrentCulture));

builder.WebHost.ConfigureKestrel(ConfigureOptions);

builder.Services.AddSingleton<IConnectionsFactory, ConnectionsFactory>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
builder.Services.AddGrpc();
builder.Services.AddMediatR(typeof(Program));

var app = builder.Build();
app.Logger.LogInformation("Application created");

app.MapGrpcService<TagService>();
// app.MapGrpcService<TagSearchService>();

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
