using TagTool.Backend.Models;
using TagTool.Backend.Models.Taggable;
using TagTool.Backend.Taggers;

namespace TagTool.Backend.Services;

public interface ITaggerProvider
{
    ITagger<T> Get<T>() where T : ITaggable;
}

public interface ITaggersManager
{
    TaggedItem? Tag<T>(T item, string tagName) where T : ITaggable;
}

public class TaggersManager : ITaggersManager
{
    private readonly IServiceProvider _serviceProvider;

    public TaggersManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public TaggedItem? Tag<T>(T item, string tagName) where T : ITaggable
    {
        var itemType = item.GetType();
        var taggerType = typeof(ITagger<>).MakeGenericType(itemType);
        var tagger = (dynamic)_serviceProvider.GetRequiredService(taggerType);
        // var tagger = _serviceProvider.GetRequiredService<ITagger<T>>();

        return tagger.Tag(item, new[] { tagName });
    }
}
