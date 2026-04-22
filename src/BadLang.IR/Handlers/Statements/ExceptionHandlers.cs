using System;
using System.Collections.Generic;
using BadLang.Parser;

namespace BadLang.IR.Handlers.Statements
{
    public class TryCatchStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.TryCatch);
        public void Build(Stmt stmt, List<IRNode> ir, IIRBuilderContext context)
        {
            var tcStmt = (Stmt.TryCatch)stmt;
            var catchLabel = context.NextLabel("CATCH");
            var finallyLabel = tcStmt.FinallyBlock != null ? context.NextLabel("FINALLY") : null;
            var endLabel = context.NextLabel("TRY_END");

            ir.Add(new IRTry { CatchLabel = catchLabel, FinallyLabel = finallyLabel });
            context.BuildStmt(new Stmt.Block(tcStmt.TryBlock), ir);
            ir.Add(new IRPopTry());
            ir.Add(new IRJump { TargetLabel = finallyLabel ?? endLabel });

            ir.Add(new IRLabel { Name = catchLabel });
            if (tcStmt.CatchClauses.Count > 0)
            {
                var clause = tcStmt.CatchClauses[0];
                if (clause.ExceptionName != null) {
                    ir.Add(new IRDefine { VariableName = clause.ExceptionName.Lexeme, Value = new IRVar("__exception") });
                }
                context.BuildStmt(new Stmt.Block(clause.Body), ir);
            }
            ir.Add(new IRJump { TargetLabel = finallyLabel ?? endLabel });

            if (finallyLabel != null)
            {
                ir.Add(new IRLabel { Name = finallyLabel });
                context.BuildStmt(new Stmt.Block(tcStmt.FinallyBlock!), ir);
            }
            ir.Add(new IRLabel { Name = endLabel });
        }
    }

    public class ThrowStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.Throw);
        public void Build(Stmt stmt, List<IRNode> ir, IIRBuilderContext context)
        {
            var throwStmt = (Stmt.Throw)stmt;
            var exVal = context.BuildExpr(throwStmt.Value, ir);
            ir.Add(new IRThrow { Exception = exVal });
        }
    }
}
