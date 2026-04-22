using System;
using System.Collections.Generic;
using BadLang.IR;

namespace BadLang.Backend.Interpreter;

public class HandlerRegistry
{
    private readonly Dictionary<Type, IIRNodeHandler> _handlers = new();

    public HandlerRegistry() { }

    public HandlerRegistry(IEnumerable<IIRNodeHandler> handlers)
    {
        foreach (var handler in handlers)
        {
            Register(handler);
        }
    }

    public void Register(IIRNodeHandler handler)
    {
        foreach (var type in handler.GetHandledTypes())
        {
            _handlers[type] = handler;
        }
    }

    public void Register<T>(IIRNodeHandler handler) where T : IRNode
    {
        _handlers[typeof(T)] = handler;
    }

    public void Register(Type type, IIRNodeHandler handler)
    {
        if (!typeof(IRNode).IsAssignableFrom(type))
            throw new ArgumentException("Type must be an IRNode", nameof(type));
        _handlers[type] = handler;
    }

    public IIRNodeHandler? Get(IRNode node)
    {
        var type = node.GetType();
        return _handlers.TryGetValue(type, out var handler) ? handler : null;
    }
}
