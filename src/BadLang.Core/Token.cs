namespace BadLang.Core;

public enum TokenType
{
    // Keywords
    Var, Const, Export, If, Else, Switch, Case, Default, While, Do, For, In, 
    Break, Continue, Return, Fn, Class, Interface, Struct, Enum,
    Try, Catch, Finally, Throw, New, Import, As, This, Super,
    
    // Primitive Types
    BoolType, StringType, CharType, NumType, AnyType, VoidType,

    // Literals
    Identifier, Number, String, InterpolatedString, True, False, Null,

    // Operators
    Plus, Minus, Star, Slash, Percent, Caret,
    PlusPlus, MinusMinus,
    Equal, EqualEqual, Bang, BangEqual,
    Less, LessEqual, Greater, GreaterEqual,
    And, Or, BangLog, // !
    Ampersand, Pipe, // & |
    LessLess, GreaterGreater, // << >>
    Question, Colon, QuestionQuestion, // ? : ??
    Arrow, // =>
    Dot, DotDot, DotDotEqual, // . .. ..=

    // Compound Assignments
    PlusEqual, MinusEqual, StarEqual, SlashEqual, PercentEqual,

    // Built-in helpers
    ToString, TypeOf, NameOf, ToNumber, IsNull, Assert, Panic,

    // Punctuation
    OpenParen, CloseParen, OpenBrace, CloseBrace,
    OpenBracket, CloseBracket, // [ ]
    Semicolon, Comma,

    // Special
    EOF, Unknown, Comment
}

public record Token(TokenType Type, string Lexeme, object? Literal, int Line, int Column, int Offset);

