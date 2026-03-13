using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple generic Event Bus / Message Bus.
/// Allows fully decoupled communication between systems.
/// 
/// Usage:
///   EventBus.Subscribe<BlockRemovedEvent>(OnBlockRemoved);
///   EventBus.Publish(new BlockRemovedEvent { block = this });
///   EventBus.Unsubscribe<BlockRemovedEvent>(OnBlockRemoved);
/// </summary>
public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> _handlers
        = new Dictionary<Type, List<Delegate>>();

    public static void Subscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        if (!_handlers.ContainsKey(type))
            _handlers[type] = new List<Delegate>();
        _handlers[type].Add(handler);
    }

    public static void Unsubscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        if (_handlers.ContainsKey(type))
            _handlers[type].Remove(handler);
    }

    public static void Publish<T>(T eventData)
    {
        var type = typeof(T);
        if (!_handlers.ContainsKey(type)) return;

        // Copy list to avoid modification during iteration
        var handlers = new List<Delegate>(_handlers[type]);
        foreach (var h in handlers)
        {
            try { ((Action<T>)h)(eventData); }
            catch (Exception e) { Debug.LogError($"[EventBus] Error in handler: {e}"); }
        }
    }

    public static void Clear()
    {
        _handlers.Clear();
    }
}

// ─── Event Structs ────────────────────────────────────────────────────────────

public struct BlockRemovedEvent
{
    public Block block;
    public int   remainingCount;
}

public struct LevelCompletedEvent
{
    public int levelIndex;
    public int starsEarned;
}

public struct GameStateChangedEvent
{
    public GameState newState;
}
