using System.Collections.ObjectModel;

namespace TagTool.Backend.Repositories;

public abstract class TaggableItemDto
{
    public Guid Id { get; set; }

    public Collection<string>? TagNames { get; set; }
}

public class FileDto : TaggableItemDto
{
    public required string FullPath { get; init; }
}

public class FolderDto : TaggableItemDto
{
    public required string FullPath { get; init; }
}
