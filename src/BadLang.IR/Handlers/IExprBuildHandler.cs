using System;
using System.Collections.Generic;
using BadLang.Parser;

namespace BadLang.IR.Handlers
{
    public interface IExprBuildHandler
    {
        Type TargetType { get; }
        IRValue Build(Expr expr, List<IRNode> ir, IIRBuilderContext context);
    }
}
