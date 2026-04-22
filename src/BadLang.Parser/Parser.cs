using BadLang.Core;
using BadLang.Lexer;

namespace BadLang.Parser;

public class ParseError : Exception
{
    public Token Token { get; }
    public ParseError(Token token, string message) : base(message) => Token = token;
}

public class Parser
{
    private readonly List<Token> _tokens;
    private int _current = 0;

    public Parser(List<Token> tokens)
    {
        _tokens = tokens.FindAll(t => t.Type != TokenType.Comment);
    }

    public List<ParseError> Errors { get; } = new();

    // ─── Entry Point ──────────────────────────────────────────────────────────

    public List<Stmt> Parse()
    {
        var statements = new List<Stmt>();
        while (!IsAtEnd())
        {
            try   { statements.Add(Declaration()); }
            catch (ParseError ex) { Errors.Add(ex); Synchronize(); }
        }
        return statements;
    }

    // ─── Declarations ─────────────────────────────────────────────────────────

    private Stmt Declaration()
    {
        if (Match(TokenType.Import))    return ImportDeclaration();
        if (Match(TokenType.Class))     return ClassDeclaration();
        if (Match(TokenType.Interface)) return InterfaceDeclaration();
        if (Match(TokenType.Struct))    return StructDeclaration();
        if (Match(TokenType.Enum))      return EnumDeclaration();
        if (Match(TokenType.Fn))        return FunctionDeclaration();
        if (Match(TokenType.Var))       return VarDeclaration();
        if (Match(TokenType.Const))     return ConstDeclaration();
        if (Match(TokenType.Export))    return ExportDeclaration();
        if (IsTypeStart())              return TypePrefixedDeclaration();
        return Statement();
    }

    private Stmt ImportDeclaration()
    {
        var path = new List<Token> { ConsumeModuleSegment("Expect module name.") };
        while (Match(TokenType.Dot))
            path.Add(ConsumeModuleSegment("Expect module segment after '.'."));

        Token? alias = null;
        List<Token>? symbols = null;

        while (!Check(TokenType.Semicolon) && !IsAtEnd())
        {
            if (Match(TokenType.As))
            {
                if (alias != null) throw new ParseError(Previous(), "Already have an alias for this import.");
                alias = Consume(TokenType.Identifier, "Expect alias name after 'as'.");
            }
            else if (Match(TokenType.OpenBrace))
            {
                if (symbols != null) throw new ParseError(Previous(), "Already have a symbol list for this import.");
                symbols = new List<Token>();
                if (!Check(TokenType.CloseBrace))
                {
                    do { symbols.Add(Consume(TokenType.Identifier, "Expect symbol name.")); }
                    while (Match(TokenType.Comma));
                }
                Consume(TokenType.CloseBrace, "Expect '}' after symbols.");
            }
            else
            {
                break;
            }
        }

        Consume(TokenType.Semicolon, "Expect ';' after import.");
        return new Stmt.Import(path, alias, symbols);
    }


    private Stmt ClassDeclaration()
    {
        Token name     = Consume(TokenType.Identifier, "Expect class name.");
        var generics   = ParseGenericParameters();
        var parents    = new List<Token>();

        if (Match(TokenType.Colon))
        {
            parents.Add(Consume(TokenType.Identifier, "Expect parent name."));
            while (Match(TokenType.Comma))
                parents.Add(Consume(TokenType.Identifier, "Expect parent name."));
        }

        Consume(TokenType.OpenBrace, "Expect '{' before class body.");
        var members = ParseUntil(TokenType.CloseBrace, Declaration);
        Consume(TokenType.CloseBrace, "Expect '}' after class body.");
        return new Stmt.Class(name, generics, parents, members);
    }

    private Stmt InterfaceDeclaration()
    {
        Token name   = Consume(TokenType.Identifier, "Expect interface name.");
        var generics = ParseGenericParameters();
        Consume(TokenType.OpenBrace, "Expect '{' before interface body.");
        var signatures = ParseUntil(TokenType.CloseBrace, Declaration);
        Consume(TokenType.CloseBrace, "Expect '}' after interface body.");
        return new Stmt.Interface(name, generics, signatures);
    }

