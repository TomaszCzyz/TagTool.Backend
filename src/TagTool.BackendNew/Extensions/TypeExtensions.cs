namespace TagTool.BackendNew.Extensions;

public static class TypeExtensions
{
    public static bool ImplementsOpenGenericInterface(this Type type, Type openGenericInterface)
        => type
            .GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface);
}
