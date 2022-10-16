using System.Text;

namespace TagTool.Backend.Repositories.Dtos;

public class FileDto : TaggedItemDto
{
    public override string UniqueKey => FullPath;

    public required string FullPath { get; init; }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append(nameof(FileDto));
        stringBuilder.Append(" { ");
        stringBuilder.Append("FullPath = ");
        stringBuilder.Append(FullPath);
        stringBuilder.Append('}');
        return stringBuilder.ToString();
    }
}
