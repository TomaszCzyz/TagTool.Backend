﻿using Microsoft.EntityFrameworkCore;
using Serilog.Extensions.Logging;
using TagTool.Backend.Models;

namespace TagTool.Backend.DbContext;

public class TagContext : Microsoft.EntityFrameworkCore.DbContext
{
    public DbSet<Tag> Tags { get; set; } = null!;

    public DbSet<TaggedItem> TaggedItems { get; set; } = null!;

    public TagContext()
    {
        var path = Constants.Constants.BasePath;
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSqlite($"Data Source={Constants.Constants.DbPath}")
            .UseLoggerFactory(new SerilogLoggerFactory())
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tag>()
            .HasIndex(tag => tag.Name)
            .IsUnique();
    }
}
