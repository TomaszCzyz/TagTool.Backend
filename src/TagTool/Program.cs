using TagTool.Commands.TagOperations;
using TagTool.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();
app.Logger.LogInformation("Application created...");

app.MapPost("/TagFolder", TagFolder);
app.MapPut("/CreateTag", CreateTag);
app.MapPost("/UntagFolder", UntagFolder);
app.MapDelete("/DeleteTag", DeleteTag);

app.Logger.LogInformation("Launching application...");
await app.RunAsync();

async Task TagFolder(string path, string tagName)
{
    var command = new TagFolderCommand(path, tagName);
    await command.Execute();
}

async Task CreateTag(string tagName)
{
    var command = new CreateTagCommand(tagName);
    await command.Execute();
}

async Task UntagFolder(string path, string tagName)
{
    var command = new UntagFolderCommand(path, tagName);
    await command.Execute();
}

async Task DeleteTag(string tagName)
{
    var command = new DeleteTagCommand(tagName);
    await command.Execute();
}
