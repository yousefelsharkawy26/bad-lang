using System;
using System.Collections.Generic;
using BadLang.Parser;

namespace BadLang.IR.Handlers.Expressions
{
    public class LiteralExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.Literal);
        public IRValue Build(Expr expr, List<IRNode> ir, IIRBuilderContext context)
        {
            var lit = (Expr.Literal)expr;
            return new IRConst(lit.Value);
        }
    }

    public class ArrayLiteralExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.ArrayLiteral);
        public IRValue Build(Expr expr, List<IRNode> ir, IIRBuilderContext context)
        {
            var arrExpr = (Expr.ArrayLiteral)expr;
            var elements = new List<IRValue>();
            foreach (var el in arrExpr.Elements)
                elements.Add(context.BuildExpr(el, ir));
            var target = context.NextTemp();
            ir.Add(new IRNewArray { Target = target, Elements = elements });
            return new IRVar(target);
        }
    }

    public class MapLiteralExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.MapLiteral);
        public IRValue Build(Expr expr, List<IRNode> ir, IIRBuilderContext context)
        {
            var mapExpr = (Expr.MapLiteral)expr;
            var target = context.NextTemp();
            ir.Add(new IRNewMap { Target = target });
            var mapVar = new IRVar(target);
            foreach (var entry in mapExpr.Entries)
            {
                var key = context.BuildExpr(entry.Key, ir);
                var val = context.BuildExpr(entry.Value, ir);
                ir.Add(new IRIndexSet { ArrayOrMap = mapVar, Index = key, Value = val });
            }
            return mapVar;
        }
    }

    public class InterpolatedStringExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.InterpolatedString);
        public IRValue Build(Expr expr, List<IRNode> ir, IIRBuilderContext context)
        {
            var intStr = (Expr.InterpolatedString)expr;
            if (intStr.Parts.Count == 0) return new IRConst("");
            var result = context.BuildExpr(intStr.Parts[0], ir);
            for (int i = 1; i < intStr.Parts.Count; i++)
            {
                var part = context.BuildExpr(intStr.Parts[i], ir);
                var target = context.NextTemp();
                ir.Add(new IRBinary { Target = target, Op = "+", Left = result, Right = part });
                result = new IRVar(target);
            }
            return result;
        }
    }
}
