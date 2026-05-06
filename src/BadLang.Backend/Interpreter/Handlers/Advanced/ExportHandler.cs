using BadLang.IR;

namespace BadLang.Backend.Interpreter.Handlers.Advanced;

public class ExportHandler : IIrNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => [typeof(IrExport)];
    public bool CanHandle(IrNode node) => node is IrExport;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        var exportNode = (IrExport)node;
        context.Interpreter.HandleExport(exportNode);
        return HandlerResult.Continue;
    }
}
