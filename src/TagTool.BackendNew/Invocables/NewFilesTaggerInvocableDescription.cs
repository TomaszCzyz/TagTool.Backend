using JetBrains.Annotations;
using TagTool.BackendNew.Contracts;

namespace TagTool.BackendNew.Invocables;

[UsedImplicitly]
public class NewFilesTaggerInvocableDescription : IInvocableDescription<NewFilesTagger>
{
    public string Id { get; } = "772ECE0B";
    public string GroupId { get; } = "NewFilesTagger:9721";
    public string DisplayName { get; } = "Tag new files";
    public string Description { get; } = "Adds specified tags to files in a given folder";
}
