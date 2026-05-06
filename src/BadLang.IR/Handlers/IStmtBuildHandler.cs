using System;
using System.Collections.Generic;
using BadLang.Parser;
using BadLang.Parser.Ast;

namespace BadLang.IR.Handlers
{
    public interface IStmtBuildHandler
    {
        Type TargetType { get; }
        void Build(Stmt stmt, List<IrNode> ir, IIRBuilderContext context);
    }
}
