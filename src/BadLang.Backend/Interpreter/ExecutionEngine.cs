using System;
using System.Collections.Generic;
using BadLang.Backend.Interpreter.Handlers;
using BadLang.Backend.Interpreter.Runtime;
using BadLang.IR;

namespace BadLang.Backend.Interpreter;

public class ExecutionEngine
{
    private readonly HandlerRegistry _registry;

    public ExecutionEngine(HandlerRegistry registry)
    {
        _registry = registry;
    }

    public object? Execute(IReadOnlyList<IRNode> nodes, ExecutionContext context)
    {
        while (context.InstructionPointer < nodes.Count)
        {
            var node = nodes[context.InstructionPointer];
            var handler = _registry.Get(node);

            if (handler == null)
            {
                throw new Exception($"No handler registered for IRNode type: {node.GetType().Name}");
            }

            try
            {
                var result = handler.Handle(node, context);
                if (result.AdvanceIP)
                {
                    context.InstructionPointer++;
                }
            }
            catch (ReturnSignal rs)
            {
                return rs.Value;
            }
            catch (Exception ex)
            {
                if (context.TryStack.Count > 0)
                {
                    var tryCtx = context.TryStack.Pop();
                    context.Environment = tryCtx.SavedEnv;

                    object? exceptionValue = ex is BadLangException ble ? ble.Value : ex.Message;
                    context.Environment.Define("__exception", exceptionValue);

                    context.InstructionPointer = context.Labels[tryCtx.CatchLabel];
                    continue;
                }
                throw;
            }
        }

        return null;
    }
}
