using System.Text;

namespace TagTool.Backend.Repositories.Dtos;

public class FolderDto : TaggedItemDto
{
    public override string UniqueKey => FullPath;

    public required string FullPath { get; init; }

    public override string ToString()
        => new StringBuilder()
            .Append(nameof(FolderDto))
            .Append(" { ")
            .Append("FullPath = ")
            .Append(FullPath)
            .Append('}').ToString();
}
