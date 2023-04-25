using System.Diagnostics;
using Humanizer;
using Microsoft.Extensions.Options;
using TagTool.Backend.Models.Options;

namespace TagTool.Backend.Services;

public interface ITagNameProvider
{
    string GetName(string name);

    string GetName(string name, Models.Options.NamingConvention namingConvention);
}

public class TagNameProvider : ITagNameProvider
{
    private readonly TagsOptions _tagsOptions;

    public TagNameProvider(IOptions<TagsOptions> options)
    {
        _tagsOptions = options.Value;
    }

    public string GetName(string name) => GetName(name, _tagsOptions.NamingConvention);

    public string GetName(string name, Models.Options.NamingConvention namingConvention)
        => namingConvention switch
        {
            Models.Options.NamingConvention.Unchanged => name,
            Models.Options.NamingConvention.CamelCase => name.Camelize(),
            Models.Options.NamingConvention.PascalCase => name.Pascalize(),
            Models.Options.NamingConvention.SnakeCase => name.Underscore(),
            Models.Options.NamingConvention.KebabCase => name.Kebaberize(),
            _ => throw new UnreachableException()
        };
}
