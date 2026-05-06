using BadLang.IR;

namespace BadLang.Backend.Interpreter.Handlers.ControlFlow;

public class JumpHandler : IIrNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => new[] { typeof(IrJump) };
    public bool CanHandle(IrNode node) => node is IrJump;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        var jmp = (IrJump)node;
        context.InstructionPointer = context.Labels[jmp.TargetLabel];
        return HandlerResult.Jump;
    }
}
