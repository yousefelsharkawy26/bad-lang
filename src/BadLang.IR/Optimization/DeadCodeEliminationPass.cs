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

    public IReadOnlyList<IrNode> Apply(IReadOnlyList<IrNode> nodes)
    {
        // Step 1: Collect all variable names that are READ by any instruction
        var usedVars = new HashSet<string>();
        CollectUsedVars(nodes, usedVars);

        // Step 2: Filter out dead assignments to unused temps
        var result = new List<IrNode>(nodes.Count);
        foreach (var node in nodes)
        {
            if (IsDeadAssignment(node, usedVars))
                continue;

            // Recurse into nested bodies
            switch (node)
            {
                case IrFunctionDef funcDef:
                    result.Add(new IrFunctionDef
                    {
                        Name = funcDef.Name,
                        Parameters = funcDef.Parameters,
                        Body = Apply(funcDef.Body)
                    });
                    break;

                case IrClassDef classDef:
                    var optimizedMethods = new List<IrFunctionDef>();
                    foreach (var method in classDef.Methods)
                    {
                        optimizedMethods.Add(new IrFunctionDef
                        {
                            Name = method.Name,
                            Parameters = method.Parameters,
                            Body = Apply(method.Body)
                        });
                    }
                    result.Add(new IrClassDef
                    {
                        Name = classDef.Name,
                        SuperClass = classDef.SuperClass,
                        Methods = optimizedMethods,
                        Fields = classDef.Fields
                    });
                    break;

                case IrLambda lambda:
                    result.Add(new IrLambda
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
    private bool IsDeadAssignment(IrNode node, HashSet<string> usedVars)
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
    private string? GetPureTarget(IrNode node)
    {
        return node switch
        {
            IrAssign a => a.Target,
            IrBinary b => b.Target,
            IrUnary u => u.Target,
            IrLoad l => l.Target,
            IrNewArray na => na.Target,
            IrNewMap nm => nm.Target,
            IrIndexGet ig => ig.Target,
            IrPropertyGet pg => pg.Target,
            IrNew n => n.Target,
            // IRCall, IRMethodCall, IRSuperMethodCall are side-effecting — never eliminate
            // IRStore, IRDefine, IRPropertySet, IRIndexSet mutate state — never eliminate
            // IRExport, IRImport, IRThrow, IRAssert, IRPanic are side-effecting
            _ => null
        };
    }

    /// <summary>
    /// Walk all nodes and collect variable names that appear as READ operands.
    /// </summary>
    private void CollectUsedVars(IReadOnlyList<IrNode> nodes, HashSet<string> used)
    {
        foreach (var node in nodes)
        {
            CollectUsedVarsFromNode(node, used);
        }
    }

    private void CollectUsedVarsFromNode(IrNode node, HashSet<string> used)
    {
        switch (node)
        {
            case IrAssign a:
                CollectFromValue(a.Value, used);
                break;
            case IrBinary b:
                CollectFromValue(b.Left, used);
                CollectFromValue(b.Right, used);
                break;
            case IrUnary u:
                CollectFromValue(u.Operand, used);
                break;
            case IrStore s:
                CollectFromValue(s.Value, used);
                break;
            case IrDefine d:
                CollectFromValue(d.Value, used);
                break;
            case IrReturn r:
                if (r.Value != null) CollectFromValue(r.Value, used);
                break;
            case IrCondJump cj:
                CollectFromValue(cj.Condition, used);
                break;
            case IrCall c:
                foreach (var arg in c.Arguments) CollectFromValue(arg, used);
                break;
            case IrMethodCall mc:
                CollectFromValue(mc.Object, used);
                foreach (var arg in mc.Arguments) CollectFromValue(arg, used);
                break;
            case IrSuperMethodCall smc:
                foreach (var arg in smc.Arguments) CollectFromValue(arg, used);
                break;
            case IrPropertyGet pg:
                CollectFromValue(pg.Object, used);
                break;
            case IrPropertySet ps:
                CollectFromValue(ps.Object, used);
                CollectFromValue(ps.Value, used);
                break;
            case IrIndexGet ig:
                CollectFromValue(ig.ArrayOrMap, used);
                CollectFromValue(ig.Index, used);
                break;
            case IrIndexSet iset:
                CollectFromValue(iset.ArrayOrMap, used);
                CollectFromValue(iset.Index, used);
                CollectFromValue(iset.Value, used);
                break;
            case IrNew n:
                CollectFromValue(n.Class, used);
                foreach (var arg in n.Arguments) CollectFromValue(arg, used);
                break;
            case IrNewArray na:
                foreach (var el in na.Elements) CollectFromValue(el, used);
                break;
            case IrThrow t:
                CollectFromValue(t.Exception, used);
                break;
            case IrAssert assert:
                CollectFromValue(assert.Condition, used);
                CollectFromValue(assert.Message, used);
                break;
            case IrPanic panic:
                CollectFromValue(panic.Message, used);
                break;
            case IrFunctionDef fd:
                CollectUsedVars(fd.Body, used);
                break;
            case IrClassDef cd:
                foreach (var m in cd.Methods) CollectUsedVars(m.Body, used);
                break;
            case IrLambda lam:
                CollectUsedVars(lam.Body, used);
                // The lambda target itself is "used" because it's captured
                used.Add(lam.Target);
                break;
        }
    }

    private void CollectFromValue(IrValue value, HashSet<string> used)
    {
        if (value is IrVar v)
            used.Add(v.Name);
    }
}
