using OneOf;
using OneOf.Types;

namespace TagTool.BackendNew.TaggableFile.Operations;

internal class TaggableFileMove : ITaggableFileOperation<OneOf<Success, Error<string>>>
{
    public static string Name { get; } = "file:move";

    public Guid ItemId { get; set; }
}
