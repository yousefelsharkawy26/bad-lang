using System.Collections.Generic;

namespace BadLang.IR.Optimization
{
    /// <summary>
    /// Tracks `IRAssign` nodes where a variable is assigned a constant (`x = 5`).
    /// Replaces subsequent uses of `x` with `IRConst(5)` as long as `x` is not modified.
    /// Due to linear IR, it conservatively invalidates mappings if any reassignment or branch target is hit.
    /// </summary>
    public class ConstantPropagationPass : IOptimizationPass
    {
        public string Name => "Constant Propagation";

        public IReadOnlyList<IrNode> Apply(IReadOnlyList<IrNode> nodes)
        {
            var result = new List<IrNode>();
            var knownConstants = new Dictionary<string, IrConst>();

            foreach (var node in nodes)
            {
                // Invalidate all tracked state on jump targets (labels) to be safe across branches
                if (node is IrLabel || node is IrTry || node is IrEnterScope || node is IrExitScope)
                {
                    knownConstants.Clear();
                }

                if (node is IrAssign assign)
                {
                    // Evaluate new value with known constants
                    var newValue = ResolveValue(assign.Value, knownConstants);
                    
                    if (newValue is IrConst c)
                    {
                        knownConstants[assign.Target] = c;
                    }
                    else
                    {
                        knownConstants.Remove(assign.Target);
                    }
                    
                    result.Add(assign with { Value = newValue });
                }
                else if (node is IrBinary bin)
                {
                    knownConstants.Remove(bin.Target);
                    result.Add(bin with { 
                        Left = ResolveValue(bin.Left, knownConstants), 
                        Right = ResolveValue(bin.Right, knownConstants) 
                    });
                }
                else if (node is IrUnary un)
                {
                    knownConstants.Remove(un.Target);
                    result.Add(un with { Operand = ResolveValue(un.Operand, knownConstants) });
                }
                else if (node is IrCondJump cj)
                {
                    result.Add(cj with { Condition = ResolveValue(cj.Condition, knownConstants) });
                }
                else if (node is IrReturn ret && ret.Value != null)
                {
                    result.Add(ret with { Value = ResolveValue(ret.Value, knownConstants) });
                }
                else if (node is IrThrow thr)
                {
                    result.Add(thr with { Exception = ResolveValue(thr.Exception, knownConstants) });
                }
                else if (node is IrCall call)
                {
                    knownConstants.Remove(call.Target);
                    var newArgs = new List<IrValue>(call.Arguments.Count);
                    foreach (var arg in call.Arguments) newArgs.Add(ResolveValue(arg, knownConstants));
                    result.Add(call with { Arguments = newArgs });
                }
                else if (node is IrMethodCall mcall)
                {
                    knownConstants.Remove(mcall.Target);
                    var newArgs = new List<IrValue>(mcall.Arguments.Count);
                    foreach (var arg in mcall.Arguments) newArgs.Add(ResolveValue(arg, knownConstants));
                    result.Add(mcall with { 
                        Object = ResolveValue(mcall.Object, knownConstants),
                        Arguments = newArgs 
                    });
                }
                else if (node is IrLoad load)
                {
                    knownConstants.Remove(load.Target);
                    result.Add(load); // Load variable by name, doesn't directly use an IRValue we can replace
                }
                else if (node is IrStore store)
                {
                    // store modifies a variable
                    // If it modifies a variable that happens to match our tracking name, we should invalidate
                    // But typically IRVar targets are local compiler temps, whereas store writes to named variables.
                    // Just to be safe, if there's an overlap in names (though unlikely in clean IR), invalidate.
                    knownConstants.Remove(store.VariableName);
                    result.Add(store with { Value = ResolveValue(store.Value, knownConstants) });
                }
                else if (node is IrDefine def)
                {
                    knownConstants.Remove(def.VariableName);
                    result.Add(def with { Value = ResolveValue(def.Value, knownConstants) });
                }
                else if (node is IrPropertyGet pget)
                {
                    knownConstants.Remove(pget.Target);
                    result.Add(pget with { Object = ResolveValue(pget.Object, knownConstants) });
                }
                else if (node is IrPropertySet pset)
                {
                    result.Add(pset with { 
                        Object = ResolveValue(pset.Object, knownConstants),
                        Value = ResolveValue(pset.Value, knownConstants) 
                    });
                }
                else if (node is IrIndexGet iget)
                {
                    knownConstants.Remove(iget.Target);
                    result.Add(iget with { 
                        ArrayOrMap = ResolveValue(iget.ArrayOrMap, knownConstants),
                        Index = ResolveValue(iget.Index, knownConstants)
                    });
                }
                else if (node is IrIndexSet iset)
                {
                    result.Add(iset with { 
                        ArrayOrMap = ResolveValue(iset.ArrayOrMap, knownConstants),
                        Index = ResolveValue(iset.Index, knownConstants),
                        Value = ResolveValue(iset.Value, knownConstants)
                    });
                }
                else if (node is IrNewArray narr)
                {
                    knownConstants.Remove(narr.Target);
                    var newElems = new List<IrValue>(narr.Elements.Count);
                    foreach (var el in narr.Elements) newElems.Add(ResolveValue(el, knownConstants));
                    result.Add(narr with { Elements = newElems });
                }
                else if (node is IrNewMap nmap)
                {
                    knownConstants.Remove(nmap.Target);
                    result.Add(nmap);
                }
                else if (node is IrNew newObj)
                {
                    knownConstants.Remove(newObj.Target);
                    var newArgs = new List<IrValue>(newObj.Arguments.Count);
                    foreach (var arg in newObj.Arguments) newArgs.Add(ResolveValue(arg, knownConstants));
                    result.Add(newObj with { 
                        Class = ResolveValue(newObj.Class, knownConstants),
                        Arguments = newArgs 
                    });
                }
                else if (node is IrLambda lam)
                {
                    knownConstants.Remove(lam.Target);
                    // Pass bodies are not optimized by this simple linear pass unless recursed into.
                    // For now, we leave the body intact.
                    result.Add(lam);
                }
                else if (node is IrSuperPropertyGet spg)
                {
                    knownConstants.Remove(spg.Target);
                    result.Add(spg);
                }
                else if (node is IrSuperMethodCall smc)
                {
                    knownConstants.Remove(smc.Target);
                    var newArgs = new List<IrValue>(smc.Arguments.Count);
                    foreach (var arg in smc.Arguments) newArgs.Add(ResolveValue(arg, knownConstants));
                    result.Add(smc with { Arguments = newArgs });
                }
                else if (node is IrAssert ast)
                {
                    result.Add(ast with { 
                        Condition = ResolveValue(ast.Condition, knownConstants),
                        Message = ResolveValue(ast.Message, knownConstants)
                    });
                }
                else if (node is IrPanic pan)
                {
                    result.Add(pan with { Message = ResolveValue(pan.Message, knownConstants) });
                }
                else
                {
                    // For any unknown or unhandled nodes that might assign a target, we should be conservative.
                    // But we'd need reflection or hardcoding. Let's assume standard nodes don't have Targets if not listed.
                    // Functions, classes, etc. don't modify local temps.
                    result.Add(node);
                }
            }

            return result;
        }

        private IrValue ResolveValue(IrValue value, Dictionary<string, IrConst> knownConstants)
        {
            if (value is IrVar v && knownConstants.TryGetValue(v.Name, out var constant))
            {
                return constant;
            }
            return value;
        }
    }
}
