# BadLang

<p align="center">
  <img src="editor/bad-lang-vscode/images/logo.jpg" alt="BadLang Logo" width="128">
</p>

```text
  ____                _   _                             
 | __ )    __ _    __| | | |       __ _   _ __     __ _ 
 |  _ \   / _` |  / _` | | |      / _` | | '_ \   / _` |
 | |_) | | (_| | | (_| | | |___  | (_| | | | | | | (_| |
 |____/   \__,_|  \__,_| |_____|  \__,_| |_| |_|  \__, |
                                                  |___/ 
```

**BadLang** is a fast, flexible, C-style programming language built from the ground up for performance and developer experience. It features a tree-walking interpreter, a bytecode VM, and an LLVM backend for native compilation.

## ✨ Features

- **Modern C-Style Syntax**: Familiar and intuitive.
- **Strong & Dynamic**: A hybrid type system that gives you the best of both worlds.
- **First-Class Functions**: Functions are citizens with support for closures.
- **Object-Oriented**: Classes, inheritance, and encapsulation.
- **Module System**: Organize your code with `import` and `export`.
- **Developer Experience**: 
  - Full **LSP (Language Server Protocol)** support.
  - Comprehensive VS Code Extension.
  - Interactive REPL.
- **High Performance**: Native compilation via **LLVM**.

## 🚀 Getting Started

### Installation

#### 1. CLI (Compiler & Interpreter)
Ensure you have the [.NET 10.0 SDK](https://dotnet.microsoft.com/download) installed.

```bash
# Clone the repository
git clone https://github.com/yousefelsharkawy26/bad-lang.git
cd bad-lang

# Run the REPL
dotnet run --project src/BadLang.Cli

# Install the CLI tool globally (optional)
dotnet pack src/BadLang.Cli -o ./nupkg
dotnet tool install --global --add-source ./nupkg BadLang.Cli
```

#### 2. VS Code Extension
Install the **Bad Lang** extension from the Marketplace or build it locally:

```bash
cd editor/bad-lang-vscode
npm install
npm run compile
# Then open VS Code and use 'F5' to launch the extension development host
```

### Usage

```bash
# Run a script
badlang main.bad

# Check for errors
badlang check main.bad

# Build a native executable
badlang build main.bad
```

## 📖 Language Syntax

### Variables & Constants
```bad
var name = "Antigravity";
const version = 1.1;
var isActive = true;
```

### Functions
```bad
fn greet(name) {
    return "Hello " + name;
}

// Or using the 'function' keyword
function add(a, b) {
    return a + b;
}

print(greet("User"));
```

### Control Flow
```bad
if (x > 10) {
    print("Large");
} else {
    print("Small");
}

for (item in list) {
    print(item);
}

while (i < 5) {
    i = i + 1;
}
```

### Classes & OOP
```bad
class Animal {
    fn speak() { print("..."); }
}

class Dog : Animal {
    fn speak() { print("Woof!"); }
}

var d = new Dog();
d.speak();
```

### Error Handling
```bad
try {
    throw "Something went wrong";
} catch (e) {
    print("Error: " + e);
} finally {
    print("Cleanup");
}
```

### Modules
```bad
// math.bad
export fn add(a, b) { return a + b; }

// main.bad
import math;
print(math.add(1, 2));
```

## 🛠️ Development

### Building the Project
```bash
dotnet build BadLang.slnx
```

### Running Tests
```bash
dotnet test
```

## 📄 License
This project is licensed under the MIT License.
