using System.Reflection;
using DigitalWorxpaces.Worxpace.Base;

namespace DigitalWorxpaces.Utilities.Search.Tests;

internal static class NamedBusinessObjectFactory
{
    public static INamedBusinessObject Create(string name)
    {
        INamedBusinessObject instance =
            DispatchProxy.Create<INamedBusinessObject, NamedBusinessObjectProxy>();
        ((NamedBusinessObjectProxy)(object)instance).Name = name;
        return instance;
    }

}

public class NamedBusinessObjectProxy : DispatchProxy
{
    public string Name { get; set; } = string.Empty;

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        return targetMethod?.Name switch
        {
            "get_Name" => Name,
            "set_Name" => SetName(args),
            "ToString" => Name,
            "GetHashCode" => Name.GetHashCode(StringComparison.Ordinal),
            "Equals" => ReferenceEquals(this, args?[0]),
            _ => GetDefaultValue(targetMethod?.ReturnType),
        };
    }

    private object? SetName(object?[]? args)
    {
        Name = (string?)args?[0] ?? string.Empty;
        return null;
    }

    private static object? GetDefaultValue(Type? type) =>
        type is { IsValueType: true } ? Activator.CreateInstance(type) : null;
}
