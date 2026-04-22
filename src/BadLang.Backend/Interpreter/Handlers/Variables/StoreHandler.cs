using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Variables;

public class StoreHandler : IIRNodeHandler
{
    public System.Collections.Generic.IEnumerable<System.Type> GetHandledTypes() => new[] { typeof(IRStore) };
    public bool CanHandle(IRNode node) => node is IRStore;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        var store = (IRStore)node;
        context.Environment.Assign(store.VariableName, context.Eval(store.Value));
        return HandlerResult.Continue;
    }
}
