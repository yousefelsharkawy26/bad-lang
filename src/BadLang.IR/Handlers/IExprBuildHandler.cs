using System;
using System.Collections.Generic;
using BadLang.Parser;
using BadLang.Parser.Ast;

namespace BadLang.IR.Handlers
{
    public interface IExprBuildHandler
    {
        Type TargetType { get; }
        IrValue Build(Expr expr, List<IrNode> ir, IIRBuilderContext context);
    }
}
