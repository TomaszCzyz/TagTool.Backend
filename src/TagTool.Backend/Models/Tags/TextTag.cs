using JetBrains.Annotations;
using TagTool.Backend.DomainTypes;
using TagTool.Backend.Mappers;

namespace TagTool.Backend.Models.Tags;

public sealed class TextTag : TagBase
{
    private string _text = null!;

    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            FormattedName = nameof(TextTag) + ":" + _text;
        }
    }
}

[UsedImplicitly]
public class TextTagMapper : TagDtoMapper<TextTag, NormalTag>
{
    protected override TextTag MapFromDto(NormalTag dto) => new() { Text = dto.Name };

    protected override NormalTag MapToDto(TextTag tag) => new() { Name = tag.Text };
}
