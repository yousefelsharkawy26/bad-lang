using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Advanced;

public class ImportHandler : IIRNodeHandler
{
    public System.Collections.Generic.IEnumerable<System.Type> GetHandledTypes() => new[] { typeof(IRImport) };
    public bool CanHandle(IRNode node) => node is IRImport;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        var importNode = (IRImport)node;
        context.Interpreter.HandleImport(importNode, context.Environment);
        return HandlerResult.Continue;
    }
}
