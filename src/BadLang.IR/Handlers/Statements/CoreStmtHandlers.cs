using System;
using System.Collections.Generic;
using BadLang.Parser;

namespace BadLang.IR.Handlers.Statements
{
    public class ExpressionStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.Expression);
        public void Build(Stmt stmt, List<IRNode> ir, IIRBuilderContext context)
        {
            var exprStmt = (Stmt.Expression)stmt;
            context.BuildExpr(exprStmt.Expr, ir);
        }
    }

    public class BlockStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.Block);
        public void Build(Stmt stmt, List<IRNode> ir, IIRBuilderContext context)
        {
            var blockStmt = (Stmt.Block)stmt;
            ir.Add(new IREnterScope());
            foreach (var s in blockStmt.Statements)
            {
                context.BuildStmt(s, ir);
            }
            ir.Add(new IRExitScope());
        }
    }

    public class ReturnStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.Return);
        public void Build(Stmt stmt, List<IRNode> ir, IIRBuilderContext context)
        {
            var retStmt = (Stmt.Return)stmt;
            IRValue? retVal = retStmt.Value != null ? context.BuildExpr(retStmt.Value, ir) : null;
            ir.Add(new IRReturn { Value = retVal });
        }
    }
}
