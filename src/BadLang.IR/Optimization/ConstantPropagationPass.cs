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

        public IReadOnlyList<IRNode> Apply(IReadOnlyList<IRNode> nodes)
        {
            var result = new List<IRNode>();
            var knownConstants = new Dictionary<string, IRConst>();

            foreach (var node in nodes)
            {
                // Invalidate all tracked state on jump targets (labels) to be safe across branches
                if (node is IRLabel || node is IRTry || node is IREnterScope || node is IRExitScope)
                {
                    knownConstants.Clear();
                }

                if (node is IRAssign assign)
                {
                    // Evaluate new value with known constants
                    var newValue = ResolveValue(assign.Value, knownConstants);
                    
                    if (newValue is IRConst c)
                    {
                        knownConstants[assign.Target] = c;
                    }
                    else
                    {
                        knownConstants.Remove(assign.Target);
                    }
                    
                    result.Add(assign with { Value = newValue });
                }
                else if (node is IRBinary bin)
                {
                    knownConstants.Remove(bin.Target);
                    result.Add(bin with { 
                        Left = ResolveValue(bin.Left, knownConstants), 
                        Right = ResolveValue(bin.Right, knownConstants) 
                    });
                }
                else if (node is IRUnary un)
                {
                    knownConstants.Remove(un.Target);
                    result.Add(un with { Operand = ResolveValue(un.Operand, knownConstants) });
                }
                else if (node is IRCondJump cj)
                {
                    result.Add(cj with { Condition = ResolveValue(cj.Condition, knownConstants) });
                }
                else if (node is IRReturn ret && ret.Value != null)
                {
                    result.Add(ret with { Value = ResolveValue(ret.Value, knownConstants) });
                }
                else if (node is IRThrow thr)
                {
                    result.Add(thr with { Exception = ResolveValue(thr.Exception, knownConstants) });
                }
                else if (node is IRCall call)
                {
                    knownConstants.Remove(call.Target);
                    var newArgs = new List<IRValue>(call.Arguments.Count);
                    foreach (var arg in call.Arguments) newArgs.Add(ResolveValue(arg, knownConstants));
                    result.Add(call with { Arguments = newArgs });
                }
                else if (node is IRMethodCall mcall)
                {
                    knownConstants.Remove(mcall.Target);
                    var newArgs = new List<IRValue>(mcall.Arguments.Count);
                    foreach (var arg in mcall.Arguments) newArgs.Add(ResolveValue(arg, knownConstants));
                    result.Add(mcall with { 
                        Object = ResolveValue(mcall.Object, knownConstants),
                        Arguments = newArgs 
                    });
                }
                else if (node is IRLoad load)
                {
                    knownConstants.Remove(load.Target);
                    result.Add(load); // Load variable by name, doesn't directly use an IRValue we can replace
                }
                else if (node is IRStore store)
                {
                    // store modifies a variable
                    // If it modifies a variable that happens to match our tracking name, we should invalidate
                    // But typically IRVar targets are local compiler temps, whereas store writes to named variables.
                    // Just to be safe, if there's an overlap in names (though unlikely in clean IR), invalidate.
                    knownConstants.Remove(store.VariableName);
                    result.Add(store with { Value = ResolveValue(store.Value, knownConstants) });
                }
                else if (node is IRDefine def)
                {
                    knownConstants.Remove(def.VariableName);
                    result.Add(def with { Value = ResolveValue(def.Value, knownConstants) });
                }
                else if (node is IRPropertyGet pget)
                {
                    knownConstants.Remove(pget.Target);
                    result.Add(pget with { Object = ResolveValue(pget.Object, knownConstants) });
                }
                else if (node is IRPropertySet pset)
                {
                    result.Add(pset with { 
                        Object = ResolveValue(pset.Object, knownConstants),
                        Value = ResolveValue(pset.Value, knownConstants) 
                    });
                }
                else if (node is IRIndexGet iget)
                {
                    knownConstants.Remove(iget.Target);
                    result.Add(iget with { 
                        ArrayOrMap = ResolveValue(iget.ArrayOrMap, knownConstants),
                        Index = ResolveValue(iget.Index, knownConstants)
                    });
                }
                else if (node is IRIndexSet iset)
                {
                    result.Add(iset with { 
                        ArrayOrMap = ResolveValue(iset.ArrayOrMap, knownConstants),
                        Index = ResolveValue(iset.Index, knownConstants),
                        Value = ResolveValue(iset.Value, knownConstants)
                    });
                }
                else if (node is IRNewArray narr)
                {
                    knownConstants.Remove(narr.Target);
                    var newElems = new List<IRValue>(narr.Elements.Count);
                    foreach (var el in narr.Elements) newElems.Add(ResolveValue(el, knownConstants));
                    result.Add(narr with { Elements = newElems });
                }
                else if (node is IRNewMap nmap)
                {
                    knownConstants.Remove(nmap.Target);
                    result.Add(nmap);
                }
                else if (node is IRNew newObj)
                {
                    knownConstants.Remove(newObj.Target);
                    var newArgs = new List<IRValue>(newObj.Arguments.Count);
                    foreach (var arg in newObj.Arguments) newArgs.Add(ResolveValue(arg, knownConstants));
                    result.Add(newObj with { 
                        Class = ResolveValue(newObj.Class, knownConstants),
                        Arguments = newArgs 
                    });
                }
                else if (node is IRLambda lam)
                {
                    knownConstants.Remove(lam.Target);
                    // Pass bodies are not optimized by this simple linear pass unless recursed into.
                    // For now, we leave the body intact.
                    result.Add(lam);
                }
                else if (node is IRSuperPropertyGet spg)
                {
                    knownConstants.Remove(spg.Target);
                    result.Add(spg);
                }
                else if (node is IRSuperMethodCall smc)
                {
                    knownConstants.Remove(smc.Target);
                    var newArgs = new List<IRValue>(smc.Arguments.Count);
                    foreach (var arg in smc.Arguments) newArgs.Add(ResolveValue(arg, knownConstants));
                    result.Add(smc with { Arguments = newArgs });
                }
                else if (node is IRAssert ast)
                {
                    result.Add(ast with { 
                        Condition = ResolveValue(ast.Condition, knownConstants),
                        Message = ResolveValue(ast.Message, knownConstants)
                    });
                }
                else if (node is IRPanic pan)
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

        private IRValue ResolveValue(IRValue value, Dictionary<string, IRConst> knownConstants)
        {
            if (value is IRVar v && knownConstants.TryGetValue(v.Name, out var constant))
            {
                return constant;
            }
            return value;
        }
    }
}
