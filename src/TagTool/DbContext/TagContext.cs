using Microsoft.EntityFrameworkCore;
using TagTool.Models;
using File = TagTool.Models.File;

namespace TagTool.DbContext;

public class TagContext : Microsoft.EntityFrameworkCore.DbContext
{
    public DbSet<Tag> Tags { get; set; } = null!;

    public DbSet<Group> Groups { get; set; } = null!;

    public DbSet<File> Files { get; set; } = null!;

    private string DbPath { get; }

    public TagContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var appDataLocalPath = Environment.GetFolderPath(folder);

        var path = Path.Join(appDataLocalPath, Constants.Constants.ApplicationName);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        DbPath = Path.Join(path, "TagTool.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSqlite($"Data Source={DbPath}")
            .LogTo(Console.WriteLine, LogLevel.Information);
    }
}
