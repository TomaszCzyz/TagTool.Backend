using Google.Protobuf;
using JetBrains.Annotations;
using TagTool.Backend.Models.Mappers;

namespace TagTool.Backend.Models;

public sealed class NormalTag : TagBase
{
    private string _name = null!;

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            FormattedName = nameof(NormalTag) + ":" + _name;
        }
    }
}

[UsedImplicitly]
public class NormalTagDtoMapper : TagDtoMapper<NormalTag, DomainTypes.NormalTag>
{
    protected override NormalTag MapFromDto(DomainTypes.NormalTag dto) => new() { Name = dto.Name };

    protected override DomainTypes.NormalTag MapToDto(NormalTag tag) => new() { Name = tag.Name };
}
