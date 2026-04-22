using System;
using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.ControlFlow;

public class LabelHandler : IIRNodeHandler
{
    public System.Collections.Generic.IEnumerable<System.Type> GetHandledTypes() => new[] { typeof(IRLabel) };
    public bool CanHandle(IRNode node) => node is IRLabel;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        // Labels are handled during pre-pass, so they are no-ops during execution
        return HandlerResult.Continue;
    }
}
