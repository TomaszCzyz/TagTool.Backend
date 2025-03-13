using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TagTool.Backend.DbContext;

[UsedImplicitly]
public class TagToolDbContextFactory : IDesignTimeDbContextFactory<TagToolDbContext>
{
    public TagToolDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TagToolDbContext>();
        optionsBuilder.UseSqlite("Data Source=blog.db");

        return new TagToolDbContext(null!, optionsBuilder.Options);
    }
}
