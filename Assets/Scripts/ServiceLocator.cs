using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight Service Locator.
/// Alternative to static singletons — allows swapping implementations (e.g. for testing).
/// Usage:
///   ServiceLocator.Register<IAudioService>(myAudioManager);
///   var audio = ServiceLocator.Get<IAudioService>();
/// </summary>
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

    public static void Register<T>(T service)
    {
        _services[typeof(T)] = service;
    }

    public static T Get<T>()
    {
        if (_services.TryGetValue(typeof(T), out var service))
            return (T)service;

        Debug.LogError($"[ServiceLocator] Service of type {typeof(T).Name} not found!");
        return default;
    }

    public static bool Has<T>() => _services.ContainsKey(typeof(T));

    public static void Unregister<T>()
    {
        _services.Remove(typeof(T));
    }

    public static void Clear()
    {
        _services.Clear();
    }
}