    private Stmt StructDeclaration()
    {
        Token name = Consume(TokenType.Identifier, "Expect struct name.");
        Consume(TokenType.OpenBrace, "Expect '{' before struct body.");
        var fields = new List<Stmt.Var>();
        if (!Check(TokenType.CloseBrace))
        {
            do
            {
                if (Match(TokenType.Var))
                {
                    fields.Add((Stmt.Var)VarDeclaration());
                }
                else
                {
                    Token fieldName = Consume(TokenType.Identifier, "Expect field name.");
                    fields.Add(new Stmt.Var(fieldName, null, null));
                    // Optional separators
                    if (Check(TokenType.Comma) || Check(TokenType.Semicolon))
                        Advance();
                }
            } while (!Check(TokenType.CloseBrace) && !IsAtEnd());
        }
        Consume(TokenType.CloseBrace, "Expect '}' after struct body.");
        return new Stmt.Struct(name, fields);
    }

    private Stmt EnumDeclaration()
    {
        Token name = Consume(TokenType.Identifier, "Expect enum name.");
        Consume(TokenType.OpenBrace, "Expect '{' before enum variants.");
        var variants = new List<Token>();
        if (!Check(TokenType.CloseBrace))
        {
            variants.Add(Consume(TokenType.Identifier, "Expect enum variant."));
            while (Match(TokenType.Comma))
                variants.Add(Consume(TokenType.Identifier, "Expect enum variant."));
        }
        Consume(TokenType.CloseBrace, "Expect '}' after enum variants.");
        return new Stmt.Enum(name, variants);
    }

    private Stmt FunctionDeclaration()
    {
        Token name = Consume(TokenType.Identifier, "Expect function name.");
        Consume(TokenType.OpenParen, "Expect '(' after function name.");
        var parameters = ParseParameters();
        Consume(TokenType.CloseParen, "Expect ')' after parameters.");

        TypeNode? returnType = Match(TokenType.Colon) ? ParseType() : null;

        if (Match(TokenType.Semicolon))
            return new Stmt.Function(name, parameters, returnType, new List<Stmt>());

        Consume(TokenType.OpenBrace, "Expect '{' before function body.");
        return new Stmt.Function(name, parameters, returnType, Block());
    }

    private Stmt VarDeclaration()
    {
        Token name       = Consume(TokenType.Identifier, "Expect variable name.");
        TypeNode? type   = Match(TokenType.Colon) ? ParseType() : null;
        Expr? initializer = Match(TokenType.Equal) ? Expression() : null;
        Consume(TokenType.Semicolon, "Expect ';' after variable declaration.");
        return new Stmt.Var(name, type, initializer);
    }

    private Stmt ConstDeclaration()
    {
        Token name     = Consume(TokenType.Identifier, "Expect constant name.");
        TypeNode? type = Match(TokenType.Colon) ? ParseType() : null;
        Consume(TokenType.Equal, "Expect '=' after constant name.");
        Expr initializer = Expression();
        Consume(TokenType.Semicolon, "Expect ';' after constant declaration.");
        return new Stmt.Const(name, type, initializer);
    }

    private Stmt ExportDeclaration()
    {
        if (Match(TokenType.OpenBrace))
        {
            var symbols = new List<Token>();
            if (!Check(TokenType.CloseBrace))
            {
                do
                {
                    symbols.Add(Consume(TokenType.Identifier, "Expect symbol name in export list."));
                } while (Match(TokenType.Comma));
            }
            Consume(TokenType.CloseBrace, "Expect '}' after export list.");
            Consume(TokenType.Semicolon, "Expect ';' after export list.");
            return new Stmt.ExportList(symbols);
        }

        Stmt decl = Declaration();
        if (decl is not (Stmt.Var or Stmt.Const or Stmt.Function or Stmt.Class
                      or Stmt.Interface or Stmt.Struct or Stmt.Enum))
            throw Error(Previous(), "Only declarations can be exported.");
        return new Stmt.Export(decl);
    }

