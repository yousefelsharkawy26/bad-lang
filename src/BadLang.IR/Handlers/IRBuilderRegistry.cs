using System;
using System.Collections.Generic;
using BadLang.Parser;
using BadLang.IR.Handlers.Statements;
using BadLang.IR.Handlers.Expressions;

namespace BadLang.IR.Handlers
{
    public class IRBuilderRegistry
    {
        public Dictionary<Type, IStmtBuildHandler> StmtHandlers { get; } = new();
        public Dictionary<Type, IExprBuildHandler> ExprHandlers { get; } = new();

        public IRBuilderRegistry()
        {
            RegisterDefaultHandlers();
        }

        private void RegisterDefaultHandlers()
        {
            // Statements
            Register(new ExpressionStmtHandler());
            Register(new BlockStmtHandler());
            Register(new ReturnStmtHandler());
            Register(new VarStmtHandler());
            Register(new ConstStmtHandler());
            Register(new FunctionStmtHandler());
            Register(new ClassStmtHandler());
            Register(new InterfaceStmtHandler());
            Register(new StructStmtHandler());
            Register(new EnumStmtHandler());
            Register(new IfStmtHandler());
            Register(new SwitchStmtHandler());
            Register(new WhileStmtHandler());
            Register(new DoWhileStmtHandler());
            Register(new ForInStmtHandler());
            Register(new BreakStmtHandler());
            Register(new ContinueStmtHandler());
            Register(new TryCatchStmtHandler());
            Register(new ThrowStmtHandler());
            Register(new ExportStmtHandler());
            Register(new ExportListStmtHandler());
            Register(new ImportStmtHandler());

            // Expressions
            Register(new LiteralExprHandler());
            Register(new ArrayLiteralExprHandler());
            Register(new MapLiteralExprHandler());
            Register(new InterpolatedStringExprHandler());
            Register(new VariableExprHandler());
            Register(new AssignExprHandler());
            Register(new GetExprHandler());
            Register(new SetExprHandler());
            Register(new IndexExprHandler());
            Register(new ThisExprHandler());
            Register(new SuperExprHandler());
            Register(new BinaryExprHandler());
            Register(new UnaryExprHandler());
            Register(new LogicalExprHandler());
            Register(new TernaryExprHandler());
            Register(new NullCoalesceExprHandler());
            Register(new CallExprHandler());
            Register(new NewExprHandler());
            Register(new LambdaExprHandler());
            Register(new TypeCastExprHandler());
            Register(new ToNumberExprHandler());
            Register(new NameOfExprHandler());
            Register(new ToStringExprHandler());
            Register(new TypeOfExprHandler());
            Register(new IsNullExprHandler());
            Register(new AssertExprHandler());
            Register(new PanicExprHandler());
            Register(new GroupingExprHandler());
        }

        public void Register(IStmtBuildHandler handler)
        {
            StmtHandlers[handler.TargetType] = handler;
        }

        public void Register(IExprBuildHandler handler)
        {
            ExprHandlers[handler.TargetType] = handler;
        }
    }
}
