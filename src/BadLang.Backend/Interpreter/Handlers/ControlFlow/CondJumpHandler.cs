using BadLang.IR;

namespace BadLang.Backend.Interpreter.Handlers.ControlFlow;

public class CondJumpHandler : IIrNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => [typeof(IrCondJump)];
    public bool CanHandle(IrNode node) => node is IrCondJump;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        var cjmp = (IrCondJump)node;
        bool cond = Evaluator.IsTruthy(context.Eval(cjmp.Condition));
        var targetLabel = cond ? cjmp.TrueLabel : cjmp.FalseLabel;
        
        context.InstructionPointer = context.Labels[targetLabel];
        return HandlerResult.Jump;
    }
}
