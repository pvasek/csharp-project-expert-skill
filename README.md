# C# Skill - Roslyn-Powered Code Analysis CLI

A command-line tool for C# code analysis and refactoring using Roslyn APIs. Provides fast, accurate, token-efficient operations for working with C# codebases.

## Features

- **18 Commands** across 7 categories for comprehensive code analysis
- **Symbol-aware operations** that understand C# semantics
- **Safe refactoring** with preview mode
- **Multiple output formats**: JSON, Text, Markdown
- **Fast and accurate** using Roslyn compiler APIs

## Installation

```bash
git clone <repository-url>
cd csharp-skill/src/CSharpSkill
dotnet build
```

## Usage

```bash
dotnet run -- [command] [options]

# Or after building, use the executable directly:
./bin/Debug/net10.0/csharp-skill [command] [options]
```

### Global Options

- `--solution, -s <path>` - Path to .sln file (required for most commands)
- `--project, -p <path>` - Path to .csproj file (alternative to solution)
- `--output, -o <format>` - Output format: json, text, or markdown (default: json)
- `--verbose, -v` - Enable verbose logging

## Commands

### Symbol Commands

#### find-definition
Find where a symbol (class, method, property, etc.) is defined.

```bash
csharp-skill -s MySolution.sln find-definition UserService --type class
csharp-skill -s MySolution.sln find-definition GetById --type method --in-namespace MyApp.Services
```

**Options:**
- `--type, -t` - Filter by symbol type: class, method, property, field, interface, enum
- `--in-file, -f` - Search only in specific file
- `--in-namespace, -n` - Search only in specific namespace

#### find-references
Find all references/usages of a symbol throughout the solution.

```bash
csharp-skill -s MySolution.sln find-references GetById --type method
csharp-skill -s MySolution.sln find-references IUserRepository --type interface
```

**Options:**
- `--type, -t` - Symbol type to search for
- `--in-namespace, -n` - Symbol namespace

#### signature
Get the signature and documentation of a symbol.

```bash
csharp-skill -s MySolution.sln signature GetById --type method --include-overloads
csharp-skill -s MySolution.sln signature UserService --type class
```

**Options:**
- `--type, -t` - Type of symbol
- `--include-overloads` - Show all overloads for methods
- `--include-docs` - Include XML documentation comments (default: true)

#### list-members
List all members (methods, properties, fields) of a type.

```bash
csharp-skill -s MySolution.sln list-members UserService
csharp-skill -s MySolution.sln list-members User --kind method --accessibility public
```

**Options:**
- `--kind, -k` - Filter by member kind: method, property, field, event
- `--accessibility, -a` - Filter by accessibility: public, private, protected, internal
- `--include-inherited` - Include inherited members

#### rename
Safely rename a symbol across the entire solution.

```bash
# Preview mode (show changes without applying)
csharp-skill -s MySolution.sln rename UserService UserManager --preview

# Apply rename
csharp-skill -s MySolution.sln rename UserService UserManager --type class --rename-file

# Rename method
csharp-skill -s MySolution.sln rename GetById FindById --type method
```

**Options:**
- `--type, -t` - Type of symbol being renamed
- `--in-namespace, -n` - Limit scope to namespace
- `--preview` - Show changes without applying them
- `--rename-file` - Also rename the file if renaming a type

### Compilation Commands

#### diagnostics
Get all compilation errors, warnings, and info messages.

```bash
csharp-skill -s MySolution.sln diagnostics --severity error
csharp-skill -s MySolution.sln diagnostics --file src/UserService.cs --severity warning
csharp-skill -s MySolution.sln diagnostics --code CS0246
```

**Options:**
- `--severity, -s` - Filter by severity: error, warning, info
- `--file, -f` - Get diagnostics only for specific file
- `--code, -c` - Filter by diagnostic code (e.g., CS0246)

#### check-symbol-exists
Quickly verify if a symbol exists and is accessible.

```bash
csharp-skill -s MySolution.sln check-symbol-exists UserDto --type class
csharp-skill -s MySolution.sln check-symbol-exists GetById --type method --in-namespace MyApp.Services
```

**Options:**
- `--type, -t` - Expected symbol type
- `--in-namespace, -n` - Expected namespace

### Type Hierarchy Commands

#### find-implementations
Find all implementations of an interface or abstract class.

```bash
csharp-skill -s MySolution.sln find-implementations IUserRepository
csharp-skill -s MySolution.sln find-implementations IDisposable
```

#### inheritance-tree
Show inheritance hierarchy (ancestors and descendants).

```bash
csharp-skill -s MySolution.sln inheritance-tree UserService
csharp-skill -s MySolution.sln inheritance-tree BaseService --direction down
```

**Options:**
- `--direction, -d` - Show ancestors, descendants, or both (default: both)

### Call Analysis Commands

#### find-callers
Find all methods that call a specific method.

```bash
csharp-skill -s MySolution.sln find-callers GetById
csharp-skill -s MySolution.sln find-callers ProcessOrder
```

