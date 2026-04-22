using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BadLang.Core;
using BadLang.Lexer;

namespace BadLang.Parser;

public class ModuleLoader
{
    private readonly Dictionary<string, IReadOnlyList<Stmt>> _cache = new();
    private readonly string _basePath;

    public string BasePath => _basePath;

    public ModuleLoader(string basePath)
    {
        _basePath = basePath;
    }

    public string? Resolve(string path)
    {
        string relativePath = path.Replace('.', Path.DirectorySeparatorChar) + ".bad";
        string fullPath = Path.Combine(_basePath, relativePath);

        if (File.Exists(fullPath)) return fullPath;

        if (path.StartsWith("stdlib."))
        {
            string modulePath = path.Substring(7).Replace('.', Path.DirectorySeparatorChar) + ".bad";
            string? searchDir = _basePath;
            while (searchDir != null)
            {
                string stdlibRoot = Path.Combine(searchDir, "stdlib");
                if (Directory.Exists(stdlibRoot))
                {
                    // 1. Try direct path relative to stdlib root
                    string directPath = Path.Combine(stdlibRoot, modulePath);
                    if (File.Exists(directPath)) return directPath;

                    // 2. Try subdirectories
                    string moduleName = Path.GetFileName(modulePath);
                    string[] subDirs = { "core", "collections", "system" };
                    foreach (var subDir in subDirs)
                    {
                        string candidate = Path.Combine(stdlibRoot, subDir, moduleName);
                        if (File.Exists(candidate)) return candidate;
                    }
                }
                searchDir = Path.GetDirectoryName(searchDir);
            }
        }

        string? currentDir = _basePath;
        while (currentDir != null)
        {
            string candidate = Path.Combine(currentDir, relativePath);
            if (File.Exists(candidate)) return candidate;
            currentDir = Path.GetDirectoryName(currentDir);
        }

        return null;
    }

    public IReadOnlyList<Stmt> LoadModule(IReadOnlyList<Token> path)
    {
        string? fullPath = Resolve(string.Join(".", path.Select(t => t.Lexeme)));
        if (fullPath == null)
        {
             throw new CompileError($"Module not found: {string.Join(".", path.Select(t => t.Lexeme))}", path.First());
        }

        if (_cache.TryGetValue(fullPath, out var cached))
        {
            return cached;
        }

        string source = File.ReadAllText(fullPath);
        var lexer = new BadLang.Lexer.Lexer(source);
        var tokens = lexer.ScanTokens();
        var parser = new Parser(tokens);
        var statements = parser.Parse();

        _cache[fullPath] = statements;
        return statements;
    }
}
