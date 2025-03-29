using JetBrains.Annotations;
using OneOf;
using TagTool.BackendNew.Contracts.Invocables;

namespace TagTool.BackendNew.TaggableItems.TaggableFile.Operations;

[UsedImplicitly(
    ImplicitUseKindFlags.Assign,
    ImplicitUseTargetFlags.WithMembers | ImplicitUseTargetFlags.WithInheritors,
    Reason = "Created by IOperationManger when during request dispatch.")]
internal interface ITaggableFileOperation<out TResponse> : ITaggableItemOperation<TaggableFile, TResponse> where TResponse : IOneOf;
