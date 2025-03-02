using System.Text.Json.Serialization;
using TagTool.BackendNew.Models;

namespace TagTool.BackendNew.Invocables.Common;

public abstract class PayloadWithQuery
{
    [JsonIgnore]
    public TagQuery TagQuery { get; set; } = null!;
}
