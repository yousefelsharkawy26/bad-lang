using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using BadLang.Parser.Ast;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.StatementHandlers;

public class SwitchStatementHandler(
    CompilationSession session,
    IStatementCompiler statementCompiler,
    IExpressionCompiler expressionCompiler)
    : StatementHandler(session, statementCompiler, expressionCompiler)
{
    public override bool CanHandle(Stmt stmt) => stmt is Stmt.Switch;

    public override void Compile(Stmt stmt)
    {
        var switchStmt = (Stmt.Switch)stmt;
        var builder = Session.Infrastructure.Builder;
        var module = Session.Infrastructure.Module;

        var switchVal = ExpressionCompiler.Compile(switchStmt.Expr);
        var switchType = Session.LastExpressionType;

        var func = builder.InsertBlock.Parent;
        var mergeBb = func.AppendBasicBlock("switch_merge");

        Session.BreakStack.Push(mergeBb);

        foreach (var @case in switchStmt.Cases)
        {
            var nextBb = func.AppendBasicBlock("switch_next");
            var bodyBb = func.AppendBasicBlock("switch_body");

            var caseVal = ExpressionCompiler.Compile(@case.Condition!);
            LLVMValueRef isEqual;
            if (switchType == "string")
            {
                var rtFn = module.GetNamedFunction("badlang_str_eq");
                isEqual = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_str_eq"), rtFn, new[] { Session.ToPtr(switchVal, Session.StringPtrType), Session.ToPtr(caseVal, Session.StringPtrType) }, "str_eq");
            }
            else
            {
                isEqual = builder.BuildFCmp(LLVMRealPredicate.LLVMRealOEQ, Session.ToDouble(switchVal), Session.ToDouble(caseVal), "case_cmp");
            }

            builder.BuildCondBr(isEqual, bodyBb, nextBb);

            builder.PositionAtEnd(bodyBb);
            foreach (var s in @case.Body) StatementCompiler.Compile(s);
            if (builder.InsertBlock.Terminator.Handle == IntPtr.Zero) 
                builder.BuildBr(mergeBb);

            builder.PositionAtEnd(nextBb);
        }

        if (switchStmt.DefaultBranch != null)
        {
            foreach (var s in switchStmt.DefaultBranch) StatementCompiler.Compile(s);
        }
        
        if (builder.InsertBlock.Terminator.Handle == IntPtr.Zero) 
            builder.BuildBr(mergeBb);

        builder.PositionAtEnd(mergeBb);
        Session.BreakStack.Pop();
    }
}
