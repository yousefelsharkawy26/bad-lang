using System;
using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers.ControlFlow;

public class JumpHandler : IIRNodeHandler
{
    public System.Collections.Generic.IEnumerable<System.Type> GetHandledTypes() => new[] { typeof(IRJump) };
    public bool CanHandle(IRNode node) => node is IRJump;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        var jmp = (IRJump)node;
        context.InstructionPointer = context.Labels[jmp.TargetLabel];
        return HandlerResult.Jump;
    }
}
