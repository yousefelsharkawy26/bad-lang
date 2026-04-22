using System;
using System.Collections.Generic;
using System.Linq;

namespace BadLang.IR.Optimization;

/// <summary>
/// Dead Code Elimination Pass: removes IR instructions whose results are never consumed.
///
/// Rules:
///   1. Track which temp variables (_tN) are read by any subsequent instruction.
///   2. If an instruction writes to a temp that is never read, and has no side effects, remove it.
///   3. Never remove side-effecting nodes: IRCall, IRMethodCall, IRStore, IRPropertySet,
///      IRIndexSet, IRExport, IRImport, IRThrow, IRPanic, IRAssert, IRReturn, etc.
///   4. Never remove control flow: IRLabel, IRJump, IRCondJump, IRTry, IRPopTry.
///   5. Never remove definitions: IRDefine, IRFunctionDef, IRClassDef, IRStructDef, IREnumDef.
/// </summary>
public class DeadCodeEliminationPass : IOptimizationPass
{
    public string Name => "DeadCodeElimination";

    public IReadOnlyList<IRNode> Apply(IReadOnlyList<IRNode> nodes)
    {
        // Step 1: Collect all variable names that are READ by any instruction
        var usedVars = new HashSet<string>();
        CollectUsedVars(nodes, usedVars);

        // Step 2: Filter out dead assignments to unused temps
        var result = new List<IRNode>(nodes.Count);
        foreach (var node in nodes)
        {
            if (IsDeadAssignment(node, usedVars))
                continue;

            // Recurse into nested bodies
            switch (node)
            {
                case IRFunctionDef funcDef:
                    result.Add(new IRFunctionDef
                    {
                        Name = funcDef.Name,
                        Parameters = funcDef.Parameters,
                        Body = Apply(funcDef.Body)
                    });
                    break;

                case IRClassDef classDef:
                    var optimizedMethods = new List<IRFunctionDef>();
                    foreach (var method in classDef.Methods)
                    {
                        optimizedMethods.Add(new IRFunctionDef
                        {
                            Name = method.Name,
                            Parameters = method.Parameters,
                            Body = Apply(method.Body)
                        });
                    }
                    result.Add(new IRClassDef
                    {
                        Name = classDef.Name,
                        SuperClass = classDef.SuperClass,
                        Methods = optimizedMethods,
                        Fields = classDef.Fields
                    });
                    break;

                case IRLambda lambda:
                    result.Add(new IRLambda
                    {
                        Target = lambda.Target,
                        Parameters = lambda.Parameters,
                        Body = Apply(lambda.Body),
                        CapturedVariables = lambda.CapturedVariables
                    });
                    break;

                default:
                    result.Add(node);
                    break;
            }
        }

        return result;
    }

    /// <summary>
    /// Determines if a node is a dead assignment: writes to a temp that nobody reads,
    /// AND has no side effects.
    /// </summary>
    private bool IsDeadAssignment(IRNode node, HashSet<string> usedVars)
    {
        string? target = GetPureTarget(node);
        if (target == null) return false; // side-effecting or non-assignment

        // Only eliminate compiler-generated temps (_tN), never user variables
        if (!target.StartsWith("_t")) return false;

        return !usedVars.Contains(target);
    }

    /// <summary>
    /// Returns the target variable if the node is a pure (side-effect-free) assignment.
    /// Returns null for side-effecting nodes.
    /// </summary>
    private string? GetPureTarget(IRNode node)
    {
        return node switch
        {
            IRAssign a => a.Target,
            IRBinary b => b.Target,
            IRUnary u => u.Target,
            IRLoad l => l.Target,
            IRNewArray na => na.Target,
            IRNewMap nm => nm.Target,
            IRIndexGet ig => ig.Target,
            IRPropertyGet pg => pg.Target,
            IRNew n => n.Target,
            // IRCall, IRMethodCall, IRSuperMethodCall are side-effecting — never eliminate
            // IRStore, IRDefine, IRPropertySet, IRIndexSet mutate state — never eliminate
            // IRExport, IRImport, IRThrow, IRAssert, IRPanic are side-effecting
            _ => null
        };
    }

    /// <summary>
    /// Walk all nodes and collect variable names that appear as READ operands.
    /// </summary>
    private void CollectUsedVars(IReadOnlyList<IRNode> nodes, HashSet<string> used)
    {
        foreach (var node in nodes)
        {
            CollectUsedVarsFromNode(node, used);
        }
    }

    private void CollectUsedVarsFromNode(IRNode node, HashSet<string> used)
    {
        switch (node)
        {
            case IRAssign a:
                CollectFromValue(a.Value, used);
                break;
            case IRBinary b:
                CollectFromValue(b.Left, used);
                CollectFromValue(b.Right, used);
                break;
            case IRUnary u:
                CollectFromValue(u.Operand, used);
                break;
            case IRStore s:
                CollectFromValue(s.Value, used);
                break;
            case IRDefine d:
                CollectFromValue(d.Value, used);
                break;
            case IRReturn r:
                if (r.Value != null) CollectFromValue(r.Value, used);
                break;
            case IRCondJump cj:
                CollectFromValue(cj.Condition, used);
                break;
            case IRCall c:
                foreach (var arg in c.Arguments) CollectFromValue(arg, used);
                break;
            case IRMethodCall mc:
                CollectFromValue(mc.Object, used);
                foreach (var arg in mc.Arguments) CollectFromValue(arg, used);
                break;
            case IRSuperMethodCall smc:
                foreach (var arg in smc.Arguments) CollectFromValue(arg, used);
                break;
            case IRPropertyGet pg:
                CollectFromValue(pg.Object, used);
                break;
            case IRPropertySet ps:
                CollectFromValue(ps.Object, used);
                CollectFromValue(ps.Value, used);
                break;
            case IRIndexGet ig:
                CollectFromValue(ig.ArrayOrMap, used);
                CollectFromValue(ig.Index, used);
                break;
            case IRIndexSet iset:
                CollectFromValue(iset.ArrayOrMap, used);
                CollectFromValue(iset.Index, used);
                CollectFromValue(iset.Value, used);
                break;
            case IRNew n:
                CollectFromValue(n.Class, used);
                foreach (var arg in n.Arguments) CollectFromValue(arg, used);
                break;
            case IRNewArray na:
                foreach (var el in na.Elements) CollectFromValue(el, used);
                break;
            case IRThrow t:
                CollectFromValue(t.Exception, used);
                break;
            case IRAssert assert:
                CollectFromValue(assert.Condition, used);
                CollectFromValue(assert.Message, used);
                break;
            case IRPanic panic:
                CollectFromValue(panic.Message, used);
                break;
            case IRFunctionDef fd:
                CollectUsedVars(fd.Body, used);
                break;
            case IRClassDef cd:
                foreach (var m in cd.Methods) CollectUsedVars(m.Body, used);
                break;
            case IRLambda lam:
                CollectUsedVars(lam.Body, used);
                // The lambda target itself is "used" because it's captured
                used.Add(lam.Target);
                break;
        }
    }

    private void CollectFromValue(IRValue value, HashSet<string> used)
    {
        if (value is IRVar v)
            used.Add(v.Name);
    }
}
