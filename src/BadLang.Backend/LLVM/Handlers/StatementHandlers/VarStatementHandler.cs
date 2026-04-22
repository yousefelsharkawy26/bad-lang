using BadLang.Backend.LLVM.Core;
using BadLang.Backend.LLVM.Infrastructure;
using BadLang.Core;
using BadLang.Parser;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.StatementHandlers;

public class VarStatementHandler : StatementHandler
{
    public VarStatementHandler(CompilationSession session, IStatementCompiler statementCompiler, IExpressionCompiler expressionCompiler) 
        : base(session, statementCompiler, expressionCompiler) { }

    public override bool CanHandle(Stmt stmt) => stmt is Stmt.Var;

    public override void Compile(Stmt stmt)
    {
        var varStmt = (Stmt.Var)stmt;
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;

        LLVMValueRef initialVal;
        if (varStmt.Initializer != null)
        {
            initialVal = ExpressionCompiler.Compile(varStmt.Initializer);
        }
        else
        {
            initialVal = Session.ToI64(LLVMValueRef.CreateConstReal(ctx.DoubleType, 0.0));
        }

        var ptr = builder.BuildAlloca(ctx.Int64Type, varStmt.Name.Lexeme);
        builder.BuildStore(initialVal, ptr);
        
        string typeName = "any";
        if (varStmt.Type is TypeNode.UserDefined userType)
        {
            typeName = userType.Name.Lexeme;
        }
        else if (varStmt.Type is TypeNode.Primitive primitiveType)
        {
            typeName = primitiveType.Token.Type == TokenType.StringType ? "string" : (primitiveType.Token.Type == TokenType.NumType ? "number" : "any");
        }
        else if (varStmt.Type == null && varStmt.Initializer != null)
        {
            // Infer type
            if (varStmt.Initializer is Expr.New newExpr)
            {
                // This helper might need to be in Session or a separate utility
                if (newExpr.Callee is Expr.Variable v) typeName = v.Name.Lexeme;
            }
            else if (Session.LastExpressionType != null)
            {
                typeName = Session.LastExpressionType;
            }
        }

        Session.Symbols.DefineVariable(varStmt.Name.Lexeme, ptr, typeName);
    }
}
