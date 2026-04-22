using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers;

public class ScopeHandlers : IIRNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => new[] { typeof(IREnterScope), typeof(IRExitScope) };
    public bool CanHandle(IRNode node) => 
        node is IREnterScope || 
        node is IRExitScope;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        switch (node)
        {
            case IREnterScope _:
                context.Environment = new Environment(context.Environment);
                break;
            case IRExitScope _:
                context.Environment = context.Environment.Enclosing!;
                break;
        }
        return HandlerResult.Continue;
    }
}