    private Stmt TypePrefixedDeclaration()
    {
        TypeNode type    = ParseType();
        Token name       = Consume(TokenType.Identifier, "Expect variable name.");
        Expr? initializer = Match(TokenType.Equal) ? Expression() : null;
        Consume(TokenType.Semicolon, "Expect ';' after declaration.");
        return new Stmt.Var(name, type, initializer);
    }

    // ─── Statements ───────────────────────────────────────────────────────────

    private Stmt Statement()
    {
        if (Match(TokenType.If))        return IfStatement();
        if (Match(TokenType.While))     return WhileStatement();
        if (Match(TokenType.Do))        return DoWhileStatement();
        if (Match(TokenType.For))       return ForStatement();
        if (Match(TokenType.Switch))    return SwitchStatement();
        if (Match(TokenType.Try))       return TryCatchStatement();
        if (Match(TokenType.Return))    return ReturnStatement();
        if (Match(TokenType.Throw))     return ThrowStatement();
        if (Match(TokenType.OpenBrace)) return new Stmt.Block(Block());

        if (Match(TokenType.Break))
        {
            Token kw = Previous();
            Consume(TokenType.Semicolon, "Expect ';' after 'break'.");
            return new Stmt.Break(kw);
        }
        if (Match(TokenType.Continue))
        {
            Token kw = Previous();
            Consume(TokenType.Semicolon, "Expect ';' after 'continue'.");
            return new Stmt.Continue(kw);
        }

        return ExpressionStatement();
    }

    private Stmt IfStatement()
    {
        Expr condition  = ParseOptionallyParenthesized(Expression);
        Stmt thenBranch = Statement();
        Stmt? elseBranch = Match(TokenType.Else) ? Statement() : null;
        return new Stmt.If(condition, thenBranch, elseBranch);
    }

    private Stmt WhileStatement()
    {
        Token keyword  = Previous();
        Expr condition = ParseOptionallyParenthesized(Expression);
        Stmt body      = Statement();
        return new Stmt.While(keyword, condition, body);
    }

    private Stmt DoWhileStatement()
    {
        Token keyword = Previous();
        Stmt body     = Statement();
        Consume(TokenType.While, "Expect 'while' after 'do' body.");
        Expr condition = ParseOptionallyParenthesized(Expression);
        Consume(TokenType.Semicolon, "Expect ';' after do-while.");
        return new Stmt.DoWhile(keyword, condition, body);
    }

    private Stmt ForStatement()
    {
        bool hasParen = Match(TokenType.OpenParen);
        Token name = Consume(TokenType.Identifier, "Expect variable name in for-in.");
        Consume(TokenType.In, "Expect 'in' after for-in variable.");
        Expr iterable = Expression();
        if (hasParen) Consume(TokenType.CloseParen, "Expect ')' after for-in.");
        Stmt body     = Statement();
        return new Stmt.ForIn(name, iterable, body);
    }

    private Stmt SwitchStatement()
    {
        Expr expression = ParseOptionallyParenthesized(Expression);
        Consume(TokenType.OpenBrace, "Expect '{' before switch body.");

        var cases = new List<CaseClause>();
        List<Stmt>? defaultBranch = null;

        while (!Check(TokenType.CloseBrace) && !IsAtEnd())
        {
            if (Match(TokenType.Case))
            {
                Expr condition = Expression();
                Consume(TokenType.Colon, "Expect ':' after case value.");
                cases.Add(new CaseClause(condition, SwitchBody()));
            }
            else if (Match(TokenType.Default))
            {
                Consume(TokenType.Colon, "Expect ':' after default.");
                defaultBranch = SwitchBody();
            }
        }

        Consume(TokenType.CloseBrace, "Expect '}' after switch body.");
        return new Stmt.Switch(expression, cases, defaultBranch);
    }

    private List<Stmt> SwitchBody()
    {
        if (Check(TokenType.OpenBrace)) return Block();
        return ParseUntil(TokenType.Case, TokenType.Default, TokenType.CloseBrace, Declaration);
    }

