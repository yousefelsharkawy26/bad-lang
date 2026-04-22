using System;
using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers;

public class ReturnHandler : IIRNodeHandler
{
    public System.Collections.Generic.IEnumerable<System.Type> GetHandledTypes() => new[] { typeof(IRReturn) };
    public bool CanHandle(IRNode node) => node is IRReturn;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        var ret = (IRReturn)node;
        var value = ret.Value != null ? context.Eval(ret.Value) : null;
        throw new ReturnSignal(value);
    }
}
