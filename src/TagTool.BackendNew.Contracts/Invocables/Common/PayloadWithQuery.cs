using System.Text.Json.Serialization;
using TagTool.BackendNew.Contracts.Entities;

namespace TagTool.BackendNew.Contracts.Invocables.Common;

public abstract class PayloadWithQuery
{
    [JsonIgnore]
    public List<TagQueryPart> TagQuery { get; set; } = null!;
}
