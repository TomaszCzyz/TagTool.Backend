#pragma warning disable CA1852
using System.Globalization;
using LiteDB;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;
using Serilog.Exceptions;
using TagTool.Backend.Constants;
using TagTool.Backend.Models.Taggable;
using TagTool.Backend.Repositories;
using TagTool.Backend.Repositories.Dtos;
using TagTool.Backend.Services;
using TagTool.Backend.Taggers;
using File = TagTool.Backend.Models.Taggable.File;

var builder = WebApplication.CreateBuilder(args); // todo: check if this would not be enough: Host.CreateDefaultBuilder();

builder.Host.UseSerilog((_, configuration) =>
    configuration
        .MinimumLevel.Information()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithExceptionDetails()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}]{NewLine} {Message:lj}{NewLine}{Exception}",
            formatProvider: CultureInfo.CurrentCulture)
        .WriteTo.SQLite(Constants.LogsDbPath, formatProvider: CultureInfo.CurrentCulture, storeTimestampInUtc: true, batchSize: 1));

builder.WebHost.ConfigureKestrel(ConfigureOptions);

builder.Services.AddTransient<ITagsRepo, TagsRepo>();
builder.Services.AddTransient<ITaggedItemsRepo, TaggedItemsRepo>();
builder.Services.AddTransient(typeof(ITagger<File>), typeof(FileTagger));
builder.Services.AddTransient(typeof(ITagger<Folder>), typeof(FolderTagger));
builder.Services.AddGrpc();

var app = builder.Build();
app.Logger.LogInformation("Application created");

app.MapGrpcService<TagService>();
// app.MapGrpcService<TagSearchService>();

InitializeDatabase();

app.Logger.LogInformation("Launching application...");
await app.RunAsync();

void ConfigureOptions(KestrelServerOptions kestrelServerOptions)
{
    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "GrpcDevelopment")
    {
        if (System.IO.File.Exists(Constants.SocketPath))
        {
            System.IO.File.Delete(Constants.SocketPath);
        }

        kestrelServerOptions.ListenUnixSocket(Constants.SocketPath, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
    }
    else
    {
        kestrelServerOptions.ListenLocalhost(5280, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
    }
}

void InitializeDatabase()
{
    using var db = new LiteDatabase(Constants.DbPath);

    db.UtcDate = true;
    var taggedItems = db.GetCollection<TaggedItemDto>("TaggedItems");
    var tags = db.GetCollection<TagDto>("Tags");

    taggedItems.EnsureIndex(x => x.Id, true);
    tags.EnsureIndex(tag => tag.Name, true);
}
