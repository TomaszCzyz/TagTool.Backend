using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using MediatR;
using TagTool.Backend.Models.Tags;
using TagTool.Backend.Services;

namespace TagTool.Backend.Queries;

public class GetAllTagsAssociationsQuery : IStreamRequest<IAssociationManager.GroupDescription>
{
    public required TagBase TagBase { get; init; }
}

[UsedImplicitly]
public class GetAllTagsAssociations : IStreamRequestHandler<GetAllTagsAssociationsQuery, IAssociationManager.GroupDescription>
{
    private readonly IAssociationManager _associationManager;

    public GetAllTagsAssociations(IAssociationManager associationManager)
    {
        _associationManager = associationManager;
    }

    public async IAsyncEnumerable<IAssociationManager.GroupDescription> Handle(
        GetAllTagsAssociationsQuery request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (request.TagBase is null) throw new NotImplementedException();

        var allRelations = _associationManager.GetAllRelations(cancellationToken);

        await foreach (var groupDescription in allRelations)
        {
            yield return groupDescription;
        }
    }
}
