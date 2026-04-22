using System;
using System.Collections.Generic;
using BadLang.Parser;

namespace BadLang.IR.Handlers.Statements
{
    public class IfStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.If);
        public void Build(Stmt stmt, List<IRNode> ir, IIRBuilderContext context)
        {
            var ifStmt = (Stmt.If)stmt;
            var cond = context.BuildExpr(ifStmt.Condition, ir);
            var trueLabel = context.NextLabel("IF_TRUE");
            var falseLabel = context.NextLabel("IF_FALSE");
            var endLabel = context.NextLabel("IF_END");

            ir.Add(new IRCondJump { Condition = cond, TrueLabel = trueLabel, FalseLabel = falseLabel });
            ir.Add(new IRLabel { Name = trueLabel });
            context.BuildStmt(ifStmt.ThenBranch, ir);
            ir.Add(new IRJump { TargetLabel = endLabel });
            ir.Add(new IRLabel { Name = falseLabel });
            if (ifStmt.ElseBranch != null)
                context.BuildStmt(ifStmt.ElseBranch, ir);
            ir.Add(new IRLabel { Name = endLabel });
        }
    }

    public class SwitchStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.Switch);
        public void Build(Stmt stmt, List<IRNode> ir, IIRBuilderContext context)
        {
            var switchStmt = (Stmt.Switch)stmt;
            var switchVal = context.BuildExpr(switchStmt.Expr, ir);
            var endLabel = context.NextLabel("SWITCH_END");
            var defaultLabel = switchStmt.DefaultBranch != null ? context.NextLabel("SWITCH_DEFAULT") : endLabel;

            List<string> caseLabels = new List<string>();
            foreach (var c in switchStmt.Cases)
            {
                var caseLabel = context.NextLabel("SWITCH_CASE");
                caseLabels.Add(caseLabel);
                
                var conditionVal = context.BuildExpr(c.Condition!, ir);
                var isEqual = context.NextTemp();
                ir.Add(new IRBinary { Target = isEqual, Op = "==", Left = switchVal, Right = conditionVal });
                
                var nextCondLabel = context.NextLabel("SWITCH_NEXT");
                ir.Add(new IRCondJump { Condition = new IRVar(isEqual), TrueLabel = caseLabel, FalseLabel = nextCondLabel });
                ir.Add(new IRLabel { Name = nextCondLabel });
            }

            ir.Add(new IRJump { TargetLabel = defaultLabel });

            for (int i = 0; i < switchStmt.Cases.Count; i++)
            {
                ir.Add(new IRLabel { Name = caseLabels[i] });
                context.BuildStmt(new Stmt.Block(switchStmt.Cases[i].Body), ir);
                ir.Add(new IRJump { TargetLabel = endLabel });
            }

            if (switchStmt.DefaultBranch != null)
            {
                ir.Add(new IRLabel { Name = defaultLabel });
                context.BuildStmt(new Stmt.Block(switchStmt.DefaultBranch), ir);
                ir.Add(new IRJump { TargetLabel = endLabel });
            }

            ir.Add(new IRLabel { Name = endLabel });
        }
    }
}
