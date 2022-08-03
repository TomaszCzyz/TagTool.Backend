using TagTool.Commands;using TagTool.Commands.TagOperations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();
app.Logger.LogInformation("Application created...");

var commandInvoker = new CommandInvoker();

app.MapPost("/TagFolder", TagFolder);
app.MapPut("/CreateTag", CreateTag);
app.MapPost("/UntagFolder", UntagFolder);
app.MapDelete("/DeleteTag", DeleteTag);
app.MapPost("/Undo", Undo);
app.MapPost("/Redo", Redo);

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
