using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Variables;

public class AssignHandler : IIRNodeHandler
{
    public System.Collections.Generic.IEnumerable<System.Type> GetHandledTypes() => new[] { typeof(IRAssign) };
    public bool CanHandle(IRNode node) => node is IRAssign;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        var assign = (IRAssign)node;
        context.Environment.Define(assign.Target, context.Eval(assign.Value));
        return HandlerResult.Continue;
    }
}
