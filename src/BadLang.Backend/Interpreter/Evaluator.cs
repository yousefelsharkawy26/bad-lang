using System.Collections;
using System.Collections.Generic;
using BadLang.Backend.Interpreter.Runtime;

namespace BadLang.Backend.Interpreter;

public static class Evaluator
{
    public static object? EvaluateUnary(string op, object? right)
    {
        return op switch
        {
            "-" => -(double)right!,
            "!" => !IsTruthy(right),
            "isNull" => right == null,
            "toString" => right?.ToString() ?? "null",
            "typeof" => GetTypeString(right),
            "get_enumerator" => GetEnumerator(right),
            "enumerator_next" => EnumeratorNext(right),
            "enumerator_current" => EnumeratorCurrent(right),
            "toNumber" => double.Parse(right?.ToString() ?? "0"),
            _ => null
        };
    }

    public static string GetTypeString(object? obj)
    {
        if (obj == null) return "null";
        if (obj is double) return "number";
        if (obj is bool) return "bool";
        if (obj is string) return "string";
        if (obj is IList) return "list";
        if (obj is IDictionary) return "map";
        if (obj is IBadLangCallable) return "function";
        return "object";
    }

    public static object? GetEnumerator(object? obj)
    {
        if (obj is IEnumerable enumerable)
            return enumerable.GetEnumerator();
        throw new Exception("Object is not enumerable.");
    }

    public static bool EnumeratorNext(object? enumerator)
    {
        if (enumerator is IEnumerator e)
            return e.MoveNext();
        return false;
    }

    public static object? EnumeratorCurrent(object? enumerator)
    {
        if (enumerator is IEnumerator e)
            return e.Current;
        return null;
    }

    public static object? EvaluateBinary(string op, object? left, object? right)
    {
        return op switch
        {
            "+" => (left is string || right is string) 
                ? (left?.ToString() ?? "null") + (right?.ToString() ?? "null")
                : (double)left! + (double)right!,
            "-" => (double)left! - (double)right!,
            "*" => (double)left! * (double)right!,
            "/" => (double)left! / (double)right!,
            "%" => (double)left! % (double)right!,
            ">" => (double)left! > (double)right!,
            ">=" => (double)left! >= (double)right!,
            "<" => (double)left! < (double)right!,
            "<=" => (double)left! <= (double)right!,
            "==" => IsEqual(left, right),
            "!=" => !IsEqual(left, right),
            "&&" => IsTruthy(left) && IsTruthy(right),
            "||" => IsTruthy(left) || IsTruthy(right),
            _ => null
        };
    }

    public static bool IsTruthy(object? obj)
    {
        if (obj == null) return false;
        if (obj is bool b) return b;
        if (obj is double d) return d != 0;
        return true;
    }

    public static bool IsEqual(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null) return false;
        return a.Equals(b);
    }
}
