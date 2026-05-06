using BadLang.IR;
using BadLang.Backend.Interpreter.Handlers;

namespace BadLang.Backend.Interpreter;

public interface IIrNodeHandler
{
    IEnumerable<Type> GetHandledTypes();
    bool CanHandle(IrNode node);
    HandlerResult Handle(IrNode node, ExecutionContext context);
}
