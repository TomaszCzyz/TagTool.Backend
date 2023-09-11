using JetBrains.Annotations;
using OneOf;
using TagTool.Backend.Models;
using TagTool.Backend.Services;

namespace TagTool.Backend.Commands;

// todo: make this command reversible by adding 'thrash' functionality
public class DeleteFileRequest : ICommand<OneOf<SuccessResponse, ErrorResponse>>
{
    public required string Path { get; init; }
}

[UsedImplicitly]
public class DeleteFile : ICommandHandler<DeleteFileRequest, OneOf<SuccessResponse, ErrorResponse>>
{
    private readonly ICommonStorage _commonStorage;

    public DeleteFile(ICommonStorage commonStorage)
    {
        _commonStorage = commonStorage;
    }

    public async Task<OneOf<SuccessResponse, ErrorResponse>> Handle(DeleteFileRequest request, CancellationToken cancellationToken)
    {
        var moveFileToTrash = await _commonStorage.MoveFileToTrash(request.Path, cancellationToken);

        return moveFileToTrash.Match<OneOf<SuccessResponse, ErrorResponse>>(
            successMessage => new SuccessResponse(successMessage, null),
            error => error);
    }
}
