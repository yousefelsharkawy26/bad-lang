using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers;

public class ScopeHandlers : IIrNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => new[] { typeof(IrEnterScope), typeof(IrExitScope) };
    public bool CanHandle(IrNode node) => 
        node is IrEnterScope || 
        node is IrExitScope;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        switch (node)
        {
            case IrEnterScope _:
                context.Environment = new Environment(context.Environment);
                break;
            case IrExitScope _:
                context.Environment = context.Environment.Enclosing!;
                break;
        }
        return HandlerResult.Continue;
    }
}
