using System;
using System.Collections.Generic;
using BadLang.Parser;
using BadLang.Parser.Ast;

namespace BadLang.IR.Handlers.Expressions
{
    public class VariableExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.Variable);
        public IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context)
        {
            var varExpr = (Expr.Variable)expr;
            var loadTarget = context.NextTemp();
            ir.Add(new IrLoad { Target = loadTarget, VariableName = varExpr.Name.Lexeme });
            return new IrVar(loadTarget);
        }
    }

    public class AssignExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.Assign);
        public IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context)
        {
            var assignExpr = (Expr.Assign)expr;
            var val = context.BuildExpr(assignExpr.Value, ir);
            if (assignExpr.Target is Expr.Variable targetVar)
            {
                ir.Add(new IrStore { VariableName = targetVar.Name.Lexeme, Value = val });
            }
            else if (assignExpr.Target is Expr.Get getExpr)
            {
                var obj = context.BuildExpr(getExpr.Object, ir);
                ir.Add(new IrPropertySet { Object = obj, Property = getExpr.Name.Lexeme, Value = val });
            }
            else if (assignExpr.Target is Expr.Index indexExpr)
            {
                var arr = context.BuildExpr(indexExpr.Target, ir);
                var idx = context.BuildExpr(indexExpr.IndexValue, ir);
                ir.Add(new IrIndexSet { ArrayOrMap = arr, Index = idx, Value = val });
            }
            return val;
        }
    }

    public class GetExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.Get);
        public IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context)
        {
            var getExpr = (Expr.Get)expr;
            var obj = context.BuildExpr(getExpr.Object, ir);
            var target = context.NextTemp();
            ir.Add(new IrPropertyGet { Target = target, Object = obj, Property = getExpr.Name.Lexeme });
            return new IrVar(target);
        }
    }

    public class SetExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.Set);
        public IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context)
        {
            var setExpr = (Expr.Set)expr;
            var obj = context.BuildExpr(setExpr.Object, ir);
            var val = context.BuildExpr(setExpr.Value, ir);
            ir.Add(new IrPropertySet { Object = obj, Property = setExpr.Name.Lexeme, Value = val });
            return val;
        }
    }

    public class IndexExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.Index);
        public IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context)
        {
            var indexExpr = (Expr.Index)expr;
            var arr = context.BuildExpr(indexExpr.Target, ir);
            var idx = context.BuildExpr(indexExpr.IndexValue, ir);
            var target = context.NextTemp();
            ir.Add(new IrIndexGet { Target = target, ArrayOrMap = arr, Index = idx });
            return new IrVar(target);
        }
    }

    public class ThisExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.This);
        public IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context)
        {
            var thisTarget = context.NextTemp();
            ir.Add(new IrLoad { Target = thisTarget, VariableName = "this" });
            return new IrVar(thisTarget);
        }
    }

    public class SuperExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.Super);
        public IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context)
        {
            var superExpr = (Expr.Super)expr;
            var target = context.NextTemp();
            ir.Add(new IrSuperPropertyGet { Target = target, MethodName = superExpr.Method.Lexeme });
            return new IrVar(target);
        }
    }
}
