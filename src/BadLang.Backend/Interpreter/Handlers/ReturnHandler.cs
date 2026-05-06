using System;
using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers;

public class ReturnHandler : IIrNodeHandler
{
    public System.Collections.Generic.IEnumerable<System.Type> GetHandledTypes() => new[] { typeof(IrReturn) };
    public bool CanHandle(IrNode node) => node is IrReturn;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        var ret = (IrReturn)node;
        var value = ret.Value != null ? context.Eval(ret.Value) : null;
        throw new ReturnSignal(value);
    }
}
