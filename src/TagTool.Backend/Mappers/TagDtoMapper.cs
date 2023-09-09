using Google.Protobuf;
using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Mappers;

public interface ITagFromDtoMapper
{
    TagBase FromDto(IMessage dto);
}

public interface ITagToDtoMapper
{
    IMessage ToDto(TagBase tag);
}

/// <summary>
///     The base class for tag types that need mapping 'from' and 'to' dto.
///     In the case when tag needs to have only one mapping new class should be created.
/// </summary>
public abstract class TagDtoMapper<TTag, TDto> : ITagFromDtoMapper, ITagToDtoMapper
    where TTag : TagBase
    where TDto : IMessage
{
    public TagBase FromDto(IMessage dto) => MapFromDto((TDto)dto);

    public IMessage ToDto(TagBase tag) => MapToDto((TTag)tag);

    protected abstract TTag MapFromDto(TDto dto);

    protected abstract TDto MapToDto(TTag tag);
}
