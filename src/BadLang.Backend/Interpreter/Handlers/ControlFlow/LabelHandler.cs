using BadLang.IR;

namespace BadLang.Backend.Interpreter.Handlers.ControlFlow;

public class LabelHandler : IIrNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => [typeof(IrLabel)];
    public bool CanHandle(IrNode node) => node is IrLabel;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        // Labels are handled during pre-pass, so they are no-ops during execution
        return HandlerResult.Continue;
    }
}
