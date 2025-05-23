using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using TagTool.BackendNew.Contracts.Invocables;
using TagTool.BackendNew.Models;

namespace TagTool.BackendNew.Extensions;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddInvocables(this IServiceCollection services, Assembly[] assemblyMarkers)
    {
        var invocables = assemblyMarkers
            .SelectMany(x => x.ExportedTypes)
            .Where(t => IsInvocable(t) && t is { IsInterface: false, IsAbstract: false })
            .ToList();

        foreach (var type in invocables)
        {
            if (type.ImplementsOpenGenericInterface(typeof(IBackgroundInvocable<>)))
            {
                services.AddSingleton(type);
            }
            else
            {
                services.AddScoped(type);
            }
        }

        var queuingHandlers = assemblyMarkers
            .SelectMany(x => x.ExportedTypes)
            .Where(t => t.ImplementsOpenGenericInterface(typeof(IQueuingHandler<,>)) && t is { IsInterface: false, IsAbstract: false })
            .ToList();


        foreach (var queuingHandler in queuingHandlers)
        {
            // TODO: I think keyed services would be better here...
            var interfaceType = queuingHandler.GetInterfaces().First(i => i.Name == "IQueuingHandler`2");

            services.AddScoped(interfaceType, queuingHandler);
        }

        return services;
    }

    public static IServiceCollection AddInvocableDefinitions(this IServiceCollection services, Assembly[] assemblyMarkers)
    {
        var invocableDescriptions = assemblyMarkers
            .SelectMany(x => x.ExportedTypes)
            .Where(x => typeof(IInvocableDescriptionBase).IsAssignableFrom(x) && x is { IsInterface: false, IsAbstract: false })
            .Select(type => (
                Type: type
                    .GetInterfaces()
                    .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IInvocableDescription<>))
                    .GetGenericArguments()
                    .First(),
                Instance: (IInvocableDescriptionBase)Activator.CreateInstance(type)!))
            .ToDictionary(tuple => tuple.Type, tuple => tuple.Instance);

        var invocables = assemblyMarkers
            .SelectMany(x => x.ExportedTypes)
            .Where(t => IsInvocable(t) && t is { IsInterface: false, IsAbstract: false })
            .ToList();

        var options = new JsonSerializerOptions(JsonSerializerOptions.Default)
        {
            RespectNullableAnnotations = true,
        };
        JsonSchemaExporterOptions exporterOptions = new()
        {
            TreatNullObliviousAsNonNullable = true, TransformSchemaNode = TransformSchemaNode
        };

        List<InvocableDefinition> invocableDefinitions = [];
        foreach (var type in invocables)
        {
            var schema = options.GetJsonSchemaAsNode(type, exporterOptions);

            if (!invocableDescriptions.TryGetValue(type, out var description))
            {
                throw new InvalidOperationException($"No {typeof(IInvocableDescription<>).Name} implemented for Invocable {type.Name}.");
            }

            var triggerType = GetTriggerType(type);

            var invocableDescriptorDto = new InvocableDefinition(
                description.Id,
                description.GroupId,
                description.DisplayName,
                description.Description,
                schema.ToJsonString(),
                triggerType,
                type);

            invocableDefinitions.Add(invocableDescriptorDto);
        }

        services.AddSingleton(invocableDefinitions.ToArray());

        return services;
    }

    private static TriggerType GetTriggerType(Type type)
    {
        if (type.ImplementsOpenGenericInterface(typeof(IEventTriggeredInvocable<>)))
        {
            return TriggerType.Event;
        }

        if (type.ImplementsOpenGenericInterface(typeof(ICronTriggeredInvocable<>)))
        {
            return TriggerType.Cron;
        }

        if (type.ImplementsOpenGenericInterface(typeof(IBackgroundInvocable<>)))
        {
            return TriggerType.Background;
        }

        throw new NotSupportedException("Unknown Trigger type.");
    }

    private static bool IsInvocable(Type t)
        => t.ImplementsOpenGenericInterface(typeof(IEventTriggeredInvocable<>))
           || t.ImplementsOpenGenericInterface(typeof(ICronTriggeredInvocable<>))
           || t.ImplementsOpenGenericInterface(typeof(IBackgroundInvocable<>));

    private static JsonNode TransformSchemaNode(JsonSchemaExporterContext context, JsonNode node)
    {
        // We are at Path properties.Payload.properties.<PayloadProperties>
        // This is needed, because we want to describe only "root" properties.
        if (context.Path.Length != 4)
        {
            return node;
        }

        var attributeProvider = context.PropertyInfo is not null
            ? context.PropertyInfo.AttributeProvider
            : context.TypeInfo.Type;

        var specialTypeAttr = attributeProvider?
            .GetCustomAttributes(inherit: true)
            .Select(attr => attr as SpecialTypeAttribute)
            .FirstOrDefault(attr => attr is not null);


        if (specialTypeAttr == null)
        {
            return node;
        }

        if (node is not JsonObject jObj)
        {
            throw new NotImplementedException("Handle the case where the node is a boolean");
        }

        jObj.Remove("properties");

        switch (specialTypeAttr.Type)
        {
            case SpecialTypeAttribute.Kind.DirectoryPath:
                jObj.SetAt(jObj.IndexOf("type"), "directoryPath");
                break;
            case SpecialTypeAttribute.Kind.SingleTag:
                jObj.SetAt(jObj.IndexOf("type"), "tag");
                break;
            default:
                throw new NotSupportedException($"SpecialType {specialTypeAttr.Type} is not supported");
        }

        return node;
    }
}