    private Stmt TryCatchStatement()
    {
        Consume(TokenType.OpenBrace, "Expect '{' to start try block.");
        var tryBlock     = Block();
        var catchClauses = new List<CatchClause>();

        while (Match(TokenType.Catch))
        {
            TypeNode? type = null;
            Token? catchVar = null;

            if (Match(TokenType.OpenParen))
            {
                (type, catchVar) = ParseCatchParameter();
                Consume(TokenType.CloseParen, "Expect ')' after catch parameter.");
            }

            Consume(TokenType.OpenBrace, "Expect '{' to start catch block.");
            catchClauses.Add(new CatchClause(type, catchVar, Block()));
        }

        List<Stmt>? finallyBlock = null;
        if (Match(TokenType.Finally))
        {
            Consume(TokenType.OpenBrace, "Expect '{' to start finally block.");
            finallyBlock = Block();
        }

        return new Stmt.TryCatch(tryBlock, catchClauses, finallyBlock);
    }

    /// <summary>Determines (type, name) from a catch(...) parameter.</summary>
    private (TypeNode? type, Token? name) ParseCatchParameter()
    {
        // (TypeName varName)  — two identifiers
        if (Check(TokenType.Identifier) && PeekNext().Type == TokenType.Identifier)
            return (ParseType(), Consume(TokenType.Identifier, "Expect exception name."));

        // (varName)  — single bare identifier
        if (Check(TokenType.Identifier))
            return (null, Consume(TokenType.Identifier, "Expect exception name."));

        // (PrimitiveType varName?)
        TypeNode type = ParseType();
        Token? name   = Check(TokenType.Identifier)
            ? Consume(TokenType.Identifier, "Expect exception name.")
            : null;
        return (type, name);
    }

    private Stmt ReturnStatement()
    {
        Token keyword = Previous();
        Expr? value   = !Check(TokenType.Semicolon) ? Expression() : null;
        Consume(TokenType.Semicolon, "Expect ';' after return value.");
        return new Stmt.Return(keyword, value);
    }

    private Stmt ThrowStatement()
    {
        Token keyword = Previous();
        Expr value    = Expression();
        Consume(TokenType.Semicolon, "Expect ';' after throw.");
        return new Stmt.Throw(keyword, value);
    }

    private List<Stmt> Block()
    {
        var statements = ParseUntil(TokenType.CloseBrace, Declaration);
        Consume(TokenType.CloseBrace, "Expect '}' after block.");
        return statements;
    }

    private Stmt ExpressionStatement()
    {
        Expr expr = Expression();
        Consume(TokenType.Semicolon, "Expect ';' after expression.");
        return new Stmt.Expression(expr);
    }

    // ─── Expressions ──────────────────────────────────────────────────────────

    private Expr Expression() => Assignment();

    private Expr Assignment()
    {
        Expr expr = Ternary();

        if (!Match(TokenType.Equal, TokenType.PlusEqual, TokenType.MinusEqual,
                   TokenType.StarEqual, TokenType.SlashEqual, TokenType.PercentEqual))
            return expr;

        Token op    = Previous();
        Expr  value = Assignment();

        return expr switch
        {
            Expr.Variable v => new Expr.Assign(v, op, value),
            Expr.Index    i => new Expr.Assign(i, op, value),
            Expr.Get      g => new Expr.Set(g.Object, g.Name, op.Type == TokenType.Equal
                                    ? value
                                    : new Expr.Binary(g, op, value)),
            _ => throw Error(op, "Invalid assignment target.")
        };
    }

    private Expr Ternary()
    {
        Expr expr = NullCoalesce();
        if (!Match(TokenType.Question)) return expr;
        Expr thenBranch = Expression();
        Consume(TokenType.Colon, "Expect ':' in ternary expression.");
        return new Expr.Ternary(expr, thenBranch, Expression());
    }

    private Expr NullCoalesce()
    {
        Expr expr = Or();
        while (Match(TokenType.QuestionQuestion))
            expr = new Expr.NullCoalesce(expr, Or());
        return expr;
    }

    private Expr Or()  => ParseLogical(And,       TokenType.Or);
    private Expr And() => ParseLogical(BitwiseOr, TokenType.And);

