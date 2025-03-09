namespace TagTool.BackendNew.Contracts;

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
