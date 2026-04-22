using System;
using System.Collections.Generic;
using BadLang.Parser;

namespace BadLang.IR.Handlers
{
    public interface IStmtBuildHandler
    {
        Type TargetType { get; }
        void Build(Stmt stmt, List<IRNode> ir, IIRBuilderContext context);
    }
}
