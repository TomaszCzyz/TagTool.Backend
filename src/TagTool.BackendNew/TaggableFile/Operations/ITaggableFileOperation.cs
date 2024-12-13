using OneOf;
using TagTool.BackendNew.Interfaces;

namespace TagTool.BackendNew.TaggableFile.Operations;

internal interface ITaggableFileOperation<TResponse> : ITaggableItemOperation<TaggableFile, TResponse> where TResponse : IOneOf;
