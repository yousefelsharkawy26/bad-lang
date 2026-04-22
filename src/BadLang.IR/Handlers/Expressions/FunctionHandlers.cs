using System;
using System.Collections.Generic;
using System.Linq;
using BadLang.Parser;

namespace BadLang.IR.Handlers.Expressions
{
    public class CallExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.Call);
        public IRValue Build(Expr expr, List<IRNode> ir, IIRBuilderContext context)
        {
            var callExpr = (Expr.Call)expr;
            var args = new List<IRValue>();
            foreach (var arg in callExpr.Arguments)
                args.Add(context.BuildExpr(arg, ir));
            
            var target = context.NextTemp();

            if (callExpr.Callee is Expr.Get methodGet)
            {
                var obj = context.BuildExpr(methodGet.Object, ir);
                ir.Add(new IRMethodCall { Target = target, Object = obj, MethodName = methodGet.Name.Lexeme, Arguments = args });
            }
            else if (callExpr.Callee is Expr.Super superGet)
            {
                ir.Add(new IRSuperMethodCall { Target = target, MethodName = superGet.Method.Lexeme, Arguments = args });
            }
            else if (callExpr.Callee is Expr.Variable funcVar)
            {
                ir.Add(new IRCall { Target = target, FunctionName = funcVar.Name.Lexeme, Arguments = args });
            }
            else
            {
                var callee = context.BuildExpr(callExpr.Callee, ir);
                ir.Add(new IRCall { Target = target, FunctionName = callee.ToString() ?? "unknown", Arguments = args });
            }

            return new IRVar(target);
        }
    }

    public class NewExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.New);
        public IRValue Build(Expr expr, List<IRNode> ir, IIRBuilderContext context)
        {
            var newExpr = (Expr.New)expr;
            var args = new List<IRValue>();
            foreach (var arg in newExpr.Arguments)
                args.Add(context.BuildExpr(arg, ir));
            var target = context.NextTemp();
            var klass = context.BuildExpr(newExpr.Callee, ir);
            ir.Add(new IRNew { Target = target, Class = klass, Arguments = args });
            return new IRVar(target);
        }
    }

    public class LambdaExprHandler : IExprBuildHandler
    {
        public Type TargetType => typeof(Expr.Lambda);
        public IRValue Build(Expr expr, List<IRNode> ir, IIRBuilderContext context)
        {
            var lambdaExpr = (Expr.Lambda)expr;
            var lambdaTarget = context.NextTemp();
            var body = new List<IRNode>();
            
            if (lambdaExpr.ExpressionBody != null)
            {
                var retVal = context.BuildExpr(lambdaExpr.ExpressionBody, body);
                body.Add(new IRReturn { Value = retVal });
            }
            else
            {
                context.BuildStmt(new Stmt.Block(lambdaExpr.Body), body);
            }
            
            var lambdaDef = new IRLambda
            {
                Target = lambdaTarget,
                Parameters = lambdaExpr.Parameters.Select(p => p.Name.Lexeme).ToList(),
                Body = body
            };
            
            ir.Add(lambdaDef);
            return new IRVar(lambdaTarget);
        }
    }
}

