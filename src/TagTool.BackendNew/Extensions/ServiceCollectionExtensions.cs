using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Models;

namespace TagTool.BackendNew.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInvocableDefinitions(this IServiceCollection services)
    {
        var invocableDescriptions = typeof(Program).Assembly.ExportedTypes
            .Where(x => typeof(IInvocableDescriptionBase).IsAssignableFrom(x) && x is { IsInterface: false, IsAbstract: false })
            .Select(type => (
                Type: type
                    .GetInterfaces()
                    .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IInvocableDescription<>))
                    .GetGenericArguments()
                    .First(),
                Instance: (IInvocableDescriptionBase)Activator.CreateInstance(type)!))
            .ToDictionary(tuple => tuple.Type, tuple => tuple.Instance);

        var invocables = typeof(Program).Assembly.ExportedTypes
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

            var triggerType = type.ImplementsOpenGenericInterface(typeof(IEventTriggeredInvocable<>)) ? TriggerType.Event : TriggerType.Cron;

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

    private static bool IsInvocable(Type t)
        => t.ImplementsOpenGenericInterface(typeof(IEventTriggeredInvocable<>))
           || t.ImplementsOpenGenericInterface(typeof(ICronTriggeredInvocable<>));

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