    private Expr BitwiseOr()  => ParseBinary(BitwiseXor, TokenType.Pipe);
    private Expr BitwiseXor() => ParseBinary(BitwiseAnd, TokenType.Caret);
    private Expr BitwiseAnd() => ParseBinary(Equality,   TokenType.Ampersand);
    private Expr Equality()   => ParseBinary(Comparison, TokenType.BangEqual,  TokenType.EqualEqual);
    private Expr Comparison() => ParseBinary(Shift,      TokenType.Greater,    TokenType.GreaterEqual,
                                                          TokenType.Less,       TokenType.LessEqual);
    private Expr Shift()      => ParseBinary(Term,       TokenType.LessLess,   TokenType.GreaterGreater);
    private Expr Term()       => ParseBinary(Factor,     TokenType.Minus,      TokenType.Plus);
    private Expr Factor()     => ParseBinary(Unary,      TokenType.Slash,      TokenType.Star, TokenType.Percent);

    private Expr Unary()
    {
        if (!Match(TokenType.BangLog, TokenType.Minus, TokenType.PlusPlus, TokenType.MinusMinus))
            return Cast();
        Token op = Previous();
        return new Expr.Unary(op, Unary());
    }

    private Expr Cast()
    {
        Expr expr = Call();
        while (Match(TokenType.As))
            expr = new Expr.TypeCast(expr, ParseType());
        return expr;
    }

    private Expr Call()
    {
        Expr expr = Primary();
        while (true)
        {
            if (Match(TokenType.OpenParen))
            {
                expr = FinishCall(expr);
            }
            else if (Match(TokenType.Dot))
            {
                Token name = Consume(TokenType.Identifier, "Expect property name after '.'.");
                expr = new Expr.Get(expr, name);
            }
            else if (Match(TokenType.OpenBracket))
            {
                Token bracket = Previous();
                Expr  index   = Expression();
                Consume(TokenType.CloseBracket, "Expect ']' after index.");
                expr = new Expr.Index(expr, bracket, index);
            }
            else break;
        }
        return expr;
    }

    private Expr FinishCall(Expr callee)
    {
        var arguments = new List<Expr>();
        if (!Check(TokenType.CloseParen))
        {
            do { arguments.Add(Expression()); } while (Match(TokenType.Comma));
        }
        Token paren = Consume(TokenType.CloseParen, "Expect ')' after arguments.");
        return new Expr.Call(callee, paren, arguments);
    }

    private Expr Primary()
    {
        if (Match(TokenType.False))  return new Expr.Literal(false);
        if (Match(TokenType.True))   return new Expr.Literal(true);
        if (Match(TokenType.Null))   return new Expr.Literal(null);
        if (Match(TokenType.Number, TokenType.String)) return new Expr.Literal(Previous().Literal);

        if (Match(TokenType.New))        return NewExpression();
        if (Match(TokenType.Fn))         return LambdaExpression();
        if (Match(TokenType.This))       return new Expr.This(Previous());
        if (Match(TokenType.Super))      return SuperExpression();
        if (Match(TokenType.OpenParen))  return LambdaOrGrouping();
        if (Match(TokenType.OpenBracket))return ArrayLiteralExpression();
        if (Match(TokenType.OpenBrace))  return MapLiteralExpression();

        if (Match(TokenType.InterpolatedString))
            return ParseInterpolatedString(Previous(), (string)Previous().Literal!);

        // Single-argument built-in keyword expressions: keyword(expr)
        if (Match(TokenType.TypeOf))   return ParseSingleArgKeyword(kw => new Expr.TypeOf(kw, default!));
        if (Match(TokenType.NameOf))   return ParseSingleArgKeyword(kw => new Expr.NameOf(kw, default!));
        if (Match(TokenType.ToString)) return ParseSingleArgKeyword(kw => new Expr.ToStringExpr(kw, default!));
        if (Match(TokenType.ToNumber)) return ParseSingleArgKeyword(kw => new Expr.ToNumberExpr(kw, default!));
        if (Match(TokenType.IsNull))   return ParseSingleArgKeyword(kw => new Expr.IsNullExpr(kw, default!));
        if (Match(TokenType.Panic))    return ParseSingleArgKeyword(kw => new Expr.PanicExpr(kw, default!));

        if (Match(TokenType.Assert))   return AssertExpression();

        if (Match(TokenType.Identifier, TokenType.NumType, TokenType.StringType,
                  TokenType.BoolType, TokenType.CharType, TokenType.AnyType))
            return new Expr.Variable(Previous());

        if (Match(TokenType.Unknown))
            throw Error(Previous(), (string)Previous().Literal!);

        throw Error(Peek(), "Expect expression.");
    }

