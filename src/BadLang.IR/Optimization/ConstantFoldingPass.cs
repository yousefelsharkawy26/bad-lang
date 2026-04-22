using System;
using System.Collections.Generic;

namespace BadLang.IR.Optimization;

/// <summary>
/// Constant Folding Pass: evaluates binary operations on constant operands at compile time.
/// 
/// Example:
///   _t0 = + IRConst(2), IRConst(3)   →   _t0 = IRConst(5)  (IRAssign)
///
/// Only folds pure numeric operations. String concatenation and operations
/// involving variables are left untouched.
/// </summary>
public class ConstantFoldingPass : IOptimizationPass
{
    public string Name => "ConstantFolding";

    public IReadOnlyList<IRNode> Apply(IReadOnlyList<IRNode> nodes)
    {
        var result = new List<IRNode>(nodes.Count);

        foreach (var node in nodes)
        {
            switch (node)
            {
                case IRBinary bin when bin.Left is IRConst leftConst && bin.Right is IRConst rightConst:
                    var folded = TryFold(bin.Op, leftConst.Value, rightConst.Value);
                    if (folded != null)
                    {
                        result.Add(new IRAssign { Target = bin.Target, Value = new IRConst(folded) });
                    }
                    else
                    {
                        result.Add(node);
                    }
                    break;

                case IRUnary un when un.Operand is IRConst operandConst:
                    var foldedUnary = TryFoldUnary(un.Op, operandConst.Value);
                    if (foldedUnary != null)
                    {
                        result.Add(new IRAssign { Target = un.Target, Value = new IRConst(foldedUnary) });
                    }
                    else
                    {
                        result.Add(node);
                    }
                    break;

                // Recurse into function/class/lambda bodies
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

    private object? TryFold(string op, object? left, object? right)
    {
        // Only fold numeric operations
        if (left is not double l || right is not double r)
        {
            // Special case: string concatenation with two known strings
            if (op == "+" && left is string ls && right is string rs)
                return ls + rs;

            return null;
        }

        return op switch
        {
            "+" => l + r,
            "-" => l - r,
            "*" => l * r,
            "/" => r != 0 ? l / r : null, // Don't fold division by zero
            "%" => r != 0 ? l % r : null,
            "==" => (object)(l == r),
            "!=" => (object)(l != r),
            ">" => (object)(l > r),
            ">=" => (object)(l >= r),
            "<" => (object)(l < r),
            "<=" => (object)(l <= r),
            _ => null
        };
    }

    private object? TryFoldUnary(string op, object? operand)
    {
        if (operand is double d)
        {
            return op switch
            {
                "-" => -d,
                _ => null
            };
        }

        if (op == "!" && operand is bool b)
            return !b;

        if (op == "isNull")
            return operand == null;

        return null;
    }
}
