using System.Collections.Generic;
using System.Linq;

namespace BadLang.IR.Optimization
{
    /// <summary>
    /// Tracks `IRAssign` nodes where a variable is assigned another variable (`x = y`).
    /// Replaces subsequent uses of `x` with `y` as long as neither `x` nor `y` are modified.
    /// Due to linear IR, it conservatively invalidates mappings if any branch target is hit or aliased variables are reassigned.
    /// </summary>
    public class CopyPropagationPass : IOptimizationPass
    {
        public string Name => "Copy Propagation";

        public IReadOnlyList<IRNode> Apply(IReadOnlyList<IRNode> nodes)
        {
            var result = new List<IRNode>();
            var knownAliases = new Dictionary<string, string>(); // target -> source

            foreach (var node in nodes)
            {
                // Invalidate all tracked state on jump targets (labels) to be safe across branches
                if (node is IRLabel || node is IRTry || node is IREnterScope || node is IRExitScope)
                {
                    knownAliases.Clear();
                }

                if (node is IRAssign assign)
                {
                    var newValue = ResolveValue(assign.Value, knownAliases);
                    
                    if (newValue is IRVar v)
                    {
                        // x = y
                        InvalidateVariable(assign.Target, knownAliases);
                        knownAliases[assign.Target] = v.Name;
                    }
                    else
                    {
                        InvalidateVariable(assign.Target, knownAliases);
                    }
                    
                    result.Add(assign with { Value = newValue });
                }
                else if (node is IRBinary bin)
                {
                    InvalidateVariable(bin.Target, knownAliases);
                    result.Add(bin with { 
                        Left = ResolveValue(bin.Left, knownAliases), 
                        Right = ResolveValue(bin.Right, knownAliases) 
                    });
                }
                else if (node is IRUnary un)
                {
                    InvalidateVariable(un.Target, knownAliases);
                    result.Add(un with { Operand = ResolveValue(un.Operand, knownAliases) });
                }
                else if (node is IRCondJump cj)
                {
                    result.Add(cj with { Condition = ResolveValue(cj.Condition, knownAliases) });
                }
                else if (node is IRReturn ret && ret.Value != null)
                {
                    result.Add(ret with { Value = ResolveValue(ret.Value, knownAliases) });
                }
                else if (node is IRThrow thr)
                {
                    result.Add(thr with { Exception = ResolveValue(thr.Exception, knownAliases) });
                }
                else if (node is IRCall call)
                {
                    InvalidateVariable(call.Target, knownAliases);
                    var newArgs = new List<IRValue>(call.Arguments.Count);
                    foreach (var arg in call.Arguments) newArgs.Add(ResolveValue(arg, knownAliases));
                    result.Add(call with { Arguments = newArgs });
                }
                else if (node is IRMethodCall mcall)
                {
                    InvalidateVariable(mcall.Target, knownAliases);
                    var newArgs = new List<IRValue>(mcall.Arguments.Count);
                    foreach (var arg in mcall.Arguments) newArgs.Add(ResolveValue(arg, knownAliases));
                    result.Add(mcall with { 
                        Object = ResolveValue(mcall.Object, knownAliases),
                        Arguments = newArgs 
                    });
                }
                else if (node is IRLoad load)
                {
                    InvalidateVariable(load.Target, knownAliases);
                    result.Add(load);
                }
                else if (node is IRStore store)
                {
                    InvalidateVariable(store.VariableName, knownAliases);
                    result.Add(store with { Value = ResolveValue(store.Value, knownAliases) });
                }
                else if (node is IRDefine def)
                {
                    InvalidateVariable(def.VariableName, knownAliases);
                    result.Add(def with { Value = ResolveValue(def.Value, knownAliases) });
                }
                else if (node is IRPropertyGet pget)
                {
                    InvalidateVariable(pget.Target, knownAliases);
                    result.Add(pget with { Object = ResolveValue(pget.Object, knownAliases) });
                }
                else if (node is IRPropertySet pset)
                {
                    result.Add(pset with { 
                        Object = ResolveValue(pset.Object, knownAliases),
                        Value = ResolveValue(pset.Value, knownAliases) 
                    });
                }
                else if (node is IRIndexGet iget)
                {
                    InvalidateVariable(iget.Target, knownAliases);
                    result.Add(iget with { 
                        ArrayOrMap = ResolveValue(iget.ArrayOrMap, knownAliases),
                        Index = ResolveValue(iget.Index, knownAliases)
                    });
                }
                else if (node is IRIndexSet iset)
                {
                    result.Add(iset with { 
                        ArrayOrMap = ResolveValue(iset.ArrayOrMap, knownAliases),
                        Index = ResolveValue(iset.Index, knownAliases),
                        Value = ResolveValue(iset.Value, knownAliases)
                    });
                }
                else if (node is IRNewArray narr)
                {
                    InvalidateVariable(narr.Target, knownAliases);
                    var newElems = new List<IRValue>(narr.Elements.Count);
                    foreach (var el in narr.Elements) newElems.Add(ResolveValue(el, knownAliases));
                    result.Add(narr with { Elements = newElems });
                }
                else if (node is IRNewMap nmap)
                {
                    InvalidateVariable(nmap.Target, knownAliases);
                    result.Add(nmap);
                }
                else if (node is IRNew newObj)
                {
                    InvalidateVariable(newObj.Target, knownAliases);
                    var newArgs = new List<IRValue>(newObj.Arguments.Count);
                    foreach (var arg in newObj.Arguments) newArgs.Add(ResolveValue(arg, knownAliases));
                    result.Add(newObj with { 
                        Class = ResolveValue(newObj.Class, knownAliases),
                        Arguments = newArgs 
                    });
                }
                else if (node is IRLambda lam)
                {
                    InvalidateVariable(lam.Target, knownAliases);
                    result.Add(lam);
                }
                else if (node is IRSuperPropertyGet spg)
                {
                    InvalidateVariable(spg.Target, knownAliases);
                    result.Add(spg);
                }
                else if (node is IRSuperMethodCall smc)
                {
                    InvalidateVariable(smc.Target, knownAliases);
                    var newArgs = new List<IRValue>(smc.Arguments.Count);
                    foreach (var arg in smc.Arguments) newArgs.Add(ResolveValue(arg, knownAliases));
                    result.Add(smc with { Arguments = newArgs });
                }
                else if (node is IRAssert ast)
                {
                    result.Add(ast with { 
                        Condition = ResolveValue(ast.Condition, knownAliases),
                        Message = ResolveValue(ast.Message, knownAliases)
                    });
                }
                else if (node is IRPanic pan)
                {
                    result.Add(pan with { Message = ResolveValue(pan.Message, knownAliases) });
                }
                else
                {
                    result.Add(node);
                }
            }

            return result;
        }

        private IRValue ResolveValue(IRValue value, Dictionary<string, string> knownAliases)
        {
            if (value is IRVar v && knownAliases.TryGetValue(v.Name, out var source))
            {
                return new IRVar(source);
            }
            return value;
        }

        private void InvalidateVariable(string varName, Dictionary<string, string> knownAliases)
        {
            // If the variable is assigned to, it's no longer an alias for whatever it was.
            knownAliases.Remove(varName);

            // Also, any aliases that point TO this variable are now invalid because its value changed.
            var keysToRemove = knownAliases.Where(kvp => kvp.Value == varName).Select(kvp => kvp.Key).ToList();
            foreach (var key in keysToRemove)
            {
                knownAliases.Remove(key);
            }
        }
    }
}
