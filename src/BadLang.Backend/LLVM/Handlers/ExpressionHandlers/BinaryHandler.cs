using BadLang.Backend.LLVM.Core;
using BadLang.Backend.LLVM.Infrastructure;
using BadLang.Parser;
using BadLang.Core;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class BinaryHandler : ExpressionHandler
{
    public BinaryHandler(CompilationSession session, IExpressionCompiler expressionCompiler) 
        : base(session, expressionCompiler) { }

    public override bool CanHandle(Expr expr) => expr is Expr.Binary;

    public override LLVMValueRef Compile(Expr expr)
    {
        var binary = (Expr.Binary)expr;
        
        var left = ExpressionCompiler.Compile(binary.Left);
        var leftType = Session.LastExpressionType;
        var right = ExpressionCompiler.Compile(binary.Right);
        var rightType = Session.LastExpressionType;

        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;
        var module = Session.Infrastructure.Module;

        // String concatenation
        if (binary.Operator.Type == TokenType.Plus && (leftType == "string" || rightType == "string"))
        {
            LLVMValueRef lVal = left;
            LLVMValueRef rVal = right;

            if (leftType == "number")
            {
                var numToStr = module.GetNamedFunction("badlang_num_to_str");
                var numToStrType = Session.Runtime.GetRuntimeType("badlang_num_to_str");
                lVal = Session.FromPtr(builder.BuildCall2(numToStrType, numToStr, new[] { Session.ToDouble(left) }, "lstr"));
            }
            if (rightType == "number")
            {
                var numToStr = module.GetNamedFunction("badlang_num_to_str");
                var numToStrType = Session.Runtime.GetRuntimeType("badlang_num_to_str");
                rVal = Session.FromPtr(builder.BuildCall2(numToStrType, numToStr, new[] { Session.ToDouble(right) }, "rstr"));
            }

            var strConcat = module.GetNamedFunction("badlang_str_concat");
            var strConcatType = Session.Runtime.GetRuntimeType("badlang_str_concat");
            var res = builder.BuildCall2(strConcatType, strConcat, new[] { Session.ToPtr(lVal, Session.StringPtrType), Session.ToPtr(rVal, Session.StringPtrType) }, "concat_res");
            Session.LastExpressionType = "string";
            return Session.FromPtr(res);
        }

        // String equality
        if ((binary.Operator.Type == TokenType.EqualEqual || binary.Operator.Type == TokenType.BangEqual)
            && ((leftType == "string" && (rightType == "string" || rightType == "any"))
                || (rightType == "string" && leftType == "any")))
        {
            var strEq = module.GetNamedFunction("badlang_str_eq");
            var strEqType = Session.Runtime.GetRuntimeType("badlang_str_eq");
            var res = builder.BuildCall2(strEqType, strEq, new[] { Session.ToPtr(left, Session.StringPtrType), Session.ToPtr(right, Session.StringPtrType) }, "eq_res");
            if (binary.Operator.Type == TokenType.BangEqual)
            {
                res = builder.BuildNot(res, "not_eq");
            }
            Session.LastExpressionType = "bool";
            return Session.ToI64(res);
        }

        // Arithmetic
        var leftD = Session.ToDouble(left);
        var rightD = Session.ToDouble(right);

        var result = binary.Operator.Type switch
        {
            TokenType.Plus => builder.BuildFAdd(leftD, rightD, "addtmp"),
            TokenType.Minus => builder.BuildFSub(leftD, rightD, "subtmp"),
            TokenType.Star => builder.BuildFMul(leftD, rightD, "multmp"),
            TokenType.Slash => builder.BuildFDiv(leftD, rightD, "divtmp"),
            TokenType.Percent => builder.BuildFRem(leftD, rightD, "modtmp"),
            TokenType.Greater => builder.BuildFCmp(LLVMRealPredicate.LLVMRealOGT, leftD, rightD, "cmptmp"),
            TokenType.GreaterEqual => builder.BuildFCmp(LLVMRealPredicate.LLVMRealOGE, leftD, rightD, "cmptmp"),
            TokenType.Less => builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLT, leftD, rightD, "cmptmp"),
            TokenType.LessEqual => builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLE, leftD, rightD, "cmptmp"),
            TokenType.EqualEqual => builder.BuildFCmp(LLVMRealPredicate.LLVMRealOEQ, leftD, rightD, "cmptmp"),
            TokenType.BangEqual => builder.BuildFCmp(LLVMRealPredicate.LLVMRealONE, leftD, rightD, "cmptmp"),
            TokenType.Ampersand => builder.BuildSIToFP(builder.BuildAnd(builder.BuildFPToSI(leftD, ctx.Int64Type, "lint"), builder.BuildFPToSI(rightD, ctx.Int64Type, "rint"), "andtmp"), ctx.DoubleType, "fres"),
            TokenType.Pipe => builder.BuildSIToFP(builder.BuildOr(builder.BuildFPToSI(leftD, ctx.Int64Type, "lint"), builder.BuildFPToSI(rightD, ctx.Int64Type, "rint"), "ortmp"), ctx.DoubleType, "fres"),
            TokenType.Caret => builder.BuildSIToFP(builder.BuildXor(builder.BuildFPToSI(leftD, ctx.Int64Type, "lint"), builder.BuildFPToSI(rightD, ctx.Int64Type, "rint"), "xortmp"), ctx.DoubleType, "fres"),
            TokenType.LessLess => builder.BuildSIToFP(builder.BuildShl(builder.BuildFPToSI(leftD, ctx.Int64Type, "lint"), builder.BuildFPToSI(rightD, ctx.Int64Type, "rint"), "shltmp"), ctx.DoubleType, "fres"),
            TokenType.GreaterGreater => builder.BuildSIToFP(builder.BuildLShr(builder.BuildFPToSI(leftD, ctx.Int64Type, "lint"), builder.BuildFPToSI(rightD, ctx.Int64Type, "rint"), "shrtmp"), ctx.DoubleType, "fres"),
            _ => throw new CompileError($"Unsupported binary operator: {binary.Operator.Lexeme}", binary.Operator)
        };
        
        Session.LastExpressionType = "number"; // Most binary ops return a number
        return Session.ToI64(result);
    }
}
