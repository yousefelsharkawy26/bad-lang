using BadLang.IR;

namespace BadLang.Backend.Interpreter.Handlers.Advanced;

public class ImportHandler : IIrNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => [typeof(IrImport)];
    public bool CanHandle(IrNode node) => node is IrImport;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        var importNode = (IrImport)node;
        context.Interpreter.HandleImport(importNode, context.Environment);
        return HandlerResult.Continue;
    }
}
