using System;
using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.Advanced;

public class AssertHandler : IIRNodeHandler
{
    public System.Collections.Generic.IEnumerable<System.Type> GetHandledTypes() => new[] { typeof(IRAssert) };
    public bool CanHandle(IRNode node) => node is IRAssert;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        var assert = (IRAssert)node;
        if (!Evaluator.IsTruthy(context.Eval(assert.Condition)))
        {
            throw new Exception("Assertion failed: " + context.Eval(assert.Message));
        }
        return HandlerResult.Continue;
    }
}
