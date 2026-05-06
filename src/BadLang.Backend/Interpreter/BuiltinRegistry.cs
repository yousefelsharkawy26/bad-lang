using BadLang.Backend.Interpreter.Runtime;
using BadLang.Core;

namespace BadLang.Backend.Interpreter;

public static class BuiltinRegistry
{
    public static void DefineBuiltins(Environment globals, TextWriter output, HttpClient httpClient)
    {
        globals.Define("print", new NativeFunction(args =>
        {
            for (int i = 0; i < args.Count; i++)
            {
                output.Write(args[i] + (i < args.Count - 1 ? " " : ""));
            }
            return null;
        }));

        globals.Define("println", new NativeFunction(args =>
        {
            for (int i = 0; i < args.Count; i++)
            {
                output.Write(args[i] + (i < args.Count - 1 ? " " : ""));
            }
            output.WriteLine();
            return null;
        }));

        globals.Define("input", new NativeFunction(args =>
        {
            if (args.Count > 0) output.Write(args[0]?.ToString());
            return Console.ReadLine();
        }));

        globals.Define("len", new NativeFunction(args =>
        {
            if (args.Count == 0) return 0.0;
            if (args[0] is string s) return (double)s.Length;
            if (args[0] is List<object?> l) return (double)l.Count;
            return 0.0;
        }));

        globals.Define("str", new NativeFunction(args =>
        {
            if (args.Count == 0 || args[0] == null) return "null";
            if (args[0] is List<object?> list)
            {
                return "[" + string.Join(", ", list.Select(v => v?.ToString() ?? "null")) + "]";
            }
            return args[0]?.ToString() ?? "null";
        }));

        globals.Define("toString", new NativeFunction(args =>
        {
            if (args.Count == 0 || args[0] == null) return "null";
            if (args[0] is List<object?> list)
                return "[" + string.Join(", ", list.Select(v => v?.ToString() ?? "null")) + "]";
            return args[0]?.ToString() ?? "null";
        }));

        globals.Define("num", new NativeFunction(args =>
        {
            if (args.Count == 0) return 0.0;
            if (double.TryParse(args[0]?.ToString(), out double result)) return result;
            return 0.0;
        }));
        globals.Define("toNumber", new NativeFunction(args =>
        {
            if (args.Count == 0) return 0.0;
            if (double.TryParse(args[0]?.ToString(), out double result)) return result;
            return 0.0;
        }));

        // Math Primitives
        globals.Define("math_sin", new NativeFunction(args => Math.Sin(Convert.ToDouble(args[0]))));
        globals.Define("math_cos", new NativeFunction(args => Math.Cos(Convert.ToDouble(args[0]))));
        globals.Define("math_tan", new NativeFunction(args => Math.Tan(Convert.ToDouble(args[0]))));
        globals.Define("math_sqrt", new NativeFunction(args => Math.Sqrt(Convert.ToDouble(args[0]))));
        globals.Define("math_pow", new NativeFunction(args => Math.Pow(Convert.ToDouble(args[0]), Convert.ToDouble(args[1]))));
        globals.Define("math_floor", new NativeFunction(args => Math.Floor(Convert.ToDouble(args[0]))));
        globals.Define("math_ceil", new NativeFunction(args => Math.Ceiling(Convert.ToDouble(args[0]))));
        globals.Define("math_round", new NativeFunction(args => Math.Round(Convert.ToDouble(args[0]))));
        globals.Define("math_abs", new NativeFunction(args => Math.Abs(Convert.ToDouble(args[0]))));

        // String Primitives
        globals.Define("str_len", new NativeFunction(args => (double)(args[0]?.ToString()?.Length ?? 0)));
        globals.Define("str_lower", new NativeFunction(args => args[0]?.ToString()?.ToLower() ?? ""));
        globals.Define("str_upper", new NativeFunction(args => args[0]?.ToString()?.ToUpper() ?? ""));
        globals.Define("str_substring", new NativeFunction(args =>
        {
            var s = args[0]?.ToString() ?? "";
            var start = Convert.ToInt32(args[1]);
            var len = Convert.ToInt32(args[2]);
            if (start < 0 || start >= s.Length) return "";
            if (start + len > s.Length) len = s.Length - start;
            return s.Substring(start, len);
        }));
        globals.Define("str_replace", new NativeFunction(args => (args[0]?.ToString() ?? "").Replace(args[1]?.ToString() ?? "", args[2]?.ToString() ?? "")));
        globals.Define("str_split", new NativeFunction(args => (args[0]?.ToString() ?? "").Split(args[1]?.ToString() ?? "").Cast<object?>().ToList()));

        // List Primitives
        globals.Define("list_push", new NativeFunction(args =>
        {
            if (args[0] is List<object?> list) list.Add(args[1]);
            return null;
        }));
        globals.Define("list_pop", new NativeFunction(args =>
        {
            if (args[0] is List<object?> { Count: > 0 } list)
            {
                var item = list[^1];
                list.RemoveAt(list.Count - 1);
                return item;
            }
            return null;
        }));
        globals.Define("list_remove_at", new NativeFunction(args =>
        {
            if (args[0] is List<object?> list)
            {
                var index = Convert.ToInt32(args[1]);
                if (index >= 0 && index < list.Count) list.RemoveAt(index);
            }
            return null;
        }));
        globals.Define("list_clear", new NativeFunction(args =>
        {
            if (args[0] is List<object?> list) list.Clear();
            return null;
        }));
        globals.Define("list_length", new NativeFunction(args =>
        {
            if (args[0] is List<object?> list) return (double)list.Count;
            return 0.0;
        }));
        globals.Define("list_contains", new NativeFunction(args =>
        {
            if (args[0] is List<object?> list) return list.Contains(args[1]);
            return false;
        }));
        globals.Define("list_index_of", new NativeFunction(args =>
        {
            if (args[0] is List<object?> list) return (double)list.IndexOf(args[1]);
            return -1.0;
        }));
        globals.Define("list_reverse", new NativeFunction(args =>
        {
            if (args[0] is List<object?> list) list.Reverse();
            return null;
        }));

        // IO Primitives
        globals.Define("io_read_file", new NativeFunction(args =>
        {
            var path = args[0]?.ToString() ?? "";
            if (!File.Exists(path)) return null;
            return File.ReadAllText(path);
        }));
        globals.Define("io_write_file", new NativeFunction(args =>
        {
            var path = args[0]?.ToString() ?? "";
            var content = args[1]?.ToString() ?? "";
            File.WriteAllText(path, content);
            return null;
        }));
        globals.Define("io_append_file", new NativeFunction(args =>
        {
            var path = args[0]?.ToString() ?? "";
            var content = args[1]?.ToString() ?? "";
            File.AppendAllText(path, content);
            return null;
        }));
        globals.Define("io_file_exists", new NativeFunction(args =>
        {
            var path = args[0]?.ToString() ?? "";
            return File.Exists(path);
        }));
        globals.Define("io_delete_file", new NativeFunction(args =>
        {
            var path = args[0]?.ToString() ?? "";
            if (File.Exists(path)) File.Delete(path);
            return null;
        }));
        globals.Define("io_read_lines", new NativeFunction(args =>
        {
            var path = args[0]?.ToString() ?? "";
            if (!File.Exists(path)) return new List<object?>();
            return File.ReadAllLines(path).Cast<object?>().ToList();
        }));

        // Time Primitives
        globals.Define("time_now", new NativeFunction(
            _ => (double)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
        );
        globals.Define("time_sleep", new NativeFunction(args =>
        {
            var ms = Convert.ToInt32(args[0]);
            Thread.Sleep(ms);
            return null;
        }));

        // Map Primitives
        globals.Define("map_new", new NativeFunction(_ => new Dictionary<object, object?>()));
        globals.Define("map_set", new NativeFunction(args =>
        {
            if (args[0] is Dictionary<object, object?> map && args[1] is { } key)
                map[key] = args[2];
            return null;
        }));
        globals.Define("map_get", new NativeFunction(args =>
        {
            if (args[0] is Dictionary<object, object?> map && args[1] is { } key)
                return map.GetValueOrDefault(key);
            return null;
        }));
        globals.Define("map_has", new NativeFunction(args =>
        {
            if (args[0] is Dictionary<object, object?> map && args[1] is { } key)
                return map.ContainsKey(key);
            return false;
        }));
        globals.Define("map_remove", new NativeFunction(args =>
        {
            if (args[0] is Dictionary<object, object?> map && args[1] is { } key)
                map.Remove(key);
            return null;
        }));
        globals.Define("map_keys", new NativeFunction(args =>
        {
            if (args[0] is Dictionary<object, object?> map)
                return map.Keys.Cast<object?>().ToList();
            return new List<object?>();
        }));
        globals.Define("map_values", new NativeFunction(args =>
        {
            if (args[0] is Dictionary<object, object?> map)
                return map.Values.ToList();
            return new List<object?>();
        }));
        globals.Define("map_size", new NativeFunction(args =>
        {
            if (args[0] is Dictionary<object, object?> map)
                return (double)map.Count;
            return 0.0;
        }));

        // Type Utilities
        globals.Define("typeof", new NativeFunction(
            args => Evaluator.GetTypeString(args.Count > 0 ? args[0] : null))
        );
        globals.Define("toInt", new NativeFunction(
            args =>
        {
            if (args.Count == 0) return 0.0;
            return (double)(long)Convert.ToDouble(args[0]);
        }));
        globals.Define("toFloat", new NativeFunction(args =>
        {
            if (args.Count == 0) return 0.0;
            return Convert.ToDouble(args[0]);
        }));

        globals.Define("assert", new NativeFunction(args =>
        {
            if (args.Count == 0 || !Evaluator.IsTruthy(args[0]))
            {
                var msg = args.Count > 1 ? args[1]?.ToString() ?? "Assertion failed" : "Assertion failed";
                throw new RuntimeException(
                    new Token(TokenType.Identifier, "assert", null, 0, 0, 0), msg);
            }
            return null;
        }));

        // OS Primitives
        globals.Define("os_getenv", new NativeFunction(args =>
        {
            if (args.Count > 0 && args[0] is string name)
                return System.Environment.GetEnvironmentVariable(name);
            return null;
        }));
        globals.Define("os_exit", new NativeFunction(args =>
        {
            int code = args.Count > 0 ? (int)Convert.ToDouble(args[0]) : 0;
            System.Environment.Exit(code);
            return null;
        }));
        globals.Define("os_platform", new NativeFunction(_ =>
        {
            if (OperatingSystem.IsWindows()) return "windows";
            if (OperatingSystem.IsMacOS()) return "macos";
            if (OperatingSystem.IsLinux()) return "linux";
            return "unknown";
        }));
        globals.Define("os_args", new NativeFunction(_ =>
        {
            return System.Environment.GetCommandLineArgs().Select(x => (object?)x).ToList();
        }));

        // Net Primitives
        globals.Define("net_http_get", new NativeFunction(args =>
        {
            if (args.Count > 0 && args[0] is string url)
            {
                try
                {
                    return httpClient.GetStringAsync(url).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    throw new RuntimeException(null!, $"HTTP Request failed: {ex.Message}");
                }
            }
            return null;
        }));
    }
}
