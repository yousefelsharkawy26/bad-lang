using System.Collections.Generic;
using BadLang.Parser;
using BadLang.Parser.Ast;

namespace BadLang.IR.Handlers
{
    public interface IIRBuilderContext
    {
        string NextTemp();
        string NextLabel(string prefix = "L");
        void PushLoop(string startLabel, string endLabel);
        void PopLoop();
        (string StartLabel, string EndLabel) PeekLoop();
        bool HasLoop { get; }

        void BuildStmt(Stmt stmt, List<IrNode> ir);
        IrValue BuildExpr(Expr expr, List<IrNode> ir);
        
        string? GetExportName(Stmt stmt);
    }
}
