using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class PerkFactory
{
    private static readonly Dictionary<string, Type> _registry = new();

    static PerkFactory()
    {
        var perkTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IPerk).IsAssignableFrom(t)
                     && !t.IsInterface
                     && !t.IsAbstract);

        foreach (var type in perkTypes)
        {
            string id = type.Name.Replace("Perk", "");
            _registry[id] = type;
        }
    }

    public static IPerk Create(string id)
    {
        if (_registry.TryGetValue(id, out var type))
            return (IPerk)Activator.CreateInstance(type);

        Debug.LogError($"Unknown perk ID: {id}");
        return null;
    }
}
