using BadLang.IR;

namespace BadLang.Backend.Interpreter.Handlers.Variables;

public class AssignHandler : IIrNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => [typeof(IrAssign)];
    public bool CanHandle(IrNode node) => node is IrAssign;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        var assign = (IrAssign)node;
        context.Environment.Define(assign.Target, context.Eval(assign.Value));
        return HandlerResult.Continue;
    }
}
