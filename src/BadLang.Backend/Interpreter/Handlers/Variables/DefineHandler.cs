using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Variables;

public class DefineHandler : IIRNodeHandler
{
    public System.Collections.Generic.IEnumerable<System.Type> GetHandledTypes() => new[] { typeof(IRDefine) };
    public bool CanHandle(IRNode node) => node is IRDefine;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        var def = (IRDefine)node;
        context.Environment.Define(def.VariableName, context.Eval(def.Value));
        return HandlerResult.Continue;
    }
}
