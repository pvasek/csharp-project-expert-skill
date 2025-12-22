---
name: csharp-project-expert
description: |
  Expert C# project and solution analysis, navigation, and refactoring tool. Find symbol
  definitions/references, safely rename across solutions, navigate type hierarchies, analyze
  dependencies and call graphs, generate code, and check compilation errors. Understands C#
  semantics and project structure. Replaces slow grep/text searches with precise, compiler-accurate
  operations. Essential for refactoring, code quality, and understanding large C# projects.
compatibility: |
  Requires .NET 10.0 runtime. Works with C# solutions (.sln) or projects (.csproj).
  Executable location: scripts/CSharpExpertCli (macOS/Linux) or scripts/CSharpExpertCli.exe (Windows).
  All commands require --solution or --project path. Supports C# 1.0-12.0.
license: MIT
---

# C# Project Expert

Expert code analysis and refactoring for C# projects using compiler-accurate semantic understanding.

## Quick Start

Verify the tool is working:

```bash
./scripts/CSharpExpertCli --help
```

Try your first command:

```bash
# Find where a class is defined
./scripts/CSharpExpertCli --solution /path/to/YourSolution.sln find-definition UserService --type class

# Find all references to a method
./scripts/CSharpExpertCli --solution /path/to/YourSolution.sln find-references GetById --type method
```

All commands support JSON, text, and markdown output via `--output` or `-o`:

```bash
./scripts/CSharpExpertCli -s MySolution.sln -o json find-definition UserService --type class
```

## When to Use This Skill

**Use this skill when:**
- Finding where C# symbols (classes, methods, interfaces) are defined
- Finding all usages/references of a symbol before refactoring
- Safely renaming symbols across an entire solution
- Understanding method signatures, parameters, and return types
- Checking for compilation errors before committing changes
- Finding implementations of interfaces or abstract classes
- Analyzing method call hierarchies (who calls what)
- Exploring type members without reading entire files
- Working with C# projects larger than 10 files
- Need precise, compiler-accurate results vs. text search

**Don't use this skill when:**
- Working with non-C# files (JavaScript, Python, etc.)
- Simple text searches in comments or strings
- Project doesn't have .sln or .csproj file
- .NET runtime not available in environment
- Need to analyze code without compiling (tool requires buildable code)

## Core Commands

All commands follow this pattern:

```bash
./scripts/CSharpExpertCli --solution <path> [--output format] <command> [options] <arguments>
```

### Global Options

- `--solution, -s <path>` - Path to .sln file (required)
- `--project, -p <path>` - Path to .csproj file (alternative)
- `--output, -o <format>` - Output format: `json`, `text`, or `markdown` (default: json)
- `--verbose, -v` - Enable verbose logging

### 1. find-definition

Find where a symbol is defined (class, method, property, interface, etc.).

```bash
# Find a class
./scripts/CSharpExpertCli -s MySolution.sln find-definition UserService --type class

# Find a method in a specific namespace
./scripts/CSharpExpertCli -s MySolution.sln find-definition GetById \
  --type method \
  --in-namespace MyApp.Services
```

**Options:**
- `--type, -t` - Symbol type: `class`, `method`, `property`, `field`, `interface`, `enum`
- `--in-file, -f` - Search only in specific file
- `--in-namespace, -n` - Search only in specific namespace

**Output (JSON):**
```json
{
  "symbol": "UserService",
  "kind": "class",
  "location": {
    "file": "src/Services/UserService.cs",
    "line": 15,
    "column": 18
  },
  "namespace": "MyApp.Services",
  "accessibility": "public"
}
```

### 2. find-references

Find all usages of a symbol throughout the solution.

```bash
# Find all references to a method
./scripts/CSharpExpertCli -s MySolution.sln find-references GetById --type method

# Find all references to an interface
./scripts/CSharpExpertCli -s MySolution.sln find-references IUserRepository --type interface
```

**Options:**
- `--type, -t` - Symbol type to search for
- `--in-namespace, -n` - Symbol namespace

