using System;
using System.Collections.Generic;

namespace BadLang.Semantic
{
    public class Symbol
    {
        public required string Name { get; set; }
        public required string Type { get; set; }
        public bool IsGlobal { get; set; }
    }

    public class SymbolTable
    {
        private readonly Dictionary<string, Symbol> _symbols = new Dictionary<string, Symbol>();
        private readonly SymbolTable? _parent;

        public SymbolTable(SymbolTable? parent = null)
        {
            _parent = parent;
        }

        public void Define(Symbol symbol)
        {
            _symbols[symbol.Name] = symbol;
        }

        public Symbol? Resolve(string name)
        {
            if (_symbols.TryGetValue(name, out var symbol))
                return symbol;

            return _parent?.Resolve(name);
        }
    }
}