    // ─── Primary helpers ──────────────────────────────────────────────────────

    /// <summary>Parses a keyword built-in that takes a single parenthesised expression.</summary>
    private Expr ParseSingleArgKeyword(Func<Token, Expr> factory)
    {
        Token keyword = Previous();
        Consume(TokenType.OpenParen, $"Expect '(' after '{keyword.Lexeme}'.");
        Expr arg = Expression();
        Consume(TokenType.CloseParen, $"Expect ')' after expression.");
        // Re-create using the real argument
        return keyword.Type switch
        {
            TokenType.TypeOf   => new Expr.TypeOf(keyword, arg),
            TokenType.NameOf   => new Expr.NameOf(keyword, arg),
            TokenType.ToString => new Expr.ToStringExpr(keyword, arg),
            TokenType.ToNumber => new Expr.ToNumberExpr(keyword, arg),
            TokenType.IsNull   => new Expr.IsNullExpr(keyword, arg),
            TokenType.Panic    => new Expr.PanicExpr(keyword, arg),
            _                  => factory(keyword)
        };
    }

    private Expr AssertExpression()
    {
        Token keyword = Previous();
        Consume(TokenType.OpenParen, "Expect '(' after 'assert'.");
        Expr condition = Expression();
        Expr? message  = Match(TokenType.Comma) ? Expression() : null;
        Consume(TokenType.CloseParen, "Expect ')' after assert arguments.");
        return new Expr.AssertExpr(keyword, condition, message);
    }

    private Expr NewExpression()
    {
        Expr nameExpr = new Expr.Variable(Consume(TokenType.Identifier, "Expect class name."));
        while (Match(TokenType.Dot))
        {
            Token property = Consume(TokenType.Identifier, "Expect property name after '.'.");
            nameExpr = new Expr.Get(nameExpr, property);
        }
        Consume(TokenType.OpenParen, "Expect '(' after class name.");
        var args = new List<Expr>();
        if (!Check(TokenType.CloseParen))
            do { args.Add(Expression()); } while (Match(TokenType.Comma));
        Consume(TokenType.CloseParen, "Expect ')' after arguments.");
        return new Expr.New(nameExpr, args);
    }

    private Expr LambdaExpression()
    {
        Consume(TokenType.OpenParen, "Expect '(' after 'fn'.");
        var parameters = ParseParameters();
        Consume(TokenType.CloseParen, "Expect ')' after parameters.");

        if (Match(TokenType.Arrow))
            return new Expr.Lambda(parameters, new List<Stmt>(), Expression());

        Consume(TokenType.OpenBrace, "Expect '{' before lambda body.");
        return new Expr.Lambda(parameters, Block(), null);
    }

    private Expr SuperExpression()
    {
        Token keyword = Previous();
        Consume(TokenType.Dot, "Expect '.' after 'super'.");
        Token method = Consume(TokenType.Identifier, "Expect superclass method name.");
        return new Expr.Super(keyword, method);
    }

    private Expr LambdaOrGrouping()
    {
        int saved = _current;
        List<Parameter> parameters = new();
        
        // Try parsing as lambda parameters
        bool couldBeLambda = false;
        try 
        {
            if (!Check(TokenType.CloseParen))
            {
                parameters = ParseParameters();
            }
            if (Match(TokenType.CloseParen) && Check(TokenType.Arrow))
            {
                couldBeLambda = true;
            }
        }
        catch { /* ignore, backtrack later */ }

        if (couldBeLambda)
        {
            Consume(TokenType.Arrow, "Expect '=>' after lambda parameters.");
            if (Match(TokenType.OpenBrace))
            {
                return new Expr.Lambda(parameters, Block(), null);
            }
            return new Expr.Lambda(parameters, new List<Stmt>(), Expression());
        }

        // Backtrack and parse as grouping
        _current = saved;
        Expr expr = Expression();
        Consume(TokenType.CloseParen, "Expect ')' after expression.");
        return new Expr.Grouping(expr);
    }

