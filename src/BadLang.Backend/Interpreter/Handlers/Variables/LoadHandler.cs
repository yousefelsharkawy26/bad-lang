using BadLang.IR;

namespace BadLang.Backend.Interpreter.Handlers.Variables;

public class LoadHandler : IIrNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => [typeof(IrLoad)];
    public bool CanHandle(IrNode node) => node is IrLoad;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        var load = (IrLoad)node;
        context.Environment.Define(load.Target, context.Environment.Get(load.VariableName));
        return HandlerResult.Continue;
    }
}
