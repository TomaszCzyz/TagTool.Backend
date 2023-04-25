namespace TagTool.Backend.Models.Options;

public enum NamingConvention
{
    Unchanged = 0,
    CamelCase = 1,
    PascalCase = 2,
    KebabCase = 3,
    SnakeCase = 4,
}

public class TagsOptions
{
    public NamingConvention NamingConvention { get; set; } = NamingConvention.CamelCase;
}
