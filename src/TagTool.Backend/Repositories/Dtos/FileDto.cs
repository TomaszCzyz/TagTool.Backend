using System.Text;

namespace TagTool.Backend.Repositories.Dtos;

public class FileDto : TaggedItemDto
{
    public override string UniqueKey => FullPath;

    public required string FullPath { get; init; }

    public override string ToString()
        => new StringBuilder()
            .Append(nameof(FileDto))
            .Append(" { ")
            .Append("FullPath = ")
            .Append(FullPath)
            .Append('}').ToString();
}
