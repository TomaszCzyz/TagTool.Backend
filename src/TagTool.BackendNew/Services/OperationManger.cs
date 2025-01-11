using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using JetBrains.Annotations;
using MediatR;
using OneOf;
using TagTool.BackendNew.Contracts;

namespace TagTool.BackendNew.Services;

public interface IOperationManger
{
    Task<IOneOf> InvokeOperation(string itemId, string operationName, string operationPayload);

    string[] GetOperationNames(Type taggableItemType);
}

[UsedImplicitly]
public sealed class OperationManger : IOperationManger
{
    private readonly IMediator _mediator;
    private readonly ConcurrentDictionary<string, Type> _operations = new();
    private readonly Dictionary<Type, List<string>> _taggableItemOperationNames = new();

    public OperationManger(IMediator mediator, params Type[] markers)
    {
        _mediator = mediator;

        var types = markers
            .SelectMany(marker => marker.Assembly.ExportedTypes
                .Where(x => typeof(ITaggableItemOperationBase).IsAssignableFrom(x) && x is { IsInterface: false, IsAbstract: false }))
            .ToArray();


        foreach (var type in types)
        {
            var operationType = type.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITaggableItemOperation<,>));
            var taggableItemType = operationType.GenericTypeArguments[0];

            var nameProperty = type.GetProperty(nameof(ITaggableItemOperationBase.Name),
                BindingFlags.Static | BindingFlags.Public);

            if (nameProperty?.GetValue(null) is string name)
            {
                _operations[name] = type;
                _taggableItemOperationNames[taggableItemType].Add(name);
            }
            else
            {
                throw new InvalidOperationException($"Type {type.Name} does not have a valid static Name property.");
            }
        }
    }

    public async Task<IOneOf> InvokeOperation(string itemId, string operationName, string operationPayload)
    {
        var payloadType = GetPayloadType(operationName);

        var obj = JsonSerializer.Deserialize(operationPayload, payloadType);

        if (obj is not ITaggableItemOperationBase payload)
        {
            throw new InvalidOperationException($"Operation '{operationName}' is not supported");
        }

        payload.ItemId = Guid.Parse(itemId);
        return (await _mediator.Send(payload) as IOneOf)!;
    }

    public string[] GetOperationNames(Type taggableItemType)
    {
        return _taggableItemOperationNames[taggableItemType].ToArray();
    }

    private Type GetPayloadType(string operationName)
    {
        if (_operations.TryGetValue(operationName, out var type))
        {
            return type;
        }

        throw new InvalidOperationException($"Operation '{operationName}' was not regirtered");
    }
}
