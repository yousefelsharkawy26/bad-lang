using BadLang.IR;

namespace BadLang.Backend.Interpreter.Handlers.Advanced;

public class AssertHandler : IIrNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => [ typeof(IrAssert) ];
    public bool CanHandle(IrNode node) => node is IrAssert;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        var assert = (IrAssert)node;
        if (!Evaluator.IsTruthy(context.Eval(assert.Condition)))
        {
            throw new Exception("Assertion failed: " + context.Eval(assert.Message));
        }
        return HandlerResult.Continue;
    }
}
