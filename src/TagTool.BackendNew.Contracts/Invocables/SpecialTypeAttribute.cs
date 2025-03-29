namespace TagTool.BackendNew.Contracts.Invocables;

[AttributeUsage(AttributeTargets.Property)]
public class SpecialTypeAttribute : Attribute
{
    public Kind Type { get; set; }

    public enum Kind
    {
        DirectoryPath = 0,
        SingleTag = 1,
    }
}
