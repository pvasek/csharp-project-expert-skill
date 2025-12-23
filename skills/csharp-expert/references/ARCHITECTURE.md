# Technical Architecture

This document describes the technical architecture, implementation details, and performance considerations of the C# Project Expert skill.

## Table of Contents

- [Overview](#overview)
- [Roslyn API Integration](#roslyn-api-integration)
- [Architecture Components](#architecture-components)
- [Performance Considerations](#performance-considerations)
- [Output Format Specifications](#output-format-specifications)
- [Dependencies](#dependencies)
- [Error Handling](#error-handling)

---

## Overview

The C# Project Expert skill is built on top of the .NET Compiler Platform (Roslyn), which provides programmatic access to the same semantic analysis that powers Visual Studio, Rider, and other C# IDEs.

### Key Architectural Principles

1. **Compiler-Accurate Analysis** - Uses the same Roslyn APIs as Visual Studio
2. **Solution-Wide Context** - Understands entire solutions, not just individual files
3. **Symbol-Based Operations** - Works with C# symbols, not text patterns
4. **Multiple Output Formats** - JSON for automation, text/markdown for humans
5. **Fast and Efficient** - Caches compilation for performance

---

## Roslyn API Integration

### What is Roslyn?

Roslyn is the .NET Compiler Platform that provides:
- **Syntax Analysis** - Parse C# code into syntax trees
- **Semantic Analysis** - Understand meaning, types, and symbols
- **Symbol Resolution** - Find definitions, references, and relationships
- **Code Generation** - Generate and modify code programmatically

### Core Roslyn Components Used

#### 1. MSBuild Workspace

```csharp
using Microsoft.CodeAnalysis.MSBuild;

// Load solution
var workspace = MSBuildWorkspace.Create();
var solution = await workspace.OpenSolutionAsync(solutionPath);
```

**Purpose:** Loads .sln files with full project references and configurations.

**Benefits:**
- Understands multi-project solutions
- Resolves project-to-project references
- Respects build configurations (Debug/Release)
- Handles NuGet package references

#### 2. Compilation

```csharp
// Get compilation for a project
var project = solution.Projects.First();
var compilation = await project.GetCompilationAsync();
```

**Purpose:** Creates a compiled representation of the code.

**Benefits:**
- Access to all declared symbols
- Type checking and diagnostics
- Full semantic model

#### 3. Semantic Model

```csharp
var document = project.Documents.First();
var semanticModel = await document.GetSemanticModelAsync();
var symbol = semanticModel.GetSymbolInfo(node).Symbol;
```

**Purpose:** Provides semantic understanding of code.

**Benefits:**
- Resolve symbols from syntax nodes
- Get type information
- Find references and definitions

#### 4. Symbol Finder

```csharp
using Microsoft.CodeAnalysis.FindSymbols;

// Find all references to a symbol
var references = await SymbolFinder.FindReferencesAsync(symbol, solution);
```

**Purpose:** Fast, accurate symbol search across solution.

**Benefits:**
- Find definitions and references
- Find derived/base types
- Find implementations of interfaces

---

## Architecture Components

### 1. CLI Layer (`Program.cs`)

Handles command-line interface using `System.CommandLine`:

```
User Input → Command Parser → Command Handler → Roslyn API → Output Formatter → JSON/Text/Markdown
```

**Responsibilities:**
- Parse command-line arguments
- Validate input
- Route to appropriate command handler
- Format and output results

### 2. API Client (`RoslynApiClient.cs`)

Core wrapper around Roslyn APIs:

**Key Methods:**
- `LoadSolutionAsync()` - Initialize workspace
- `FindSymbolAsync()` - Locate symbols by name
- `GetReferencesAsync()` - Find all references
- `RenameSymbolAsync()` - Safe symbol renaming
- `GetDiagnosticsAsync()` - Compilation errors/warnings

### 3. Command Handlers

Each command has a dedicated handler implementing `ICommandHandler`:

**Structure:**
```
Commands/
├── Symbol/
│   ├── FindDefinitionCommand.cs
│   ├── FindReferencesCommand.cs
│   ├── RenameCommand.cs
│   ├── SignatureCommand.cs
│   └── ListMembersCommand.cs
├── Compilation/
│   ├── DiagnosticsCommand.cs
│   └── CheckSymbolExistsCommand.cs
├── TypeHierarchy/
│   ├── FindImplementationsCommand.cs
│   └── InheritanceTreeCommand.cs
├── CallAnalysis/
│   ├── FindCallersCommand.cs
│   └── FindCalleesCommand.cs
├── Dependency/
│   ├── DependenciesCommand.cs
│   └── UnusedCodeCommand.cs
├── CodeGeneration/
│   ├── GenerateInterfaceCommand.cs
│   └── ImplementInterfaceCommand.cs
└── Organization/
    ├── ListTypesCommand.cs
    ├── NamespaceTreeCommand.cs
    └── AnalyzeFileCommand.cs
```

**Each Handler:**
1. Validates input parameters
2. Calls RoslynApiClient methods
3. Processes results
4. Returns structured data models

### 4. Data Models

Strongly-typed models for command results:

```
Models/
├── SymbolLocation.cs        - File location information
├── SymbolReference.cs        - Reference details
├── SymbolSignature.cs        - Method/type signatures
├── TypeMember.cs             - Class member information
├── DiagnosticInfo.cs         - Compilation diagnostics
├── InheritanceTree.cs        - Type hierarchy data
├── CallAnalysis.cs           - Method call information
├── DependencyAnalysis.cs     - Dependency information
├── CodeGeneration.cs         - Generated code
├── Organization.cs           - Namespace/type organization
└── RenameResult.cs           - Rename preview/result
```

### 5. Output Formatters

Converts models to different output formats:

**Supported Formats:**
- **JSON** - Structured, machine-readable (default)
- **Text** - Human-readable terminal output
- **Markdown** - Formatted for documentation

**Example:**
```csharp
public interface IOutputFormatter
{
    string Format<T>(T data);
}

// JsonOutputFormatter, TextOutputFormatter, MarkdownOutputFormatter
```

---

## Performance Considerations

### Compilation Caching

**Problem:** Loading and compiling a solution is expensive (2-10 seconds for large solutions).

**Solution:** The workspace and compilations are cached after first load.

```csharp
private static MSBuildWorkspace? _workspace;
private static Solution? _solution;

public async Task<Solution> LoadSolutionAsync(string path)
{
    if (_solution != null && _solution.FilePath == path)
        return _solution; // Return cached solution

    _workspace = MSBuildWorkspace.Create();
    _solution = await _workspace.OpenSolutionAsync(path);
    return _solution;
}
```

**Impact:**
- First command: 2-10 seconds
- Subsequent commands: < 100ms

### Symbol Resolution

**Fast Path:** When searching for symbols, narrow the search space:

```csharp
// Slow - search all projects
var allSymbols = solution.Projects
    .SelectMany(p => p.Documents)
    .SelectMany(d => GetAllSymbols(d));

// Fast - filter by name first
var candidates = await SymbolFinder.FindDeclarationsAsync(
    solution,
    name: symbolName,
    ignoreCase: false
);
```

### Parallel Processing

Multiple files can be analyzed in parallel:

```csharp
var diagnostics = await Task.WhenAll(
    project.Documents.Select(async doc => {
        var semantic = await doc.GetSemanticModelAsync();
        return semantic.GetDiagnostics();
    })
);
```

### Memory Management

**Large Solutions:**
- Roslyn keeps syntax trees in memory
- Large solutions (100+ projects) can use 500MB-1GB RAM
- Workspace is disposed after operation completes

**Optimization:**
```csharp
// Only load necessary documents
var relevantDocs = project.Documents
    .Where(d => d.FilePath.Contains("Services"));

foreach (var doc in relevantDocs)
{
    // Process
}
// Unload when done
workspace.Dispose();
```

---

## Output Format Specifications

### JSON Schema

All commands output JSON following this structure:

```json
{
  "command": "find-definition",
  "timestamp": "2024-01-15T10:30:00Z",
  "success": true,
  "data": {
    // Command-specific payload
  },
  "errors": []
}
```

**Success Response:**
```json
{
  "success": true,
  "data": { /* command results */ }
}
```

**Error Response:**
```json
{
  "success": false,
  "errors": [
    {
      "code": "SYMBOL_NOT_FOUND",
      "message": "Symbol 'UserService' not found in solution",
      "details": "..."
    }
  ]
}
```

### Text Format

Human-readable output:

```
Symbol: UserService
Kind: class
Location:
  File: src/Services/UserService.cs
  Line: 15
  Column: 18
Namespace: MyApp.Services
Accessibility: public
```

**Format Rules:**
- Key-value pairs with proper indentation
- Nested data indented by 2 spaces
- Clear visual separation between sections

### Markdown Format

Documentation-friendly output:

```markdown
## Symbol: UserService

**Kind:** class
**Location:** `src/Services/UserService.cs:15:18`
**Namespace:** `MyApp.Services`
**Accessibility:** public
```

**Format Rules:**
- Headers for major sections
- Bold for labels
- Code formatting for paths and code
- Lists for collections

---

## Dependencies

### Core Dependencies

**Microsoft.CodeAnalysis.CSharp.Workspaces** (v5.0.0)
- Provides Roslyn APIs for C# analysis
- Includes syntax/semantic analysis
- Symbol resolution and renaming

**Microsoft.CodeAnalysis.Workspaces.MSBuild** (v5.0.0)
- Loads .sln and .csproj files
- Handles MSBuild-based projects
- Resolves project references

**Microsoft.Build.Locator** (v1.8.1)
- Locates MSBuild on the system
- Required for MSBuildWorkspace
- Ensures correct .NET SDK version

**System.CommandLine** (v2.0.0-beta4.22272.1)
- Modern CLI framework
- Argument parsing and validation
- Help generation

### Runtime Requirements

**.NET 10.0 Runtime**
- Required to run the tool
- Supports C# 1.0-12.0 analysis
- Cross-platform (Windows/macOS/Linux)

**MSBuild**
- Included with .NET SDK
- Required for solution loading
- Version matches .NET SDK version

---

## Error Handling

### Error Categories

#### 1. Solution Loading Errors

```
- Solution file not found
- Invalid solution format
- Project load failures
- Missing dependencies
```

**Handling:** Return clear error with path and resolution steps.

#### 2. Symbol Not Found

```
- Symbol name doesn't exist
- Symbol exists but wrong type
- Ambiguous symbol name
```

**Handling:** Return exit code 2, suggest alternatives if available.

#### 3. Compilation Errors

```
- C# syntax errors
- Type resolution failures
- Missing references
```

**Handling:** Report via diagnostics command, don't fail other commands.

#### 4. Invalid Arguments

```
- Missing required options
- Invalid type values
- Malformed paths
```

**Handling:** Show usage help, suggest correct format.

### Exit Codes

- `0` - Success
- `1` - General error (exception, invalid arguments)
- `2` - Not found (symbol not found, file not found)

### Error Messages

**Good Error Message:**
```
Error: Symbol 'UserService' not found in solution.

Suggestions:
  - Check spelling: did you mean 'UserServices'?
  - Verify symbol type: try --type class
  - Check namespace: try --in-namespace MyApp.Services

Use --verbose for more details.
```

**Poor Error Message:**
```
Error: Symbol not found.
```

---

## Supported C# Versions

The tool supports C# language versions 1.0 through 12.0 via Roslyn 5.0:

- **C# 1.0-7.3** - Full support
- **C# 8.0** - Nullable reference types, pattern matching
- **C# 9.0** - Records, init properties
- **C# 10.0** - Global usings, file-scoped namespaces
- **C# 11.0** - Raw string literals, list patterns
- **C# 12.0** - Primary constructors, collection expressions

**Note:** The tool analyzes code based on the project's target framework and language version specified in .csproj files.

---

## Extension Points

### Adding New Commands

1. Create command handler class
2. Implement `ICommandHandler` interface
3. Register in `Program.cs`
4. Add data model if needed
5. Update documentation

### Custom Output Formatters

Implement `IOutputFormatter` interface:

```csharp
public class CustomFormatter : IOutputFormatter
{
    public string Format<T>(T data)
    {
        // Custom formatting logic
    }
}
```

---

## Troubleshooting

### Workspace Load Failures

**Symptom:** "Could not load solution"

**Causes:**
- MSBuild not found
- .NET SDK version mismatch
- Corrupted project files

**Solution:**
```bash
# Verify MSBuild
dotnet --info

# Try restoring packages
cd path/to/solution
dotnet restore

# Check for project errors
dotnet build
```

### Symbol Resolution Issues

**Symptom:** "Symbol not found" but it exists

**Causes:**
- Symbol in different namespace
- Private/internal accessibility
- #if conditional compilation

**Solution:**
- Use `--in-namespace` to narrow search
- Check accessibility with `list-members`
- Ensure conditional symbols are defined

### Performance Issues

**Symptom:** Commands taking > 30 seconds

**Causes:**
- Very large solution (200+ projects)
- Network drive
- Insufficient memory

**Solution:**
- Use `--project` instead of `--solution`
- Run on local drive
- Increase available RAM
- Close other applications
