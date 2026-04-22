using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Variables;

public class LoadHandler : IIRNodeHandler
{
    public System.Collections.Generic.IEnumerable<System.Type> GetHandledTypes() => new[] { typeof(IRLoad) };
    public bool CanHandle(IRNode node) => node is IRLoad;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        var load = (IRLoad)node;
        context.Environment.Define(load.Target, context.Environment.Get(load.VariableName));
        return HandlerResult.Continue;
    }
}
