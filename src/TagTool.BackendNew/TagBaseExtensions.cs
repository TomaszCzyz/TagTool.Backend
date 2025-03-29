using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Services.Grpc.Dtos;

namespace TagTool.BackendNew;

public static class TagBaseExtensions
{
    public static Tag ToDto(this TagBase tag) => new() { Id = tag.Id, Text = tag.Text };

    public static IEnumerable<Tag> ToDtos(this IEnumerable<TagBase> tags) => tags.Select(t => t.ToDto());
}
