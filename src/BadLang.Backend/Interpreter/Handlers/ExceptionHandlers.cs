using System;
using BadLang.IR;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter.Handlers;

public class ExceptionHandlers : IIRNodeHandler
{
    public IEnumerable<Type> GetHandledTypes() => new[] { typeof(IRTry), typeof(IRPopTry), typeof(IRThrow) };
    public bool CanHandle(IRNode node) => 
        node is IRTry || 
        node is IRPopTry || 
        node is IRThrow;

    public HandlerResult Handle(IRNode node, ExecutionContext context)
    {
        var env = context.Environment;
        var tryStack = context.TryStack;
        var interpreter = context.Interpreter;

        switch (node)
        {
            case IRTry tryNode:
                tryStack.Push(new TryContext
                {
                    CatchLabel = tryNode.CatchLabel,
                    FinallyLabel = tryNode.FinallyLabel,
                    SavedEnv = env
                });
                break;
            case IRPopTry _:
                tryStack.Pop();
                break;
            case IRThrow thrw:
                throw new BadLangException(interpreter.GetValueInternal(thrw.Exception, env));
        }
        
        return HandlerResult.Continue;
    }
}
