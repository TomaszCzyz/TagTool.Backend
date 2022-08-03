﻿namespace TagTool.Models;

public class File
{
    public int Id { get; set; }

    //public ifExternalFolder

    public string Name { get; set; } = null!;

    public long Length { get; set; }

    public DateTime? DateCreated { get; set; }

    public DateTime? DateModified { get; set; }

    public string? Location { get; set; }

    public ICollection<Tag> Tags { get; } = new List<Tag>();

    public static explicit operator File(FileInfo fileInfo) =>
        new()
        {
            Name = fileInfo.Name,
            Length = fileInfo.Length,
            DateCreated = fileInfo.CreationTime,
            DateModified = fileInfo.LastWriteTime,
            Location = fileInfo.DirectoryName
        };
}
