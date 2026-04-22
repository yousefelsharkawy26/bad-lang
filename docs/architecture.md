# BadLang Architecture

The BadLang compiler and toolchain are organized into several modular projects.

## Project Structure
- **BadLang.Lexer**: Tokenizes source code.
- **BadLang.Parser**: Converts tokens into an Abstract Syntax Tree (AST).
- **BadLang.Semantic**: Performs type checking and semantic analysis.
- **BadLang.IR**: Generates Intermediate Representation (IR).
- **BadLang.Backend**: Translates IR into target code (LLVM or Interpreted).
- **BadLang.Core**: Shared types and abstractions (Token, CompileError).
- **BadLang.Runtime**: C-based runtime and memory management (GC).
- **BadLang.Cli**: Command-line interface for running and building projects.

## Workflow
1. **Source** -> Lexer -> **Tokens**
2. **Tokens** -> Parser -> **AST**
3. **AST** -> Semantic Analyzer -> **Validated AST**
4. **Validated AST** -> IR Builder -> **IR**
5. **IR** -> LLVM Backend -> **Native Object Code**
   OR
   **IR** -> Interpreter -> **Execution**
