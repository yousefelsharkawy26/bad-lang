using BadLang.IR;
using BadLang.Core;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Definitions;

public class FunctionDefHandler : IIRNodeHandler
{
    public System.Collections.Generic.IEnumerable<System.Type> GetHandledTypes() => new[] { typeof(IRFunctionDef) };
    public bool CanHandle(IRNode node) => node is IRFunctionDef;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        var fdef = (IRFunctionDef)node;
        context.Environment.Define(fdef.Name, new BadLangFunction(fdef, context.Environment, false));
        return HandlerResult.Continue;
    }
}
