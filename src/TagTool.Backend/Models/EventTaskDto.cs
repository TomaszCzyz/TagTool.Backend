namespace TagTool.Backend.Models;

public class EventTaskDto
{
    public required string TaskId { get; set; }

    public required string ActionId { get; set; }

    // todo: The property 'EventTaskDto.Events' is a collection or enumeration type with a value converter but with no value comparer. Set a value comparer to ensure the collection/enumeration elements are compared correctly.
    public required Dictionary<string, string> ActionAttributes { get; set; }

    // todo: The property 'EventTaskDto.ActionAttributes' is a collection or enumeration type with a value converter but with no value comparer. Set a value comparer to ensure the collection/enumeration elements are compared correctly.
    // convert it to list of Own Entities of record (string EventName)
    public required string[] Events { get; set; }
}
