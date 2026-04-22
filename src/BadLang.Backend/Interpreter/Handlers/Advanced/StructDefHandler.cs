using BadLang.IR;
using BadLang.Core;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Advanced;

public class StructDefHandler : IIRNodeHandler
{
    public System.Collections.Generic.IEnumerable<System.Type> GetHandledTypes() => new[] { typeof(IRStructDef) };
    public bool CanHandle(IRNode node) => node is IRStructDef;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        var sdef = (IRStructDef)node;
        context.Environment.Define(sdef.Name, new BadLangStruct(sdef.Name, sdef.Fields));
        return HandlerResult.Continue;
    }
}
