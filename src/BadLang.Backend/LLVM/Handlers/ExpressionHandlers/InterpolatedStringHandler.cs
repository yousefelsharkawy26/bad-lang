using System;
using System.Collections.Generic;
using BadLang.Backend.LLVM.Core;
using BadLang.Parser;
using LLVMSharp.Interop;

namespace BadLang.Backend.LLVM.Handlers.ExpressionHandlers;

public class InterpolatedStringHandler : ExpressionHandler
{
    public InterpolatedStringHandler(CompilationSession session, IExpressionCompiler expressionCompiler) 
        : base(session, expressionCompiler) { }

    public override bool CanHandle(Expr expr) => expr is Expr.InterpolatedString;

    public override LLVMValueRef Compile(Expr expr)
    {
        var interpolated = (Expr.InterpolatedString)expr;
        var builder = Session.Infrastructure.Builder;
        var ctx = Session.Infrastructure.Context;
        var module = Session.Infrastructure.Module;
        
        // Use a temporary list to store all the string objects
        var strings = new List<LLVMValueRef>();

        foreach (var part in interpolated.Parts)
        {
            if (part is Expr.Literal partLiteral && partLiteral.Value is string s)
            {
                var cstr = builder.BuildGlobalStringPtr(s, "str");
                var strNew = module.GetNamedFunction("badlang_str_new");
                var strObj = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_str_new"), strNew, new[] { cstr }, "str_obj");
                strings.Add(strObj);
            }
            else
            {
                var val = ExpressionCompiler.Compile(part);
                var valType = Session.LastExpressionType;
                
                LLVMValueRef strObj;
                if (valType == "string")
                {
                    strObj = Session.ToPtr(val, Session.StringPtrType);
                }
                else if (valType == "number")
                {
                    var rtFn = module.GetNamedFunction("badlang_num_to_str");
                    strObj = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_num_to_str"), rtFn, new[] { Session.ToDouble(val) }, "numstr");
                }
                else if (valType == "bool")
                {
                    var rtFn = module.GetNamedFunction("badlang_bool_to_str");
                    strObj = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_bool_to_str"), rtFn, new[] { builder.BuildTrunc(val, ctx.Int1Type, "bool_trunc") }, "boolstr");
                }
                else
                {
                    var rtFn = module.GetNamedFunction("badlang_any_to_str");
                    strObj = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_any_to_str"), rtFn, new[] { val }, "anystr");
                }
                strings.Add(strObj);
            }
        }

        if (strings.Count == 0)
        {
            var emptyCstr = builder.BuildGlobalStringPtr("", "empty_str");
            var strNew = module.GetNamedFunction("badlang_str_new");
            var emptyStrObj = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_str_new"), strNew, new[] { emptyCstr }, "empty_str_obj");
            Session.LastExpressionType = "string";
            return Session.FromPtr(emptyStrObj);
        }

        var result = strings[0];
        for (int i = 1; i < strings.Count; i++)
        {
            var rtFn = module.GetNamedFunction("badlang_str_concat");
            result = builder.BuildCall2(Session.Runtime.GetRuntimeType("badlang_str_concat"), rtFn, new[] { result, strings[i] }, "concat");
        }

        Session.LastExpressionType = "string";
        return Session.FromPtr(result);
    }
}
