using System;
using System.Collections.Generic;
using BadLang.Parser;
using BadLang.Parser.Ast;

namespace BadLang.IR.Handlers.Statements
{
    public class ExpressionStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.Expression);
        public void Build(Stmt stmt, List<IrNode> ir, IIRBuilderContext context)
        {
            var exprStmt = (Stmt.Expression)stmt;
            context.BuildExpr(exprStmt.Expr, ir);
        }
    }

    public class BlockStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.Block);
        public void Build(Stmt stmt, List<IrNode> ir, IIRBuilderContext context)
        {
            var blockStmt = (Stmt.Block)stmt;
            ir.Add(new IrEnterScope());
            foreach (var s in blockStmt.Statements)
            {
                context.BuildStmt(s, ir);
            }
            ir.Add(new IrExitScope());
        }
    }

    public class ReturnStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.Return);
        public void Build(Stmt stmt, List<IrNode> ir, IIRBuilderContext context)
        {
            var retStmt = (Stmt.Return)stmt;
            IrValue? retVal = retStmt.Value != null ? context.BuildExpr(retStmt.Value, ir) : null;
            ir.Add(new IrReturn { Value = retVal });
        }
    }
}