    private Expr GroupingExpression()
    {
        Expr expr = Expression();
        Consume(TokenType.CloseParen, "Expect ')' after expression.");
        return new Expr.Grouping(expr);
    }

    private Expr ArrayLiteralExpression()
    {
        var elements = new List<Expr>();
        if (!Check(TokenType.CloseBracket))
            do { elements.Add(Expression()); } while (Match(TokenType.Comma));
        Consume(TokenType.CloseBracket, "Expect ']' after array elements.");
        return new Expr.ArrayLiteral(elements);
    }

    private Expr MapLiteralExpression()
    {
        Token brace  = Previous();
        var entries  = new List<(Expr Key, Expr Value)>();
        if (!Check(TokenType.CloseBrace))
        {
            do
            {
                Expr key = Expression();
                Consume(TokenType.Colon, "Expect ':' after map key.");
                entries.Add((key, Expression()));
            } while (Match(TokenType.Comma));
        }
        Consume(TokenType.CloseBrace, "Expect '}' after map entries.");
        return new Expr.MapLiteral(brace, entries);
    }

    // ─── String interpolation ─────────────────────────────────────────────────

    private Expr ParseInterpolatedString(Token token, string pattern)
    {
        var parts  = new List<Expr>();
        int lastPos = 0;
        int i       = 0;

        while (i < pattern.Length)
        {
            if (pattern[i] != '{') { i++; continue; }

            if (i > lastPos)
                parts.Add(new Expr.Literal(pattern[lastPos..i]));

            i++;
            int start      = i;
            int braceCount = 1;
            while (i < pattern.Length && braceCount > 0)
            {
                if (pattern[i] == '{')      braceCount++;
                else if (pattern[i] == '}') braceCount--;
                if (braceCount > 0) i++;
            }

            if (i >= pattern.Length) throw Error(token, "Unterminated interpolation.");

            string exprText = pattern[start..i];
            var subParser   = new Parser(new BadLang.Lexer.Lexer(exprText).ScanTokens());
            parts.Add(subParser.Expression());

            i++;
            lastPos = i;
        }

        if (lastPos < pattern.Length)
            parts.Add(new Expr.Literal(pattern[lastPos..]));

        return new Expr.InterpolatedString(token, parts);
    }

    // ─── Type parsing ─────────────────────────────────────────────────────────

    private TypeNode ParseType()
    {
        TypeNode type;

        if (Match(TokenType.BoolType, TokenType.StringType, TokenType.CharType,
                  TokenType.NumType,  TokenType.AnyType,    TokenType.VoidType))
        {
            type = new TypeNode.Primitive(Previous());
        }
        else
        {
            Token name = Consume(TokenType.Identifier, "Expect type name.");
            if (Match(TokenType.Less))
            {
                var args = new List<TypeNode>();
                do { args.Add(ParseType()); } while (Match(TokenType.Comma));
                Consume(TokenType.Greater, "Expect '>' after type arguments.");
                type = new TypeNode.Generic(name, args);
            }
            else
            {
                type = new TypeNode.UserDefined(name);
            }
        }

        while (Match(TokenType.OpenBracket))
        {
            Consume(TokenType.CloseBracket, "Expect ']' after type name.");
            type = new TypeNode.Array(type);
        }

        return type;
    }

    private List<Parameter> ParseParameters()
    {
        var parameters = new List<Parameter>();
        if (Check(TokenType.CloseParen)) return parameters;

        do
        {
            TypeNode? type;
            Token name;

            if (IsTypeStart())
            {
                type = ParseType();
                name = Consume(TokenType.Identifier, "Expect parameter name.");
            }
            else
            {
                name = Consume(TokenType.Identifier, "Expect parameter name.");
                type = Match(TokenType.Colon) ? ParseType() : null;
            }

            parameters.Add(new Parameter(name, type));
        } while (Match(TokenType.Comma));

        return parameters;
    }

