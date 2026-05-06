using System;
using System.Collections.Generic;
using System.Linq;
using BadLang.Parser;
using BadLang.Parser.Ast;

namespace BadLang.IR.Handlers.Statements
{
    public class VarStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.Var);
        public void Build(Stmt stmt, List<IrNode> ir, IIRBuilderContext context)
        {
            var varStmt = (Stmt.Var)stmt;
            var val = varStmt.Initializer != null ? context.BuildExpr(varStmt.Initializer, ir) : new IrConst(null);
            ir.Add(new IrDefine { VariableName = varStmt.Name.Lexeme, Value = val });
        }
    }

    public class ConstStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.Const);
        public void Build(Stmt stmt, List<IrNode> ir, IIRBuilderContext context)
        {
            var constStmt = (Stmt.Const)stmt;
            var constVal = context.BuildExpr(constStmt.Initializer, ir);
            ir.Add(new IrDefine { VariableName = constStmt.Name.Lexeme, Value = constVal });
        }
    }

    public class FunctionStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.Function);
        public void Build(Stmt stmt, List<IrNode> ir, IIRBuilderContext context)
        {
            var funcStmt = (Stmt.Function)stmt;
            var body = new List<IrNode>();
            context.BuildStmt(new Stmt.Block(funcStmt.Body), body);
            var funcDef = new IrFunctionDef
            {
                Name = funcStmt.Name.Lexeme,
                Parameters = funcStmt.Params.Select(p => p.Name.Lexeme).ToList(),
                Body = body
            };
            ir.Add(funcDef);
        }
    }

    public class ClassStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.Class);
        public void Build(Stmt stmt, List<IrNode> ir, IIRBuilderContext context)
        {
            var classStmt = (Stmt.Class)stmt;
            var methods = new List<IrFunctionDef>();
            var fields = new List<string>();

            foreach (var member in classStmt.Members)
            {
                if (member is Stmt.Function f)
                {
                    var body = new List<IrNode>();
                    context.BuildStmt(new Stmt.Block(f.Body), body);
                    var funcDef = new IrFunctionDef
                    {
                        Name = f.Name.Lexeme,
                        Parameters = f.Params.Select(p => p.Name.Lexeme).ToList(),
                        Body = body
                    };
                    methods.Add(funcDef);
                }
                else if (member is Stmt.Var v)
                {
                    fields.Add(v.Name.Lexeme);
                }
            }

            var classDef = new IrClassDef
            {
                Name = classStmt.Name.Lexeme,
                SuperClass = classStmt.Parents.Count > 0 ? classStmt.Parents[0].Lexeme : null,
                Methods = methods,
                Fields = fields
            };
            ir.Add(classDef);
        }
    }

    public class InterfaceStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.Interface);
        public void Build(Stmt stmt, List<IrNode> ir, IIRBuilderContext context)
        {
            var interfaceStmt = (Stmt.Interface)stmt;
            var interfaceDef = new IrInterfaceDef
            {
                Name = interfaceStmt.Name.Lexeme,
                MethodSignatures = interfaceStmt.Signatures.Select(s => s is Stmt.Function f ? f.Name.Lexeme : "unknown").ToList()
            };
            ir.Add(interfaceDef);
        }
    }

    public class StructStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.Struct);
        public void Build(Stmt stmt, List<IrNode> ir, IIRBuilderContext context)
        {
            var structStmt = (Stmt.Struct)stmt;
            var structDef = new IrStructDef
            {
                Name = structStmt.Name.Lexeme,
                Fields = structStmt.Fields.Select(f => f.Name.Lexeme).ToList()
            };
            ir.Add(structDef);
        }
    }

    public class EnumStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.Enum);
        public void Build(Stmt stmt, List<IrNode> ir, IIRBuilderContext context)
        {
            var enumStmt = (Stmt.Enum)stmt;
            var enumDef = new IrEnumDef
            {
                Name = enumStmt.Name.Lexeme,
                Variants = enumStmt.Variants.Select(v => v.Lexeme).ToList()
            };
            ir.Add(enumDef);
        }
    }
}

