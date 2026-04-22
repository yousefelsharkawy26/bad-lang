using System;
using System.Collections.Generic;
using BadLang.Parser;
using BadLang.Core;

namespace BadLang.IR.Handlers.Expressions
{
    public class TypeCastExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.TypeCast);
        public IRValue Build(Expr expr, List<IRNode> ir, IIRBuilderContext context)
        {
            var castExpr = (Expr.TypeCast)expr;
            var val = context.BuildExpr(castExpr.Expr, ir);
            if (castExpr.TargetType is TypeNode.Primitive p)
            {
                if (p.Token.Type == TokenType.NumType)
                {
                    var target = context.NextTemp();
                    ir.Add(new IRUnary { Target = target, Op = "toNumber", Operand = val });
                    return new IRVar(target);
                }
                if (p.Token.Type == TokenType.StringType)
                {
                    var target = context.NextTemp();
                    ir.Add(new IRUnary { Target = target, Op = "toString", Operand = val });
                    return new IRVar(target);
                }
            }
            return val;
        }
    }

    public class ToNumberExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.ToNumberExpr);
        public IRValue Build(Expr expr, List<IRNode> ir, IIRBuilderContext context)
        {
            var toNumber = (Expr.ToNumberExpr)expr;
            var val = context.BuildExpr(toNumber.Expr, ir);
            var target = context.NextTemp();
            ir.Add(new IRUnary { Target = target, Op = "toNumber", Operand = val });
            return new IRVar(target);
        }
    }

    public class NameOfExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.NameOf);
        public IRValue Build(Expr expr, List<IRNode> ir, IIRBuilderContext context)
        {
            var nameOfExpr = (Expr.NameOf)expr;
            string name = nameOfExpr.Expr switch
            {
                Expr.Variable v => v.Name.Lexeme,
                Expr.Get g => g.Name.Lexeme,
                _ => "unknown"
            };
            return new IRConst(name);
        }
    }

    public class ToStringExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.ToStringExpr);
        public IRValue Build(Expr expr, List<IRNode> ir, IIRBuilderContext context)
        {
            var toStr = (Expr.ToStringExpr)expr;
            var val = context.BuildExpr(toStr.Expr, ir);
            var target = context.NextTemp();
            ir.Add(new IRUnary { Target = target, Op = "toString", Operand = val });
            return new IRVar(target);
        }
    }

    public class TypeOfExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.TypeOf);
        public IRValue Build(Expr expr, List<IRNode> ir, IIRBuilderContext context)
        {
            var typeOf = (Expr.TypeOf)expr;
            var val = context.BuildExpr(typeOf.Expr, ir);
            var target = context.NextTemp();
            ir.Add(new IRUnary { Target = target, Op = "typeof", Operand = val });
            return new IRVar(target);
        }
    }

    public class IsNullExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.IsNullExpr);
        public IRValue Build(Expr expr, List<IRNode> ir, IIRBuilderContext context)
        {
            var isNull = (Expr.IsNullExpr)expr;
            var val = context.BuildExpr(isNull.Expr, ir);
            var target = context.NextTemp();
            ir.Add(new IRUnary { Target = target, Op = "isNull", Operand = val });
            return new IRVar(target);
        }
    }

    public class AssertExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.AssertExpr);
        public IRValue Build(Expr expr, List<IRNode> ir, IIRBuilderContext context)
        {
            var assert = (Expr.AssertExpr)expr;
            var cond = context.BuildExpr(assert.Condition, ir);
            var msg = assert.Message != null ? context.BuildExpr(assert.Message, ir) : new IRConst("Assertion failed");
            ir.Add(new IRAssert { Condition = cond, Message = msg });
            return new IRConst(null);
        }
    }

    public class PanicExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.PanicExpr);
        public IRValue Build(Expr expr, List<IRNode> ir, IIRBuilderContext context)
        {
            var panic = (Expr.PanicExpr)expr;
            var msg = context.BuildExpr(panic.Message, ir);
            ir.Add(new IRPanic { Message = msg });
            return new IRConst(null);
        }
    }

    public class GroupingExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.Grouping);
        public IRValue Build(Expr expr, List<IRNode> ir, IIRBuilderContext context)
        {
            var groupExpr = (Expr.Grouping)expr;
            return context.BuildExpr(groupExpr.Expr, ir);
        }
    }
}
