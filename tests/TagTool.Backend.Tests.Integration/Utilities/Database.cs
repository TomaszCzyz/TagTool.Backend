using Microsoft.EntityFrameworkCore;
using TagTool.Backend.DbContext;
using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Tests.Integration.Utilities;

public static class Database
{
    public static void InitializeDbForTests(ITagToolDbContext db)
    {
        db.Tags.AddRange(GetSeedingTextTags());
        db.SaveChanges();
        db.ChangeTracker.Clear();
    }

    public static void ReinitializeDbForTests(ITagToolDbContext db)
    {
        db.Tags.RemoveRange(db.Tags.OfType<TextTag>());
        InitializeDbForTests(db);
    }

    public static void ClearTagsAssociations(ITagToolDbContext db)
    {
        foreach (var group in db.TagSynonymsGroups.Include(g => g.Synonyms))
        {
            group.Synonyms.Clear();
        }

        db.TagSynonymsGroups.RemoveRange(db.TagSynonymsGroups);
        db.SaveChanges();
        db.ChangeTracker.Clear();
    }

    private static IEnumerable<TagBase> GetSeedingTextTags()
        => new List<TagBase>
        {
            new TextTag { Text = "TestTag1" },
            new TextTag { Text = "TestTag2" },
            new TextTag { Text = "TestTag3" },
            new TextTag { Text = "TestTag4" },
            new TextTag { Text = "TestTag5" },
            new TextTag { Text = "TestTag6" }
        };

    private static IEnumerable<TagBase> GetSeedingTagsAssociations()
    {
        return new List<TagBase>
        {
            new TextTag { Text = "TestTag1" },
            new TextTag { Text = "TestTag2" },
            new TextTag { Text = "TestTag3" },
            new TextTag { Text = "TestTag4" },
            new TextTag { Text = "TestTag5" },
            new TextTag { Text = "TestTag6" },
        };
    }
}
