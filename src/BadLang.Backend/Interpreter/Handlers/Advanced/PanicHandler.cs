using System;
using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Advanced;

public class PanicHandler : IIRNodeHandler
{
    public System.Collections.Generic.IEnumerable<System.Type> GetHandledTypes() => new[] { typeof(IRPanic) };
    public bool CanHandle(IRNode node) => node is IRPanic;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        var panic = (IRPanic)node;
        throw new Exception("panic: " + context.Eval(panic.Message));
    }
}
