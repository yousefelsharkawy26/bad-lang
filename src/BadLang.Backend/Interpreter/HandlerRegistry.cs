using BadLang.IR;

namespace BadLang.Backend.Interpreter;

public class HandlerRegistry
{
    private readonly Dictionary<Type, IIrNodeHandler> _handlers = new();

    public HandlerRegistry(IEnumerable<IIrNodeHandler>? handlers = null)
    {
        if  (handlers == null)
            return;
        
        foreach (var handler in handlers)
        {
            Register(handler);
        }
    }

    private void Register(IIrNodeHandler handler)
    {
        foreach (var type in handler.GetHandledTypes())
        {
            _handlers[type] = handler;
        }
    }

    public void Register<T>(IIrNodeHandler handler) where T : IrNode
    {
        _handlers[typeof(T)] = handler;
    }

    public void Register(Type type, IIrNodeHandler handler)
    {
        if (!typeof(IrNode).IsAssignableFrom(type))
            throw new ArgumentException("Type must be an IRNode", nameof(type));
        _handlers[type] = handler;
    }

    public IIrNodeHandler? Get(IrNode node)
    {
        var type = node.GetType();
        return _handlers.GetValueOrDefault(type);
    }
}
