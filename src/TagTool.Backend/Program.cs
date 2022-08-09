using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using TagTool.Backend.Commands;
using TagTool.Backend.Commands.TagOperations;
using TagTool.Backend.DbContext;
using TagTool.Backend.Services;

var socketPath = Path.Combine(Path.GetTempPath(), "socket.tmp");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddGrpc();
builder.WebHost.ConfigureKestrel(
    options =>
    {
        if (File.Exists(socketPath))
        {
            File.Delete(socketPath);
        }
        options.ListenUnixSocket(socketPath, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
    });

var app = builder.Build();
app.Logger.LogInformation("Application created...");

var commandInvoker = new CommandInvoker();

app.MapGrpcService<TagToolService>();

app.MapPost("/TagFolder", TagFolder);
app.MapPut("/CreateTag", CreateTag);
app.MapPost("/UntagFolder", UntagFolder);
app.MapDelete("/DeleteTag", DeleteTag);
app.MapPost("/Undo", Undo);
app.MapPost("/Redo", Redo);

app.Logger.LogInformation("Executing EF migrations...");
await using (var db = new TagContext())
{
    db.Database.Migrate();
}

app.Logger.LogInformation("Launching application...");
await app.RunAsync();

async Task TagFolder(string path, string tagName)
{
    var command = new TagFolderCommand(path, tagName);
    await commandInvoker.SetAndInvoke(command);
}

async Task CreateTag(string tagName)
{
    var command = new CreateTagCommand(tagName);
    await commandInvoker.SetAndInvoke(command);
}

async Task UntagFolder(string path, string tagName)
{
    var command = new UntagFolderCommand(path, tagName);
    await commandInvoker.SetAndInvoke(command);
}

async Task DeleteTag(string tagName)
{
    var command = new DeleteTagCommand(tagName);
    await commandInvoker.SetAndInvoke(command);
}

async Task Undo()
{
    await commandInvoker.Undo();
}

async Task Redo()
{
    await commandInvoker.Redo();
}