**Use case:** Critical before refactoring to understand impact.

### 3. rename

Safely rename a symbol across the entire solution with preview mode.

```bash
# Preview changes (don't apply)
./scripts/CSharpExpertCli -s MySolution.sln rename UserService UserManager \
  --type class \
  --preview

# Apply rename and update file name
./scripts/CSharpExpertCli -s MySolution.sln rename UserService UserManager \
  --type class \
  --rename-file

# Rename a method
./scripts/CSharpExpertCli -s MySolution.sln rename GetById FindById --type method
```

**Options:**
- `--type, -t` - Type of symbol being renamed
- `--in-namespace, -n` - Limit scope to namespace
- `--preview` - Show changes without applying them (recommended first step)
- `--rename-file` - Also rename the file if renaming a type

**Important:** Always use `--preview` first to see what will change!

### 4. signature

Get method/type signatures with parameters and return types.

```bash
# Get method signature with all overloads
./scripts/CSharpExpertCli -s MySolution.sln signature GetById \
  --type method \
  --include-overloads

# Get class signature
./scripts/CSharpExpertCli -s MySolution.sln signature UserService --type class
```

**Options:**
- `--type, -t` - Type of symbol
- `--include-overloads` - Show all overloads for methods
- `--include-docs` - Include XML documentation comments (default: true)

### 5. diagnostics

Check for compilation errors, warnings, and messages.

```bash
# Get all errors
./scripts/CSharpExpertCli -s MySolution.sln diagnostics --severity error

# Get diagnostics for specific file
./scripts/CSharpExpertCli -s MySolution.sln diagnostics \
  --file src/UserService.cs \
  --severity warning

# Filter by diagnostic code
./scripts/CSharpExpertCli -s MySolution.sln diagnostics --code CS0246
```

**Options:**
- `--severity, -s` - Filter by severity: `error`, `warning`, `info`
- `--file, -f` - Get diagnostics only for specific file
- `--code, -c` - Filter by diagnostic code (e.g., CS0246)

**Use case:** Validate code quality before committing changes.

### 6. find-implementations

Find all classes that implement an interface or inherit from an abstract class.

```bash
# Find implementations of an interface
./scripts/CSharpExpertCli -s MySolution.sln find-implementations IUserRepository

# Find all IDisposable implementations
./scripts/CSharpExpertCli -s MySolution.sln find-implementations IDisposable
```

**Use case:** Understand which classes implement a contract, useful for polymorphism analysis.

### 7. find-callers

Find all methods that call a specific method (who uses this?).

```bash
# Find what calls GetById
./scripts/CSharpExpertCli -s MySolution.sln find-callers GetById

# Find what calls ProcessOrder
./scripts/CSharpExpertCli -s MySolution.sln find-callers ProcessOrder
```

**Use case:** Impact analysis - understand what will be affected by changes.

### 8. list-members

List all members (methods, properties, fields) of a type.

```bash
# List all members of a class
./scripts/CSharpExpertCli -s MySolution.sln list-members UserService

# List only public methods
./scripts/CSharpExpertCli -s MySolution.sln list-members User \
  --kind method \
  --accessibility public
```

**Options:**
- `--kind, -k` - Filter by member kind: `method`, `property`, `field`, `event`
- `--accessibility, -a` - Filter by: `public`, `private`, `protected`, `internal`
- `--include-inherited` - Include inherited members

## Common Workflows

### Workflow 1: Safe Refactoring

When renaming a symbol:

```bash
# 1. Check current usage
./scripts/CSharpExpertCli -s MySolution.sln find-references GetById --type method

# 2. Preview rename to see what will change
./scripts/CSharpExpertCli -s MySolution.sln rename GetById FindById \
  --type method \
  --preview

# 3. Apply rename if preview looks good
./scripts/CSharpExpertCli -s MySolution.sln rename GetById FindById --type method

# 4. Verify no new errors
./scripts/CSharpExpertCli -s MySolution.sln diagnostics --severity error
```

