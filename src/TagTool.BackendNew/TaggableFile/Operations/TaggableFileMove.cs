using OneOf;
using OneOf.Types;
using TagTool.BackendNew.Contracts;

namespace TagTool.BackendNew.TaggableFile.Operations;

internal class TaggableFileMove : ITaggableItemOperation<TaggableFile, OneOf<Success, Error<string>>>
{
    public static string Name { get; } = "file:move";

    public Guid ItemId { get; set; }
}
