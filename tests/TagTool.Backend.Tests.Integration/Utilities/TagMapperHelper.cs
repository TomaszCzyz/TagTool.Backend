using TagTool.Backend.Mappers;
using TagTool.Backend.Models.Tags;

namespace TagTool.Backend.Tests.Integration.Utilities;

public static class TagMapperHelper
{
    public static ITagMapper InitializeWithKnownMappers()
    {
        var mappers =
            new object[]
            {
                new ItemTypeTagMapper(),
                new TextTagMapper(),
                new DayTagMapper(),
                new MonthTagMapper(),
                new DayRangeTagMapper(),
                new MonthTagMapper(),
                new MonthRangeTagMapper()
            };

        var fromDto = mappers.Cast<ITagFromDtoMapper>().ToArray();
        var toDto = mappers.Cast<ITagToDtoMapper>().ToArray();

        return new TagMapper(fromDto, toDto);
    }
}
