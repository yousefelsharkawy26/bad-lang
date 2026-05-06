using BadLang.IR;

namespace BadLang.Backend.Interpreter.Handlers.Variables;

public class DefineHandler : IIrNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => [typeof(IrDefine)];
    public bool CanHandle(IrNode node) => node is IrDefine;

    public HandlerResult Handle(IrNode node, ExecutionContext context)
    {
        var def = (IrDefine)node;
        context.Environment.Define(def.VariableName, context.Eval(def.Value));
        return HandlerResult.Continue;
    }
}
