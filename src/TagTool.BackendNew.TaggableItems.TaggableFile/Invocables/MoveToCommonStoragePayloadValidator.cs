using FluentValidation;
using JetBrains.Annotations;
using TagTool.BackendNew.TaggableItems.TaggableFile.Utils;

namespace TagTool.BackendNew.TaggableItems.TaggableFile.Invocables;

[UsedImplicitly]
public class MoveToCommonStoragePayloadValidator : AbstractValidator<MoveToCommonStoragePayload>
{
    public MoveToCommonStoragePayloadValidator()
    {
        RuleFor(x => x.CommonStoragePath)
            .NotEmpty()
            .Custom(ValidationHelper.ValidatePath<MoveToCommonStoragePayload>());
    }
}
