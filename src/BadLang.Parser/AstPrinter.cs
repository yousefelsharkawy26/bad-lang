using System.Text;
using System.Linq;

using BadLang.Core;

namespace BadLang.Parser;

public class AstPrinter
{
    public string Print(IReadOnlyList<Stmt> statements)
    {
        StringBuilder sb = new();
        foreach (var stmt in statements)
        {
            sb.AppendLine(Print(stmt, 0));
        }
        return sb.ToString();
    }

    private string Print(Stmt stmt, int indent)
    {
        string padding = new(' ', indent * 2);
        return stmt switch
        {
            Stmt.Var v => $"{padding}var {v.Name.Lexeme}{(v.Type != null ? $": {PrintType(v.Type)}" : "")}{(v.Initializer != null ? $" = {Print(v.Initializer)}" : "")};",
            Stmt.Function f => $"{padding}fn {f.Name.Lexeme}({string.Join(", ", f.Params.Select(p => $"{p.Name.Lexeme}{(p.Type != null ? $": {PrintType(p.Type)}" : "")}"))}) {(f.ReturnType != null ? $": {PrintType(f.ReturnType)} " : "")}{{\n{PrintBlock(f.Body, indent + 1)}\n{padding}}}",
            Stmt.Block b => $"{padding}{{\n{PrintBlock(b.Statements, indent + 1)}\n{padding}}}",
            Stmt.If i => $"{padding}if {Print(i.Condition)} {Print(i.ThenBranch, indent)}{(i.ElseBranch != null ? $" else {Print(i.ElseBranch, indent)}" : "")}",
            Stmt.Expression e => $"{padding}{Print(e.Expr)};",
            Stmt.Return r => $"{padding}return {(r.Value != null ? Print(r.Value) : "")};",
            Stmt.Class c => $"{padding}class {c.Name.Lexeme} {{\n{string.Join("\n", c.Members.Select(m => Print(m, indent + 1)))}\n{padding}}}",
            Stmt.TryCatch tc => $"{padding}try {{\n{PrintBlock(tc.TryBlock, indent + 1)}\n{padding}}}{string.Join("", tc.CatchClauses.Select(c => $" catch({(c.ExceptionType != null ? PrintType(c.ExceptionType) : "")} {c.ExceptionName?.Lexeme}) {{\n{PrintBlock(c.Body, indent + 1)}\n{padding}}}"))}{(tc.FinallyBlock != null ? $" finally {{\n{PrintBlock(tc.FinallyBlock, indent + 1)}\n{padding}}}" : "")}",
            Stmt.Throw th => $"{padding}throw {Print(th.Value)};",
            _ => $"{padding}[Unknown Statement: {stmt.GetType().Name}]"
        };
    }

    private string PrintBlock(IReadOnlyList<Stmt> statements, int indent)
    {
        StringBuilder sb = new();
        foreach (var stmt in statements)
        {
            sb.AppendLine(Print(stmt, indent));
        }
        return sb.ToString().TrimEnd();
    }

    private string Print(Expr expr)
    {
        return expr switch
        {
            Expr.Binary b => $"({Print(b.Left)} {b.Operator.Lexeme} {Print(b.Right)})",
            Expr.Literal l => l.Value?.ToString() ?? "null",
            Expr.Variable v => v.Name.Lexeme,
            Expr.Call c => $"{Print(c.Callee)}({string.Join(", ", c.Arguments.Select(Print))})",
            Expr.Grouping g => $"({Print(g.Expr)})",
            Expr.Unary u => $"({u.Operator.Lexeme}{Print(u.Right)})",
            Expr.Assign a => $"({Print(a.Target)} {a.Operator.Lexeme} {Print(a.Value)})",
            Expr.Get g => $"{Print(g.Object)}.{g.Name.Lexeme}",
            Expr.Set s => $"{Print(s.Object)}.{s.Name.Lexeme} = {Print(s.Value)}",
            Expr.New n => $"new {Print(n.Callee)}({string.Join(", ", n.Arguments.Select(Print))})",
            Expr.This t => "this",
            Expr.Super su => $"super.{su.Method.Lexeme}",
            Expr.Index idx => $"({Print(idx.Target)}[{Print(idx.IndexValue)}])",
            Expr.ArrayLiteral al => $"[{string.Join(", ", al.Elements.Select(Print))}]",
            Expr.TypeOf t => $"typeof({Print(t.Expr)})",
            Expr.NameOf n => $"nameof({Print(n.Expr)})",
            Expr.ToStringExpr ts => $"toString({Print(ts.Expr)})",
            Expr.ToNumberExpr tn => $"toNumber({Print(tn.Expr)})",
            Expr.IsNullExpr isnull => $"isNull({Print(isnull.Expr)})",
            Expr.AssertExpr a => $"assert({Print(a.Condition)}{(a.Message != null ? $", {Print(a.Message)}" : "")})",
            Expr.PanicExpr p => $"panic({Print(p.Message)})",
            _ => $"[Unknown Expr: {expr.GetType().Name}]"
        };
    }

    private string PrintType(TypeNode type)
    {
        return type switch
        {
            TypeNode.Primitive p => p.Token.Lexeme,
            TypeNode.Array a => $"{PrintType(a.BaseType)}[]",
            TypeNode.Generic g => $"{g.Name.Lexeme}<{string.Join(", ", g.TypeArguments.Select(PrintType))}>",
            TypeNode.UserDefined u => u.Name.Lexeme,
            _ => "[Unknown Type]"
        };
    }
}
