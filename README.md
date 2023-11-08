# TagTool.Backend

This repo contains a logic that powers [TagTool.App](https://github.com/TomaszCzyz/TagTool.App).

> The app is still in a very early development stage.

---

## Design

### Main principles

The application design focuses on providing very general abstraction and logic that allows extensibility.

### Architecture

Inspired by [article](https://www.codemag.com/article/1811091/Building-a-.NET-IDE-with-JetBrains-Rider) about design of Rider IDE I decided
to separate backend logic into separate process. In the future I consider creating hosted version of the service and this approach will simplify the
later migration. As protocol between the app and the backend I chose gRPC, after finding great example
[Inter-process communication with gRPC](https://learn.microsoft.com/en-us/aspnet/core/grpc/interprocess?view=aspnetcore-7.0).
The data is stored in Sqlite using EF Core as ORM.

### Basic concepts

There are main elements:

- `Tag` (e.g. Photo, Document, 2023),
- `TaggableItem` (e.g. file, folder),
- `Task` (e.g. archive unused files, create backup).

`Tag`s are pretty straightforward. They are just markers that can be assigned to `TaggableItem`s. Most of the tags are just words,
but some have some special powers (about them later). Let's move to `TaggableItem`. Why not just _file_ and _folder_?...
Extensibility! The idea is: anything can be tagged as long as we define necessary mechanisms (like displaying it in a list,
associated action, e.t.c.). For example, we could tag a code snippet, whose invocation would result in pasting the phrase.

With this simple abstraction we can introduce extremely powerful tool. The `Task` describes an action that can be
performed on a `TaggableItem`s, which meet a certain _TagQuery_ or items that are associated with an event like _ItemTagged_.
Now we are only limited by our imagination. These are some simple ideas: backup files tagged with tag 'Backup',
index files tagged with 'Text'/'Document', move files and folder tagged with 'CommonStorage' to specified location to keep
theirs location managed internally, generate description for files tagged with 'Photo'/'Graphic'/.. using AI model,
sync certain files with cloud disk, encrypt files with tag 'Encrypt', e.t.c.

### Implementation details

to be continued...
