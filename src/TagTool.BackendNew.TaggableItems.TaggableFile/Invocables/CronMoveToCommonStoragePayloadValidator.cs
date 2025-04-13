using FluentValidation;
using JetBrains.Annotations;
using TagTool.BackendNew.TaggableItems.TaggableFile.Utils;

namespace TagTool.BackendNew.TaggableItems.TaggableFile.Invocables;

[UsedImplicitly]
public class CronMoveToCommonStoragePayloadValidator : AbstractValidator<CronMoveToCommonStoragePayload>
{
    public CronMoveToCommonStoragePayloadValidator()
    {
        RuleFor(x => x.CommonStoragePathString)
            .NotEmpty()
            .Custom(ValidationHelper.ValidatePath<CronMoveToCommonStoragePayload>());
    }
}
