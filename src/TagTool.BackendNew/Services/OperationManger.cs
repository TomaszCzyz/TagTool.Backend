using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using JetBrains.Annotations;
using MediatR;
using OneOf;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Contracts.Entities;
using TagTool.BackendNew.Contracts.Invocables;

namespace TagTool.BackendNew.Services;

public record struct OperationsInfo(string TypeName, string[] OperationNames);

public interface IOperationManger
{
    Task<IOneOf> InvokeOperation(string itemId, string operationName, string operationPayload);

    string[] GetOperationNames<T>() where T : TaggableItem;

    OperationsInfo[] GetOperationNames();
}

[UsedImplicitly]
public sealed class OperationManger : IOperationManger
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<string, Type> _operations = new();
    private readonly Dictionary<Type, List<string>> _taggableItemOperationNames = new();

    public OperationManger(IMediator mediator, IServiceProvider serviceProvider)
    {
        Type[] markers = [typeof(Program)];
        _serviceProvider = serviceProvider;

        var types = markers
            .SelectMany(marker => marker.Assembly.ExportedTypes
                .Where(x => typeof(ITaggableItemOperationBase).IsAssignableFrom(x) && x is { IsInterface: false, IsAbstract: false }))
            .ToArray();

        foreach (var type in types)
        {
            var operationType = type.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITaggableItemOperation<,>));
            var taggableItemType = operationType.GenericTypeArguments[0];

            var nameProperty = type.GetProperty(nameof(ITaggableItemOperationBase.Name), BindingFlags.Static | BindingFlags.Public);

            if (nameProperty?.GetValue(null) is string name)
            {
                _operations[name] = type;
                if (!_taggableItemOperationNames.TryAdd(taggableItemType, [name]))
                {
                    _taggableItemOperationNames[taggableItemType].Add(name);
                }
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

        var obj = JsonSerializer.Deserialize(operationPayload, payloadType, JsonSerializerOptions.Web);

        if (obj is not ITaggableItemOperationBase payload)
        {
            throw new InvalidOperationException($"Operation '{operationName}' is not supported");
        }

        payload.ItemId = Guid.Parse(itemId);

        using var scope = _serviceProvider.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return (await mediator.Send(payload) as IOneOf)!;
    }

    public string[] GetOperationNames<T>() where T : TaggableItem
    {
        return _taggableItemOperationNames[typeof(T)].ToArray();
    }

    public OperationsInfo[] GetOperationNames()
        => _taggableItemOperationNames
            .Select((pair =>
            {
                var (type, operationNames) = pair;
                if (!type.IsAssignableTo(typeof(ITaggableItemType)))
                {
                    throw new InvalidOperationException($"Type {type.Name} does not implement {nameof(ITaggableItemType)}");
                }

                var typeNameProperty = type.GetProperty(nameof(ITaggableItemType.TypeName), BindingFlags.Static | BindingFlags.Public);
                if (typeNameProperty?.GetValue(null) is string typeName)
                {
                    return new OperationsInfo(typeName, operationNames.ToArray());
                }

                throw new InvalidOperationException($"Type {type.Name} does not have a valid static {nameof(ITaggableItemType.TypeName)} property.");
            }))
            .ToArray();

    private Type GetPayloadType(string operationName)
    {
        if (_operations.TryGetValue(operationName, out var type))
        {
            return type;
        }

        throw new InvalidOperationException($"Operation '{operationName}' was not registered");
    }
}
