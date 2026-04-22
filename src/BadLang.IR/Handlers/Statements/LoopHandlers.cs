using System;
using System.Collections.Generic;
using BadLang.Parser;

namespace BadLang.IR.Handlers.Statements
{
    public class WhileStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.While);
        public void Build(Stmt stmt, List<IRNode> ir, IIRBuilderContext context)
        {
            var whileStmt = (Stmt.While)stmt;
            var startLabel = context.NextLabel("WHILE_START");
            var bodyLabel = context.NextLabel("WHILE_BODY");
            var endLabel = context.NextLabel("WHILE_END");

            context.PushLoop(startLabel, endLabel);

            ir.Add(new IRLabel { Name = startLabel });
            var cond = context.BuildExpr(whileStmt.Condition, ir);
            ir.Add(new IRCondJump { Condition = cond, TrueLabel = bodyLabel, FalseLabel = endLabel });
            ir.Add(new IRLabel { Name = bodyLabel });
            context.BuildStmt(whileStmt.Body, ir);
            ir.Add(new IRJump { TargetLabel = startLabel });
            ir.Add(new IRLabel { Name = endLabel });

            context.PopLoop();
        }
    }

    public class DoWhileStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.DoWhile);
        public void Build(Stmt stmt, List<IRNode> ir, IIRBuilderContext context)
        {
            var dwStmt = (Stmt.DoWhile)stmt;
            var startLabel = context.NextLabel("DOWHILE_START");
            var condLabel = context.NextLabel("DOWHILE_COND");
            var endLabel = context.NextLabel("DOWHILE_END");

            context.PushLoop(condLabel, endLabel);

            ir.Add(new IRLabel { Name = startLabel });
            context.BuildStmt(dwStmt.Body, ir);
            ir.Add(new IRLabel { Name = condLabel });
            var cond = context.BuildExpr(dwStmt.Condition, ir);
            ir.Add(new IRCondJump { Condition = cond, TrueLabel = startLabel, FalseLabel = endLabel });
            ir.Add(new IRLabel { Name = endLabel });

            context.PopLoop();
        }
    }

    public class ForInStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.ForIn);
        public void Build(Stmt stmt, List<IRNode> ir, IIRBuilderContext context)
        {
            var forInStmt = (Stmt.ForIn)stmt;
            var iterable = context.BuildExpr(forInStmt.Iterable, ir);
            var enumeratorTarget = context.NextTemp();
            ir.Add(new IRUnary { Target = enumeratorTarget, Op = "get_enumerator", Operand = iterable });
            var enumeratorVar = new IRVar(enumeratorTarget);

            var startLabel = context.NextLabel("FORIN_START");
            var endLabel = context.NextLabel("FORIN_END");
            context.PushLoop(startLabel, endLabel);

            ir.Add(new IRLabel { Name = startLabel });
            var hasNextTarget = context.NextTemp();
            ir.Add(new IRUnary { Target = hasNextTarget, Op = "enumerator_next", Operand = enumeratorVar });
            
            var bodyLabel = context.NextLabel("FORIN_BODY");
            ir.Add(new IRCondJump { Condition = new IRVar(hasNextTarget), TrueLabel = bodyLabel, FalseLabel = endLabel });

            ir.Add(new IRLabel { Name = bodyLabel });
            var itemTarget = context.NextTemp();
            ir.Add(new IRUnary { Target = itemTarget, Op = "enumerator_current", Operand = enumeratorVar });
            
            ir.Add(new IREnterScope());
            ir.Add(new IRDefine { VariableName = forInStmt.Variable.Lexeme, Value = new IRVar(itemTarget) });
            context.BuildStmt(forInStmt.Body, ir);
            ir.Add(new IRExitScope());

            ir.Add(new IRJump { TargetLabel = startLabel });
            ir.Add(new IRLabel { Name = endLabel });

            context.PopLoop();
        }
    }

    public class BreakStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.Break);
        public void Build(Stmt stmt, List<IRNode> ir, IIRBuilderContext context)
        {
            if (context.HasLoop)
                ir.Add(new IRJump { TargetLabel = context.PeekLoop().EndLabel });
        }
    }

    public class ContinueStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.Continue);
        public void Build(Stmt stmt, List<IRNode> ir, IIRBuilderContext context)
        {
            if (context.HasLoop)
                ir.Add(new IRJump { TargetLabel = context.PeekLoop().StartLabel });
        }
    }
}
