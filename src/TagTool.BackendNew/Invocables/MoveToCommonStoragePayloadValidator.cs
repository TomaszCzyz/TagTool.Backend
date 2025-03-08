using FluentValidation;
using JetBrains.Annotations;
using TagTool.BackendNew.Validations;

namespace TagTool.BackendNew.Invocables;

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
