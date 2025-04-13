using FluentValidation;

namespace TagTool.BackendNew.TaggableItems.TaggableFile.Utils;

public static class ValidationHelper
{
    public static Action<string, ValidationContext<T>> ValidatePath<T>()
    {
        return (s, context) =>
        {
            try
            {
                _ = Path.GetFullPath(s);
            }
            catch (Exception e)
            {
                // TODO: message mapping
                context.AddFailure("CommonStoragePath", $"Path is not valid, {e.Message}");
            }
        };
    }
}
