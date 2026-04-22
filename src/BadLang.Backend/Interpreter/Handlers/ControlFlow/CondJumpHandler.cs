using System;
using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.ControlFlow;

public class CondJumpHandler : IIRNodeHandler
{
    public System.Collections.Generic.IEnumerable<System.Type> GetHandledTypes() => new[] { typeof(IRCondJump) };
    public bool CanHandle(IRNode node) => node is IRCondJump;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        var cjmp = (IRCondJump)node;
        bool cond = Evaluator.IsTruthy(context.Eval(cjmp.Condition));
        var targetLabel = cond ? cjmp.TrueLabel : cjmp.FalseLabel;
        
        context.InstructionPointer = context.Labels[targetLabel];
        return HandlerResult.Jump;
    }
}
