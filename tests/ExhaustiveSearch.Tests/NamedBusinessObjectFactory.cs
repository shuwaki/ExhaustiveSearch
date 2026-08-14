using System.Reflection;
using DigitalWorxpaces.Worxpace.Base;

namespace ExhaustiveSearch.Tests;

internal static class NamedBusinessObjectFactory
{
    public static INamedBusinessObject Create(string name)
    {
        INamedBusinessObject instance =
            DispatchProxy.Create<INamedBusinessObject, NamedBusinessObjectProxy>();
        ((NamedBusinessObjectProxy)(object)instance).Name = name;
        return instance;
    }

    public static ISaleable CreateSaleable(string name, int usageIndex)
    {
        ISaleable instance = DispatchProxy.Create<ISaleable, NamedBusinessObjectProxy>();
        var proxy = (NamedBusinessObjectProxy)(object)instance;
        proxy.Name = name;
        proxy.UsageIndex = usageIndex;
        return instance;
    }

}

public class NamedBusinessObjectProxy : DispatchProxy
{
    public string Name { get; set; } = string.Empty;

    public int UsageIndex { get; set; }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        return targetMethod?.Name switch
        {
            "get_Name" => Name,
            "set_Name" => SetName(args),
            "get_UsageIndex" => UsageIndex,
            "set_UsageIndex" => SetUsageIndex(args),
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

    private object? SetUsageIndex(object?[]? args)
    {
        UsageIndex = (int?)args?[0] ?? 0;
        return null;
    }

    private static object? GetDefaultValue(Type? type) =>
        type is { IsValueType: true } ? Activator.CreateInstance(type) : null;
}
