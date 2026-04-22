using BadLang.Backend.LLVM.Core;
using BadLang.Backend.LLVM.Infrastructure;
using BadLang.Core;
using BadLang.Parser;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class AssignHandler : ExpressionHandler
{
    public AssignHandler(CompilationSession session, IExpressionCompiler expressionCompiler) 
        : base(session, expressionCompiler) { }

    public override bool CanHandle(Expr expr) => expr is Expr.Assign;

    public override LLVMValueRef Compile(Expr expr)
    {
        var assignExpr = (Expr.Assign)expr;
        var val = ExpressionCompiler.Compile(assignExpr.Value);
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;

        LLVMValueRef? targetPtr = null;

        if (assignExpr.Target is Expr.Variable vTarget)
        {
            targetPtr = Session.Symbols.GetVariable(vTarget.Name.Lexeme).Value;
        }
        else if (assignExpr.Target is Expr.Index indexTarget)
        {
            var targetObj = ExpressionCompiler.Compile(indexTarget.Target);
            var targetType = Session.LastExpressionType;
            var indexValue = ExpressionCompiler.Compile(indexTarget.IndexValue);

            if (targetType == "list")
            {
                if (assignExpr.Operator.Type != TokenType.Equal)
                {
                    var getFn = Session.Infrastructure.Module.GetNamedFunction("badlang_list_get");
                    var current = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_list_get"), getFn, new[] { Session.ToPtr(targetObj, Session.VoidPtrType), Session.NumberToInt(indexValue) }, "cur_l");
                    var left = Session.ToDouble(current);
                    var right = Session.ToDouble(val);
                    val = Session.ToI64(assignExpr.Operator.Type switch {
                        TokenType.PlusEqual => builder.BuildFAdd(left, right, "add"),
                        TokenType.MinusEqual => builder.BuildFSub(left, right, "sub"),
                        TokenType.StarEqual => builder.BuildFMul(left, right, "mul"),
                        TokenType.SlashEqual => builder.BuildFDiv(left, right, "div"),
                        _ => throw new System.Exception("Unsupported compound assignment")
                    });
                }

                var setFn = Session.Infrastructure.Module.GetNamedFunction("badlang_list_set");
                builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_list_set"), setFn, new[] { Session.ToPtr(targetObj, Session.VoidPtrType), Session.NumberToInt(indexValue), val }, "");
                return val;
            }
            // Add other indexable types if necessary
        }

        if (targetPtr == null) throw new System.Exception("Invalid assignment target");

        if (assignExpr.Operator.Type != TokenType.Equal)
        {
            var left = Session.ToDouble(builder.BuildLoad2(ctx.DoubleType, targetPtr.Value, "curval"));
            var right = Session.ToDouble(val);
            val = Session.ToI64(assignExpr.Operator.Type switch {
                TokenType.PlusEqual => builder.BuildFAdd(left, right, "add"),
                TokenType.MinusEqual => builder.BuildFSub(left, right, "sub"),
                TokenType.StarEqual => builder.BuildFMul(left, right, "mul"),
                TokenType.SlashEqual => builder.BuildFDiv(left, right, "div"),
                _ => throw new System.Exception("Unsupported compound assignment")
            });
        }

        builder.BuildStore(val, targetPtr.Value);
        return val;
    }
}