### Workflow 2: Understanding a Type

When exploring an unfamiliar class:

```bash
# 1. Find where it's defined
./scripts/CSharpExpertCli -s MySolution.sln find-definition UserService --type class

# 2. See its members
./scripts/CSharpExpertCli -s MySolution.sln list-members UserService

# 3. Check inheritance
./scripts/CSharpExpertCli -s MySolution.sln inheritance-tree UserService

# 4. See what it depends on
./scripts/CSharpExpertCli -s MySolution.sln dependencies UserService
```

### Workflow 3: Impact Analysis

Before modifying a method:

```bash
# 1. Find who calls this method
./scripts/CSharpExpertCli -s MySolution.sln find-callers ProcessOrder

# 2. Find what this method calls
./scripts/CSharpExpertCli -s MySolution.sln find-callees ProcessOrder

# 3. Get the method signature
./scripts/CSharpExpertCli -s MySolution.sln signature ProcessOrder \
  --type method \
  --include-docs
```

### Workflow 4: Code Quality Check

Before committing:

```bash
# 1. Check for errors
./scripts/CSharpExpertCli -s MySolution.sln diagnostics --severity error

# 2. Check for warnings in changed files
./scripts/CSharpExpertCli -s MySolution.sln diagnostics \
  --file src/Services/UserService.cs \
  --severity warning

# 3. Find unused code (optional)
./scripts/CSharpExpertCli -s MySolution.sln unused-code
```

## Command Syntax Patterns

### Filtering by Symbol Type

Most commands support `--type` to filter results:
- `class` - Classes
- `method` - Methods
- `property` - Properties
- `field` - Fields
- `interface` - Interfaces
- `enum` - Enums

### Namespace Filtering

Use `--in-namespace` to limit scope:

```bash
./scripts/CSharpExpertCli -s MySolution.sln find-definition GetUser \
  --type method \
  --in-namespace MyApp.Services
```

### File Filtering

Use `--in-file` to search specific files:

```bash
./scripts/CSharpExpertCli -s MySolution.sln find-definition UserService \
  --type class \
  --in-file src/Services/UserService.cs
```

## Output Formats

### JSON (Default)

Machine-readable, ideal for automation:

```bash
./scripts/CSharpExpertCli -s MySolution.sln -o json find-definition UserService --type class
```

```json
{
  "symbol": "UserService",
  "kind": "class",
  "location": {
    "file": "src/Services/UserService.cs",
    "line": 15,
    "column": 18
  }
}
```

### Text

Human-readable, good for terminal output:

```bash
./scripts/CSharpExpertCli -s MySolution.sln -o text find-definition UserService --type class
```

```
Symbol: UserService
Kind: class
Location: src/Services/UserService.cs:15:18
Namespace: MyApp.Services
```

### Markdown

Formatted for documentation:

```bash
./scripts/CSharpExpertCli -s MySolution.sln -o markdown find-definition UserService --type class
```

## Troubleshooting

**Error: "Solution file not found"**
- Verify the path to your .sln file is correct
- Use absolute paths if relative paths aren't working

**Error: "Symbol not found"**
- Check the symbol name spelling
- Verify the symbol type matches (class vs. method)
- Try searching without namespace filter first

**Error: ".NET runtime not found"**
- Ensure .NET 10.0 runtime is installed: `dotnet --version`
- Download from: https://dotnet.microsoft.com/download

**Compilation errors in diagnostics**
- The tool requires the solution to compile successfully
- Fix compilation errors in your IDE first
- Tool uses the same Roslyn compiler as Visual Studio/Rider

## Additional Resources

For complete command reference, see [references/COMMANDS.md](references/COMMANDS.md)

For detailed workflows and best practices, see [references/WORKFLOWS.md](references/WORKFLOWS.md)

For technical architecture details, see [references/ARCHITECTURE.md](references/ARCHITECTURE.md)

For real-world examples, see [references/EXAMPLES.md](references/EXAMPLES.md)
