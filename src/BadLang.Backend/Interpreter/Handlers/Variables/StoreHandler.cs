using BadLang.IR;

namespace BadLang.Backend.Interpreter.Handlers.Variables;

public class StoreHandler : IIrNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => [typeof(IrStore)];
    public bool CanHandle(IrNode node) => node is IrStore;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        var store = (IrStore)node;
        context.Environment.Assign(store.VariableName, context.Eval(store.Value));
        return HandlerResult.Continue;
    }
}
