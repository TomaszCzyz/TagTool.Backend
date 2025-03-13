using System.Diagnostics;
using Google.Protobuf.Collections;
using TagTool.BackendNew.Entities;
using TagTool.BackendNew.Models;
using TagTool.BackendNew.Services.Grpc.Dtos;
using TagQueryParam = TagTool.BackendNew.Services.Grpc.Dtos.TagQueryParam;

namespace TagTool.BackendNew.Mappers;

public static class GrpcDtosExtensions
{
    public static TagBase MapFromDto(this Tag tag)
        => new()
        {
            Id = tag.Id, Text = tag.Text
        };

    public static IEnumerable<TagBase> MapFromDtos(this IEnumerable<Tag> tags) => tags.Select(t => t.MapFromDto());


    public static List<Models.TagQueryParam> MapFromDto(this RepeatedField<TagQueryParam> tagQueryParams)
        => tagQueryParams.Select(x => x.MapFromDto()).ToList();

    private static Models.TagQueryParam MapFromDto(this TagQueryParam tagQueryParam)
    {
        return new Models.TagQueryParam
        {
            TagId = tagQueryParam.TagId, State = MapFromDto(tagQueryParam.State),
        };
    }

    public static QueryPartState MapFromDto(this TagQueryParam.Types.QuerySegmentState state)
        => state switch
        {
            TagQueryParam.Types.QuerySegmentState.Exclude => QueryPartState.Exclude,
            TagQueryParam.Types.QuerySegmentState.Include => QueryPartState.Include,
            TagQueryParam.Types.QuerySegmentState.MustBePresent => QueryPartState.MustBePresent,
            _ => throw new UnreachableException()
        };

    public static TagQueryParam.Types.QuerySegmentState MapToDto(this QueryPartState queryPart)
        => queryPart switch
        {
            QueryPartState.Exclude => TagQueryParam.Types.QuerySegmentState.Exclude,
            QueryPartState.Include => TagQueryParam.Types.QuerySegmentState.Include,
            QueryPartState.MustBePresent => TagQueryParam.Types.QuerySegmentState.MustBePresent,
            _ => throw new UnreachableException()
        };
}
