using System;
using System.Collections.Generic;
using BadLang.Parser;
using BadLang.Parser.Ast;

namespace BadLang.IR.Handlers.Expressions
{
    public class BinaryExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.Binary);
        public IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context)
        {
            var binExpr = (Expr.Binary)expr;
            var left = context.BuildExpr(binExpr.Left, ir);
            var right = context.BuildExpr(binExpr.Right, ir);
            var target = context.NextTemp();
            ir.Add(new IrBinary { Target = target, Op = binExpr.Operator.Lexeme, Left = left, Right = right });
            return new IrVar(target);
        }
    }

    public class UnaryExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.Unary);
        public IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context)
        {
            var unaryExpr = (Expr.Unary)expr;
            var operand = context.BuildExpr(unaryExpr.Right, ir);
            var target = context.NextTemp();
            ir.Add(new IrUnary { Target = target, Op = unaryExpr.Operator.Lexeme, Operand = operand });
            return new IrVar(target);
        }
    }

    public class LogicalExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.Logical);
        public IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context)
        {
            var logicalExpr = (Expr.Logical)expr;
            var target = context.NextTemp();
            var endLabel = context.NextLabel("LOGICAL_END");
            var left = context.BuildExpr(logicalExpr.Left, ir);
            ir.Add(new IrAssign { Target = target, Value = left });

            if (logicalExpr.Operator.Lexeme == "&&")
            {
                var nextLabel = context.NextLabel("LOGICAL_NEXT");
                ir.Add(new IrCondJump { Condition = left, TrueLabel = nextLabel, FalseLabel = endLabel });
                ir.Add(new IrLabel { Name = nextLabel });
                var right = context.BuildExpr(logicalExpr.Right, ir);
                ir.Add(new IrAssign { Target = target, Value = right });
            }
            else if (logicalExpr.Operator.Lexeme == "||")
            {
                var nextLabel = context.NextLabel("LOGICAL_NEXT");
                ir.Add(new IrCondJump { Condition = left, TrueLabel = endLabel, FalseLabel = nextLabel });
                ir.Add(new IrLabel { Name = nextLabel });
                var right = context.BuildExpr(logicalExpr.Right, ir);
                ir.Add(new IrAssign { Target = target, Value = right });
            }

            ir.Add(new IrLabel { Name = endLabel });
            return new IrVar(target);
        }
    }

    public class TernaryExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.Ternary);
        public IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context)
        {
            var ternaryExpr = (Expr.Ternary)expr;
            var cond = context.BuildExpr(ternaryExpr.Condition, ir);
            var trueLabel = context.NextLabel("TERNARY_TRUE");
            var falseLabel = context.NextLabel("TERNARY_FALSE");
            var endLabel = context.NextLabel("TERNARY_END");
            var target = context.NextTemp();

            ir.Add(new IrCondJump { Condition = cond, TrueLabel = trueLabel, FalseLabel = falseLabel });
            
            ir.Add(new IrLabel { Name = trueLabel });
            var trueVal = context.BuildExpr(ternaryExpr.ThenBranch, ir);
            ir.Add(new IrAssign { Target = target, Value = trueVal });
            ir.Add(new IrJump { TargetLabel = endLabel });

            ir.Add(new IrLabel { Name = falseLabel });
            var falseVal = context.BuildExpr(ternaryExpr.ElseBranch, ir);
            ir.Add(new IrAssign { Target = target, Value = falseVal });
            
            ir.Add(new IrLabel { Name = endLabel });
            return new IrVar(target);
        }
    }

    public class NullCoalesceExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.NullCoalesce);
        public IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context)
        {
            var nullCoal = (Expr.NullCoalesce)expr;
            var left = context.BuildExpr(nullCoal.Left, ir);
            var isNullTarget = context.NextTemp();
            ir.Add(new IrUnary { Target = isNullTarget, Op = "isNull", Operand = left });
            
            var nullLabel = context.NextLabel("COALESCE_NULL");
            var notNullLabel = context.NextLabel("COALESCE_NOT_NULL");
            var endLabel = context.NextLabel("COALESCE_END");
            var target = context.NextTemp();

            ir.Add(new IrCondJump { Condition = new IrVar(isNullTarget), TrueLabel = nullLabel, FalseLabel = notNullLabel });
            
            ir.Add(new IrLabel { Name = notNullLabel });
            ir.Add(new IrAssign { Target = target, Value = left });
            ir.Add(new IrJump { TargetLabel = endLabel });

            ir.Add(new IrLabel { Name = nullLabel });
            var right = context.BuildExpr(nullCoal.Right, ir);
            ir.Add(new IrAssign { Target = target, Value = right });
            
            ir.Add(new IrLabel { Name = endLabel });
            return new IrVar(target);
        }
    }
}
