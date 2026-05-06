using System;
using System.Collections.Generic;
using BadLang.Parser;
using BadLang.Parser.Ast;

namespace BadLang.IR.Handlers.Statements
{
    public class TryCatchStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.TryCatch);
        public void Build(Stmt stmt, List<IrNode> ir, IIRBuilderContext context)
        {
            var tcStmt = (Stmt.TryCatch)stmt;
            var catchLabel = context.NextLabel("CATCH");
            var finallyLabel = tcStmt.FinallyBlock != null ? context.NextLabel("FINALLY") : null;
            var endLabel = context.NextLabel("TRY_END");

            ir.Add(new IrTry { CatchLabel = catchLabel, FinallyLabel = finallyLabel });
            context.BuildStmt(new Stmt.Block(tcStmt.TryBlock), ir);
            ir.Add(new IrPopTry());
            ir.Add(new IrJump { TargetLabel = finallyLabel ?? endLabel });

            ir.Add(new IrLabel { Name = catchLabel });
            if (tcStmt.CatchClauses.Count > 0)
            {
                var clause = tcStmt.CatchClauses[0];
                if (clause.ExceptionName != null) {
                    ir.Add(new IrDefine { VariableName = clause.ExceptionName.Lexeme, Value = new IrVar("__exception") });
                }
                context.BuildStmt(new Stmt.Block(clause.Body), ir);
            }
            ir.Add(new IrJump { TargetLabel = finallyLabel ?? endLabel });

            if (finallyLabel != null)
            {
                ir.Add(new IrLabel { Name = finallyLabel });
                context.BuildStmt(new Stmt.Block(tcStmt.FinallyBlock!), ir);
            }
            ir.Add(new IrLabel { Name = endLabel });
        }
    }

    public class ThrowStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.Throw);
        public void Build(Stmt stmt, List<IrNode> ir, IIRBuilderContext context)
        {
            var throwStmt = (Stmt.Throw)stmt;
            var exVal = context.BuildExpr(throwStmt.Value, ir);
            ir.Add(new IrThrow { Exception = exVal });
        }
    }
}
