using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Advanced;

public class ExportHandler : IIRNodeHandler
{
    public System.Collections.Generic.IEnumerable<System.Type> GetHandledTypes() => new[] { typeof(IRExport) };
    public bool CanHandle(IRNode node) => node is IRExport;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        var exportNode = (IRExport)node;
        context.Interpreter.HandleExport(exportNode);
        return HandlerResult.Continue;
    }
}
