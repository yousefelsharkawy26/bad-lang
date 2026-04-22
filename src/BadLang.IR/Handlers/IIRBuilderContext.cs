using System.Collections.Generic;
using BadLang.Parser;

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

        void BuildStmt(Stmt stmt, List<IRNode> ir);
        IRValue BuildExpr(Expr expr, List<IRNode> ir);
        
        string? GetExportName(Stmt stmt);
    }
}
