using System;
using BadLang.Backend.Interpreter.Handlers;
using BadLang.IR;

namespace BadLang.Backend.Interpreter;

public interface IIRNodeHandler
{
    IEnumerable<Type> GetHandledTypes();
    bool CanHandle(IRNode node);
    HandlerResult Handle(IRNode node, ExecutionContext context);
}