#### find-callees
Find all methods called by a specific method.

```bash
csharp-skill -s MySolution.sln find-callees GetUser
csharp-skill -s MySolution.sln find-callees ProcessOrder
```

### Dependency Analysis Commands

#### dependencies
Analyze what types/namespaces a file or type depends on.

```bash
csharp-skill -s MySolution.sln dependencies src/Controllers/UserController.cs
csharp-skill -s MySolution.sln dependencies UserService
```

#### unused-code
Find potentially unused code (methods, classes, properties).

```bash
csharp-skill -s MySolution.sln unused-code
```

### Code Generation Commands

#### generate-interface
Extract an interface from a class.

```bash
csharp-skill -s MySolution.sln generate-interface UserService
csharp-skill -s MySolution.sln generate-interface UserService -o text
```

#### implement-interface
Generate implementation stubs for an interface.

```bash
csharp-skill -s MySolution.sln implement-interface IUserRepository
csharp-skill -s MySolution.sln implement-interface IDisposable
```

### Organization Commands

#### list-types
List all types in a namespace or file.

```bash
csharp-skill -s MySolution.sln list-types --namespace MyApp.Services
csharp-skill -s MySolution.sln list-types
```

**Options:**
- `--namespace` - Filter by namespace

#### namespace-tree
Show the namespace hierarchy of the solution.

```bash
csharp-skill -s MySolution.sln namespace-tree
csharp-skill -s MySolution.sln namespace-tree -o markdown
```

#### analyze-file
Quick comprehensive analysis of a single file.

```bash
csharp-skill -s MySolution.sln analyze-file src/Services/UserService.cs
csharp-skill -s MySolution.sln analyze-file src/Program.cs -o markdown
```

## Output Formats

### JSON (default)
```bash
csharp-skill -s MySolution.sln find-definition UserService -o json
```
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

### Text
```bash
csharp-skill -s MySolution.sln find-definition UserService -o text
```
```
Symbol: UserService
Kind: class
Location: File: src/Services/UserService.cs
          Line: 15
          Column: 18
Namespace: MyApp.Services
Accessibility: public
```

### Markdown
```bash
csharp-skill -s MySolution.sln find-definition UserService -o markdown
```
```markdown
**Symbol**: UserService
**Kind**: class
**Location**: ...
```

## Example Workflows

### Workflow 1: Safe Refactoring
```bash
# 1. Check current usage
csharp-skill -s MySolution.sln find-references GetById --type method

# 2. Preview rename
csharp-skill -s MySolution.sln rename GetById FindById --type method --preview

# 3. Execute rename
csharp-skill -s MySolution.sln rename GetById FindById --type method

# 4. Verify no errors
csharp-skill -s MySolution.sln diagnostics --severity error
```

### Workflow 2: Understanding a Type
```bash
# 1. Find where it's defined
csharp-skill -s MySolution.sln find-definition UserService

# 2. See its members
csharp-skill -s MySolution.sln list-members UserService

# 3. Check inheritance
csharp-skill -s MySolution.sln inheritance-tree UserService

# 4. See what it depends on
csharp-skill -s MySolution.sln dependencies UserService
```

### Workflow 3: Code Quality
```bash
# 1. Check for compilation errors
csharp-skill -s MySolution.sln diagnostics --severity error

# 2. Find unused code
csharp-skill -s MySolution.sln unused-code

# 3. Analyze specific file
csharp-skill -s MySolution.sln analyze-file src/Services/UserService.cs
```

## Exit Codes

- `0` - Success
- `1` - General error (exception, invalid arguments)
- `2` - Not found (symbol not found, file not found)

## Requirements

- **.NET 10.0 Runtime** - Required to run the tool
  - Download: https://dotnet.microsoft.com/download/dotnet/10.0
  - The binaries are framework-dependent and require .NET 10.0 to be installed
- Solution or project file to analyze

## Architecture

The tool is built on three main components:

1. **RoslynApiClient** - Wrapper around Roslyn APIs for symbol operations
2. **Command Handlers** - 18 command implementations using System.CommandLine
3. **Output Formatters** - JSON/text/markdown output generation

All source code is located in `src/CSharpSkill/`.

## Technical Details

### Dependencies
- `Microsoft.CodeAnalysis.CSharp.Workspaces` (v5.0.0)
- `Microsoft.CodeAnalysis.Workspaces.MSBuild` (v5.0.0)
- `Microsoft.Build.Locator` (v1.8.1)
- `System.CommandLine` (v2.0.0-beta4.22272.1)

### Key Features
- Uses Roslyn's `SymbolFinder` for accurate symbol lookups
- Leverages `SemanticModel` for type information
- Safe renaming via `Renamer` API
- Compilation diagnostics from full solution analysis

## Contributing

This tool implements all 18 commands specified in [SPEC.md](SPEC.md).

## License

[Your License Here]

## Support

For issues or questions, please refer to the specification document or open an issue.
