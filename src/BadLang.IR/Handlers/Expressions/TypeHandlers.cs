using System;
using System.Collections.Generic;
using BadLang.Parser;
using BadLang.Core;
using BadLang.Parser.Ast;

namespace BadLang.IR.Handlers.Expressions
{
    public class TypeCastExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.TypeCast);
        public IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context)
        {
            var castExpr = (Expr.TypeCast)expr;
            var val = context.BuildExpr(castExpr.Expr, ir);
            if (castExpr.TargetType is TypeNode.Primitive p)
            {
                if (p.Token.Type == TokenType.NumType)
                {
                    var target = context.NextTemp();
                    ir.Add(new IrUnary { Target = target, Op = "toNumber", Operand = val });
                    return new IrVar(target);
                }
                if (p.Token.Type == TokenType.StringType)
                {
                    var target = context.NextTemp();
                    ir.Add(new IrUnary { Target = target, Op = "toString", Operand = val });
                    return new IrVar(target);
                }
            }
            return val;
        }
    }

    public class ToNumberExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.ToNumberExpr);
        public IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context)
        {
            var toNumber = (Expr.ToNumberExpr)expr;
            var val = context.BuildExpr(toNumber.Expr, ir);
            var target = context.NextTemp();
            ir.Add(new IrUnary { Target = target, Op = "toNumber", Operand = val });
            return new IrVar(target);
        }
    }

    public class NameOfExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.NameOf);
        public IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context)
        {
            var nameOfExpr = (Expr.NameOf)expr;
            string name = nameOfExpr.Expr switch
            {
                Expr.Variable v => v.Name.Lexeme,
                Expr.Get g => g.Name.Lexeme,
                _ => "unknown"
            };
            return new IrConst(name);
        }
    }

    public class ToStringExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.ToStringExpr);
        public IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context)
        {
            var toStr = (Expr.ToStringExpr)expr;
            var val = context.BuildExpr(toStr.Expr, ir);
            var target = context.NextTemp();
            ir.Add(new IrUnary { Target = target, Op = "toString", Operand = val });
            return new IrVar(target);
        }
    }

    public class TypeOfExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.TypeOf);
        public IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context)
        {
            var typeOf = (Expr.TypeOf)expr;
            var val = context.BuildExpr(typeOf.Expr, ir);
            var target = context.NextTemp();
            ir.Add(new IrUnary { Target = target, Op = "typeof", Operand = val });
            return new IrVar(target);
        }
    }

    public class IsNullExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.IsNullExpr);
        public IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context)
        {
            var isNull = (Expr.IsNullExpr)expr;
            var val = context.BuildExpr(isNull.Expr, ir);
            var target = context.NextTemp();
            ir.Add(new IrUnary { Target = target, Op = "isNull", Operand = val });
            return new IrVar(target);
        }
    }

    public class AssertExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.AssertExpr);
        public IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context)
        {
            var assert = (Expr.AssertExpr)expr;
            var cond = context.BuildExpr(assert.Condition, ir);
            var msg = assert.Message != null ? context.BuildExpr(assert.Message, ir) : new IrConst("Assertion failed");
            ir.Add(new IrAssert { Condition = cond, Message = msg });
            return new IrConst(null);
        }
    }

    public class PanicExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.PanicExpr);
        public IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context)
        {
            var panic = (Expr.PanicExpr)expr;
            var msg = context.BuildExpr(panic.Message, ir);
            ir.Add(new IrPanic { Message = msg });
            return new IrConst(null);
        }
    }

    public class GroupingExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.Grouping);
        public IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context)
        {
            var groupExpr = (Expr.Grouping)expr;
            return context.BuildExpr(groupExpr.Expr, ir);
        }
    }
}
