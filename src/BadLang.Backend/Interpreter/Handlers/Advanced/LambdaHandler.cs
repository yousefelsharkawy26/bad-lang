using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Advanced;

public class LambdaHandler : IIrNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => [typeof(IrLambda)];
    public bool CanHandle(IrNode node) => node is IrLambda;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        var ldef = (IrLambda)node;
        context.Environment.Define(ldef.Target, new BadLangLambda(ldef, context.Environment));
        return HandlerResult.Continue;
    }
}
