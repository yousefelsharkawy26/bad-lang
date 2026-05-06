using System;
using System.Collections.Generic;
using BadLang.Parser;
using BadLang.IR.Handlers;
using BadLang.Parser.Ast;

namespace BadLang.IR
{
    public class IRBuilder : IIRBuilderContext
    {
        private int _tempCount = 0;
        private int _labelCount = 0;
        private Stack<(string StartLabel, string EndLabel)> _loops = new();
        private readonly IRBuilderRegistry _registry;

        public IRBuilder() : this(new IRBuilderRegistry())
        {
        }

        public IRBuilder(IRBuilderRegistry registry)
        {
            _registry = registry;
        }

        public string NextTemp() => $"_t{_tempCount++}";
        public string NextLabel(string prefix = "L") => $"{prefix}_{_labelCount++}";
        
        public void PushLoop(string startLabel, string endLabel) => _loops.Push((startLabel, endLabel));
        public void PopLoop() => _loops.Pop();
        public (string StartLabel, string EndLabel) PeekLoop() => _loops.Peek();
        public bool HasLoop => _loops.Count > 0;

        public List<IrNode> Build(IReadOnlyList<Stmt> statements)
        {
            var ir = new List<IrNode>();
            foreach (var stmt in statements)
            {
                BuildStmt(stmt, ir);
            }
            return ir;
        }

        public void BuildStmt(Stmt stmt, List<IrNode> ir)
        {
            if (_registry.StmtHandlers.TryGetValue(stmt.GetType(), out var handler))
            {
                handler.Build(stmt, ir, this);
            }
            else
            {
                Console.WriteLine($"[IRBuilder] Warning: statement type {stmt.GetType().Name} is not fully supported yet. Generating NO-OP.");
            }
        }

        public IrValue BuildExpr(Expr expr, List<IrNode> ir)
        {
            if (_registry.ExprHandlers.TryGetValue(expr.GetType(), out var handler))
            {
                return handler.Build(expr, ir, this);
            }
            else
            {
                Console.WriteLine($"[IRBuilder] Warning: expression type {expr.GetType().Name} is not fully supported yet. Returning null.");
                return new IrConst(null);
            }
        }

        public string? GetExportName(Stmt stmt)
        {
            return stmt switch
            {
                Stmt.Var v => v.Name.Lexeme,
                Stmt.Const c => c.Name.Lexeme,
                Stmt.Function f => f.Name.Lexeme,
                Stmt.Class c => c.Name.Lexeme,
                Stmt.Interface i => i.Name.Lexeme,
                Stmt.Struct s => s.Name.Lexeme,
                Stmt.Enum e => e.Name.Lexeme,
                _ => null
            };
        }
    }
}
