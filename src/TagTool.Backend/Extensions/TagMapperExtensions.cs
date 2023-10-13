using Google.Protobuf.WellKnownTypes;
using TagTool.Backend.Mappers;
using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Extensions;

public static class TagMapperExtensions
{
    public static IEnumerable<Any> MapToDtos(this ITagMapper tagMapper, IEnumerable<TagBase> tags) => tags.Select(tagMapper.MapToDto);
}
