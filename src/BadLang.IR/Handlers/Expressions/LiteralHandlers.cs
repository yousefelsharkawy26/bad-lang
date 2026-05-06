using System;
using System.Collections.Generic;
using BadLang.Parser;
using BadLang.Parser.Ast;

namespace BadLang.IR.Handlers.Expressions
{
    public class LiteralExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.Literal);
        public IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context)
        {
            var lit = (Expr.Literal)expr;
            return new IrConst(lit.Value);
        }
    }

    public class ArrayLiteralExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.ArrayLiteral);
        public IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context)
        {
            var arrExpr = (Expr.ArrayLiteral)expr;
            var elements = new List<IrValue>();
            foreach (var el in arrExpr.Elements)
                elements.Add(context.BuildExpr(el, ir));
            var target = context.NextTemp();
            ir.Add(new IrNewArray { Target = target, Elements = elements });
            return new IrVar(target);
        }
    }

    public class MapLiteralExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.MapLiteral);
        public IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context)
        {
            var mapExpr = (Expr.MapLiteral)expr;
            var target = context.NextTemp();
            ir.Add(new IrNewMap { Target = target });
            var mapVar = new IrVar(target);
            foreach (var entry in mapExpr.Entries)
            {
                var key = context.BuildExpr(entry.Key, ir);
                var val = context.BuildExpr(entry.Value, ir);
                ir.Add(new IrIndexSet { ArrayOrMap = mapVar, Index = key, Value = val });
            }
            return mapVar;
        }
    }

    public class InterpolatedStringExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.InterpolatedString);
        public IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context)
        {
            var intStr = (Expr.InterpolatedString)expr;
            if (intStr.Parts.Count == 0) return new IrConst("");
            var result = context.BuildExpr(intStr.Parts[0], ir);
            for (int i = 1; i < intStr.Parts.Count; i++)
            {
                var part = context.BuildExpr(intStr.Parts[i], ir);
                var target = context.NextTemp();
                ir.Add(new IrBinary { Target = target, Op = "+", Left = result, Right = part });
                result = new IrVar(target);
            }
            return result;
        }
    }
}
