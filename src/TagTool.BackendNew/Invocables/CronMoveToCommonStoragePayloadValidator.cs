using FluentValidation;
using JetBrains.Annotations;
using TagTool.BackendNew.Validations;

namespace TagTool.BackendNew.Invocables;

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