    private List<Token> ParseGenericParameters()
    {
        var generics = new List<Token>();
        if (!Match(TokenType.Less)) return generics;
        do { generics.Add(Consume(TokenType.Identifier, "Expect generic parameter name.")); }
        while (Match(TokenType.Comma));
        Consume(TokenType.Greater, "Expect '>' after generic parameters.");
        return generics;
    }

    // ─── Grammar combinators ──────────────────────────────────────────────────

    /// <summary>Parses a left-associative chain of binary <see cref="Expr.Binary"/> nodes.</summary>
    private Expr ParseBinary(Func<Expr> operand, params TokenType[] ops)
    {
        Expr expr = operand();
        while (Match(ops))
        {
            Token op    = Previous();
            Expr  right = operand();
            expr = new Expr.Binary(expr, op, right);
        }
        return expr;
    }

    /// <summary>Parses a left-associative chain of <see cref="Expr.Logical"/> nodes.</summary>
    private Expr ParseLogical(Func<Expr> operand, params TokenType[] ops)
    {
        Expr expr = operand();
        while (Match(ops))
        {
            Token op    = Previous();
            Expr  right = operand();
            expr = new Expr.Logical(expr, op, right);
        }
        return expr;
    }

    /// <summary>Parses an expression optionally wrapped in parentheses.</summary>
    private T ParseOptionallyParenthesized<T>(Func<T> inner)
    {
        bool hasParen = Match(TokenType.OpenParen);
        T result      = inner();
        if (hasParen) Consume(TokenType.CloseParen, "Expect ')' after condition.");
        return result;
    }

    /// <summary>Collects items produced by <paramref name="item"/> until a stop token is reached.</summary>
    private List<T> ParseUntil<T>(TokenType stop, Func<T> item)
    {
        var list = new List<T>();
        while (!Check(stop) && !IsAtEnd())
            list.Add(item());
        return list;
    }

    /// <summary>Overload that stops on any of several stop tokens.</summary>
    private List<T> ParseUntil<T>(TokenType stop1, TokenType stop2, TokenType stop3, Func<T> item)
    {
        var list = new List<T>();
        while (!Check(stop1) && !Check(stop2) && !Check(stop3) && !IsAtEnd())
            list.Add(item());
        return list;
    }

    // ─── Token stream helpers ─────────────────────────────────────────────────

    private bool IsTypeStart() =>
        Check(TokenType.BoolType)  || Check(TokenType.StringType) ||
        Check(TokenType.CharType)  || Check(TokenType.NumType)    ||
        Check(TokenType.AnyType)   || Check(TokenType.VoidType)   ||
        (Check(TokenType.Identifier) && PeekNext().Type == TokenType.Less);

    private bool Match(params TokenType[] types)
    {
        foreach (var type in types)
        {
            if (!Check(type)) continue;
            Advance();
            return true;
        }
        return false;
    }

    private Token ConsumeModuleSegment(string message)
    {
        if (Check(TokenType.Identifier) || Check(TokenType.StringType) || Check(TokenType.NumType) || Check(TokenType.BoolType))
            return Advance();
        throw Error(Peek(), message);
    }

    private Token Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();
        throw Error(Peek(), message);
    }

    private bool Check(TokenType type) => !IsAtEnd() && Peek().Type == type;

    private Token Advance()
    {
        if (!IsAtEnd()) _current++;
        return Previous();
    }

    private bool  IsAtEnd()  => Peek().Type == TokenType.EOF;
    private Token Peek()     => _tokens[_current];
    private Token Previous() => _tokens[_current - 1];
    private Token PeekNext() => IsAtEnd() ? _tokens[_current] : _tokens[_current + 1];

    private ParseError Error(Token token, string message) => new(token, message);

    private void Synchronize()
    {
        Advance();
        while (!IsAtEnd())
        {
            if (Previous().Type == TokenType.Semicolon) return;
            switch (Peek().Type)
            {
                case TokenType.Class:
                case TokenType.Fn:
                case TokenType.Var:
                case TokenType.For:
                case TokenType.If:
                case TokenType.While:
                case TokenType.Return:
                    return;
            }
            Advance();
        }
    }
}