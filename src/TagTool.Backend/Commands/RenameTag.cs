using OneOf;
using TagTool.Backend.Models;

namespace TagTool.Backend.Commands;

public class RenameTag : ICommand<OneOf<string, ErrorResponse>>, IReversible
{
    public required string TagName { get; init; }
    public required string NewTagName { get; init; }

    public IReversible GetReverse() => new RenameTag { NewTagName = TagName, TagName = NewTagName };
}
