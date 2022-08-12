using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Exceptions;
using TagTool.Backend.Commands;
using TagTool.Backend.Constants;
using TagTool.Backend.DbContext;
using TagTool.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((_, configuration) =>
    configuration
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithExceptionDetails()
        .WriteTo.Console()
        .WriteTo.SQLite(Constants.LogsDbPath));

builder.WebHost.ConfigureKestrel(
    options =>
    {
        if (File.Exists(Constants.SocketPath))
        {
            File.Delete(Constants.SocketPath);
        }

        options.ListenUnixSocket(Constants.SocketPath, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
    });

builder.Services.AddSingleton<ICommandInvoker, CommandInvoker>();
builder.Services.AddGrpc();

var app = builder.Build();
app.Logger.LogInformation("Application created...");

app.MapGrpcService<TagToolService>();

app.Logger.LogInformation("Executing EF migrations...");
await using (var db = new TagContext())
{
    db.Database.Migrate();
}

app.Logger.LogInformation("Launching application...");
await app.RunAsync();
