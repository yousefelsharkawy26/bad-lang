using System;
using System.Collections.Generic;
using System.Linq;
using BadLang.Parser;
using BadLang.Parser.Ast;

namespace BadLang.IR.Handlers.Statements
{
    public class ExportStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.Export);
        public void Build(Stmt stmt, List<IrNode> ir, IIRBuilderContext context)
        {
            var exportStmt = (Stmt.Export)stmt;
            // Build the inner declaration first
            context.BuildStmt(exportStmt.Declaration, ir);
            
            // Then emit an export node for the name
            string? name = context.GetExportName(exportStmt.Declaration);
            if (name != null)
            {
                ir.Add(new IrExport { Name = name });
            }
        }
    }

    public class ExportListStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.ExportList);
        public void Build(Stmt stmt, List<IrNode> ir, IIRBuilderContext context)
        {
            var exportListStmt = (Stmt.ExportList)stmt;
            foreach (var symbol in exportListStmt.Symbols)
            {
                ir.Add(new IrExport { Name = symbol.Lexeme });
            }
        }
    }

    public class ImportStmtHandler : IStmtBuildHandler
    {
        public Type TargetType => typeof(Stmt.Import);
        public void Build(Stmt stmt, List<IrNode> ir, IIRBuilderContext context)
        {
            var importStmt = (Stmt.Import)stmt;
            var pathStr = string.Join(".", importStmt.Path.Select(t => t.Lexeme));
            string? alias = importStmt.Alias?.Lexeme;
            
            // If no alias is provided, we might still want to default to the path string 
            // for non-stdlib imports, but let's prioritize the explicit alias first.
            if (alias == null && !pathStr.StartsWith("stdlib."))
            {
                alias = pathStr;
            }

            ir.Add(new IrImport 
            { 
                Path = pathStr, 
                Alias = alias,
                Symbols = importStmt.Symbols?.Select(t => t.Lexeme).ToList()
            });
        }
    }
}

