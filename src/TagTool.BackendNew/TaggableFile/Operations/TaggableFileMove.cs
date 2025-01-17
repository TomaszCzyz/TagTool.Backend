using OneOf;
using OneOf.Types;

namespace TagTool.BackendNew.TaggableFile.Operations;

public sealed class TaggableFileMove : ITaggableFileOperation<OneOf<Success, Error<string>>>
{
    public static string Name { get; } = "file:move";

    public Guid ItemId { get; set; }
}
