using System.Text.Json.Serialization;
using TagTool.BackendNew.Models;

namespace TagTool.BackendNew.Invocables.Common;

public abstract class PayloadWithQuery
{
    [JsonIgnore]
    public List<TagQueryPart> TagQuery { get; set; } = null!;
}
