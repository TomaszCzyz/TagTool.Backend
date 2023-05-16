using Google.Protobuf.WellKnownTypes;

namespace TagTool.Backend.Models.Mappers;

public class TagMapper
{
    public static TagBase Map(Any tag)
    {
        TagBase? tagBase = null;

        if (tag.Is(DomainTypes.NormalTag.Descriptor))
        {
            var normalTag = tag.Unpack<DomainTypes.NormalTag>();
            tagBase = new NormalTag { Name = normalTag.Name };
        }
        else if (tag.Is(DomainTypes.YearTag.Descriptor))
        {
            var yearTag = tag.Unpack<DomainTypes.YearTag>();
            tagBase = new YearTag { DateOnly = new DateOnly(yearTag.Year, 1, 1) };
        }

        return tagBase ?? throw new ArgumentException("Unable to match tag type");
    }
}
