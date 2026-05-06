using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Definitions;

public class FunctionDefHandler : IIrNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => [typeof(IrFunctionDef)];
    public bool CanHandle(IrNode node) => node is IrFunctionDef;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        var fdef = (IrFunctionDef)node;
        context.Environment.Define(fdef.Name, new BadLangFunction(fdef, context.Environment, false));
        return HandlerResult.Continue;
    }
}
