using BadLang.Core;

namespace BadLang.Parser;

public abstract record TypeNode
{
    public record Primitive(Token Token) : TypeNode;
    public record Array(TypeNode BaseType) : TypeNode;
    public record Generic(Token Name, IReadOnlyList<TypeNode> TypeArguments) : TypeNode;
    public record UserDefined(Token Name) : TypeNode;
}

public abstract record Expr
{
    public record Binary(Expr Left, Token Operator, Expr Right) : Expr;
    public record Grouping(Expr Expr) : Expr;
    public record Literal(object? Value) : Expr;
    public record Unary(Token Operator, Expr Right) : Expr;
    public record Variable(Token Name) : Expr;
    public record Assign(Expr Target, Token Operator, Expr Value) : Expr;
    public record Logical(Expr Left, Token Operator, Expr Right) : Expr;
    public record Call(Expr Callee, Token Paren, IReadOnlyList<Expr> Arguments) : Expr;
    public record Get(Expr Object, Token Name) : Expr;
    public record Set(Expr Object, Token Name, Expr Value) : Expr;
    public record Ternary(Expr Condition, Expr ThenBranch, Expr ElseBranch) : Expr;
    public record NullCoalesce(Expr Left, Expr Right) : Expr;
    public record ArrayLiteral(IReadOnlyList<Expr> Elements) : Expr;
    public record TypeCast(Expr Expr, TypeNode TargetType) : Expr;
    public record Lambda(IReadOnlyList<Parameter> Parameters, IReadOnlyList<Stmt> Body, Expr? ExpressionBody) : Expr;
    public record New(Expr Callee, IReadOnlyList<Expr> Arguments) : Expr;
    public record This(Token Keyword) : Expr;
    public record Super(Token Keyword, Token Method) : Expr;
    public record Index(Expr Target, Token Bracket, Expr IndexValue) : Expr;
    public record MapLiteral(Token Brace, IReadOnlyList<(Expr Key, Expr Value)> Entries) : Expr;
    public record InterpolatedString(Token Token, IReadOnlyList<Expr> Parts) : Expr;
    public record TypeOf(Token Keyword, Expr Expr) : Expr;
    public record NameOf(Token Keyword, Expr Expr) : Expr;
    public record ToStringExpr(Token Keyword, Expr Expr) : Expr;
    public record ToNumberExpr(Token Keyword, Expr Expr) : Expr;
    public record IsNullExpr(Token Keyword, Expr Expr) : Expr;
    public record AssertExpr(Token Keyword, Expr Condition, Expr? Message) : Expr;
    public record PanicExpr(Token Keyword, Expr Message) : Expr;
}

public abstract record Stmt
{
    public record Expression(Expr Expr) : Stmt;
    public record Var(Token Name, TypeNode? Type, Expr? Initializer) : Stmt;
    public record Const(Token Name, TypeNode? Type, Expr Initializer) : Stmt;
    public record Block(IReadOnlyList<Stmt> Statements) : Stmt;
    public record If(Expr Condition, Stmt ThenBranch, Stmt? ElseBranch) : Stmt;
    public record While(Token Keyword, Expr Condition, Stmt Body) : Stmt;
    public record DoWhile(Token Keyword, Expr Condition, Stmt Body) : Stmt;
    public record ForIn(Token Variable, Expr Iterable, Stmt Body) : Stmt;
    public record Function(Token Name, IReadOnlyList<Parameter> Params, TypeNode? ReturnType, IReadOnlyList<Stmt> Body) : Stmt;
    public record Return(Token Keyword, Expr? Value) : Stmt;
    public record Class(Token Name, IReadOnlyList<Token> Generics, IReadOnlyList<Token> Parents, IReadOnlyList<Stmt> Members) : Stmt;
    public record Interface(Token Name, IReadOnlyList<Token> Generics, IReadOnlyList<Stmt> Signatures) : Stmt;
    public record Struct(Token Name, IReadOnlyList<Stmt.Var> Fields) : Stmt;
    public record Enum(Token Name, IReadOnlyList<Token> Variants) : Stmt;
    public record Switch(Expr Expr, IReadOnlyList<CaseClause> Cases, IReadOnlyList<Stmt>? DefaultBranch) : Stmt;
    public record TryCatch(IReadOnlyList<Stmt> TryBlock, IReadOnlyList<CatchClause> CatchClauses, IReadOnlyList<Stmt>? FinallyBlock) : Stmt;
    public record Import(IReadOnlyList<Token> Path, Token? Alias = null, IReadOnlyList<Token>? Symbols = null) : Stmt;
    public record Break(Token Keyword) : Stmt;
    public record Continue(Token Keyword) : Stmt;
    public record Throw(Token Keyword, Expr Value) : Stmt;
    public record Export(Stmt Declaration) : Stmt;
    public record ExportList(IReadOnlyList<Token> Symbols) : Stmt;
}

public record Parameter(Token Name, TypeNode? Type);
public record CaseClause(Expr? Condition, IReadOnlyList<Stmt> Body);
public record CatchClause(TypeNode? ExceptionType, Token? ExceptionName, IReadOnlyList<Stmt> Body);
