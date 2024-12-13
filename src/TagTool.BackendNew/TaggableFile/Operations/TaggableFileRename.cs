using MediatR;
using OneOf;
using OneOf.Types;

namespace TagTool.BackendNew.TaggableFile.Operations;

using Response = OneOf<Success, Error<string>>;

public class TaggableFileRename : ITaggableFileOperation<Response>
{
    public static string Name { get; } = "file:rename";

    public Guid ItemId { get; set; }

    public required string NewName { get; init; }
}

public class TaggableFileRenameOperationHandler : IRequestHandler<TaggableFileRename, Response>
{
    public Task<Response> Handle(TaggableFileRename request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
