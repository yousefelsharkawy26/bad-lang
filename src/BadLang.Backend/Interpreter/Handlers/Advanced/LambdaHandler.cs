using BadLang.IR;
using BadLang.Core;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Advanced;

public class LambdaHandler : IIRNodeHandler
{
    public System.Collections.Generic.IEnumerable<System.Type> GetHandledTypes() => new[] { typeof(IRLambda) };
    public bool CanHandle(IRNode node) => node is IRLambda;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        var ldef = (IRLambda)node;
        context.Environment.Define(ldef.Target, new BadLangLambda(ldef, context.Environment));
        return HandlerResult.Continue;
    }
}
