using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Advanced;

public class StructDefHandler : IIrNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => [typeof(IrStructDef)];
    public bool CanHandle(IrNode node) => node is IrStructDef;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        var sdef = (IrStructDef)node;
        context.Environment.Define(sdef.Name, new BadLangStruct(sdef.Name, sdef.Fields));
        return HandlerResult.Continue;
    }
}
