# Complete Command Reference

This document provides detailed reference for all 18 commands in the C# Project Expert skill.

## Command Categories

- [Symbol Commands](#symbol-commands) - find-definition, find-references, rename, signature, list-members
- [Compilation Commands](#compilation-commands) - diagnostics, check-symbol-exists
- [Type Hierarchy Commands](#type-hierarchy-commands) - find-implementations, inheritance-tree
- [Call Analysis Commands](#call-analysis-commands) - find-callers, find-callees
- [Dependency Commands](#dependency-commands) - dependencies, unused-code
- [Code Generation Commands](#code-generation-commands) - generate-interface, implement-interface
- [Organization Commands](#organization-commands) - list-types, namespace-tree, analyze-file

## Global Options

All commands support these global options:

- `--solution, -s <path>` - Path to .sln file (required for most commands)
- `--project, -p <path>` - Path to .csproj file (alternative to solution)
- `--output, -o <format>` - Output format: `json`, `text`, or `markdown` (default: json)
- `--verbose, -v` - Enable verbose logging

---

## Symbol Commands

### find-definition

Find where a symbol (class, method, property, etc.) is defined.

**Syntax:**
```bash
./scripts/CSharpExpertCli -s <solution> find-definition <symbol-name> [options]
```

**Options:**
- `--type, -t <type>` - Filter by symbol type: `class`, `method`, `property`, `field`, `interface`, `enum`
- `--in-file, -f <path>` - Search only in specific file
- `--in-namespace, -n <namespace>` - Search only in specific namespace

**Examples:**
```bash
# Find a class
./scripts/CSharpExpertCli -s MySolution.sln find-definition UserService --type class

# Find a method in a specific namespace
./scripts/CSharpExpertCli -s MySolution.sln find-definition GetById \
  --type method \
  --in-namespace MyApp.Services

# Find a property in a specific file
./scripts/CSharpExpertCli -s MySolution.sln find-definition UserId \
  --type property \
  --in-file src/Models/User.cs
```

**JSON Output:**
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

---

### find-references

Find all references/usages of a symbol throughout the solution.

**Syntax:**
```bash
./scripts/CSharpExpertCli -s <solution> find-references <symbol-name> [options]
```

**Options:**
- `--type, -t <type>` - Symbol type to search for
- `--in-namespace, -n <namespace>` - Symbol namespace

**Examples:**
```bash
# Find all references to a method
./scripts/CSharpExpertCli -s MySolution.sln find-references GetById --type method

# Find all references to an interface
./scripts/CSharpExpertCli -s MySolution.sln find-references IUserRepository \
  --type interface

# Find references to a class
./scripts/CSharpExpertCli -s MySolution.sln find-references UserDto --type class
```

**JSON Output:**
```json
{
  "symbol": "UserService.GetById",
  "totalReferences": 23,
  "references": [
    {
      "file": "src/Controllers/UserController.cs",
      "line": 45,
      "column": 28,
      "context": "var user = _userService.GetById(id);",
      "kind": "invocation"
    },
    {
      "file": "src/Services/AdminService.cs",
      "line": 102,
      "column": 15,
      "context": "return userService.GetById(adminId);",
      "kind": "invocation"
    }
  ]
}
```

---

### rename

Safely rename a symbol across the entire solution with preview mode.

**Syntax:**
```bash
./scripts/CSharpExpertCli -s <solution> rename <old-name> <new-name> [options]
```

**Options:**
- `--type, -t <type>` - Type of symbol being renamed
- `--in-namespace, -n <namespace>` - Limit scope to namespace
- `--preview` - Show changes without applying them (RECOMMENDED)
- `--rename-file` - Also rename the file if renaming a type

**Examples:**
```bash
# Preview rename (safe - shows changes without applying)
./scripts/CSharpExpertCli -s MySolution.sln rename UserService UserManager \
  --type class \
  --preview

# Apply rename and update file name
./scripts/CSharpExpertCli -s MySolution.sln rename UserService UserManager \
  --type class \
  --rename-file

# Rename a method
./scripts/CSharpExpertCli -s MySolution.sln rename GetById FindById \
  --type method

# Rename within specific namespace
./scripts/CSharpExpertCli -s MySolution.sln rename Helper Utility \
  --type class \
  --in-namespace MyApp.Common
```

**JSON Output (Preview Mode):**
```json
{
  "symbol": "UserService",
  "newName": "UserManager",
  "changes": [
    {
      "file": "src/Services/UserService.cs",
      "fileName": "UserManager.cs",
      "edits": [
        {"line": 15, "old": "class UserService", "new": "class UserManager"},
        {"line": 20, "old": "public UserService(", "new": "public UserManager("}
      ]
    },
    {
      "file": "src/Controllers/UserController.cs",
      "edits": [
        {"line": 12, "old": "private readonly UserService", "new": "private readonly UserManager"}
      ]
    }
  ],
  "totalChanges": 47,
  "affectedFiles": 12
}
```

**Important:** Always use `--preview` first to verify changes before applying!

---

### signature

Get the signature and documentation of a symbol.

**Syntax:**
```bash
./scripts/CSharpExpertCli -s <solution> signature <symbol-name> [options]
```

**Options:**
- `--type, -t <type>` - Type of symbol
- `--include-overloads` - Show all overloads for methods
- `--include-docs` - Include XML documentation comments (default: true)

**Examples:**
```bash
# Get method signature with overloads
./scripts/CSharpExpertCli -s MySolution.sln signature GetById \
  --type method \
  --include-overloads

# Get class signature
./scripts/CSharpExpertCli -s MySolution.sln signature UserService --type class

# Get property signature
./scripts/CSharpExpertCli -s MySolution.sln signature UserId --type property
```

**JSON Output:**
```json
{
  "symbol": "UserService.GetById",
  "kind": "method",
  "signatures": [
    {
      "declaration": "public User GetById(int id)",
      "returnType": "User",
      "parameters": [
        {"name": "id", "type": "int", "isOptional": false}
      ],
      "accessibility": "public",
      "isStatic": false,
      "isAsync": false,
      "documentation": "Retrieves a user by their unique identifier."
    }
  ]
}
```

---

### list-members

List all members (methods, properties, fields) of a type.

**Syntax:**
```bash
./scripts/CSharpExpertCli -s <solution> list-members <type-name> [options]
```

**Options:**
- `--kind, -k <kind>` - Filter by member kind: `method`, `property`, `field`, `event`
- `--accessibility, -a <level>` - Filter by: `public`, `private`, `protected`, `internal`
- `--include-inherited` - Include inherited members

**Examples:**
```bash
# List all members
./scripts/CSharpExpertCli -s MySolution.sln list-members UserService

# List only public methods
./scripts/CSharpExpertCli -s MySolution.sln list-members User \
  --kind method \
  --accessibility public

# List properties including inherited
./scripts/CSharpExpertCli -s MySolution.sln list-members UserDto \
  --kind property \
  --include-inherited
```

**JSON Output:**
```json
{
  "type": "UserService",
  "namespace": "MyApp.Services",
  "members": [
    {
      "name": "GetById",
      "kind": "method",
      "accessibility": "public",
      "returnType": "User",
      "signature": "public User GetById(int id)"
    },
    {
      "name": "GetAll",
      "kind": "method",
      "accessibility": "public",
      "returnType": "List<User>",
      "signature": "public List<User> GetAll()"
    }
  ]
}
```

---

## Compilation Commands

### diagnostics

Get all compilation errors, warnings, and info messages.

**Syntax:**
```bash
./scripts/CSharpExpertCli -s <solution> diagnostics [options]
```

**Options:**
- `--severity, -s <level>` - Filter by severity: `error`, `warning`, `info`
- `--file, -f <path>` - Get diagnostics only for specific file
- `--code, -c <code>` - Filter by diagnostic code (e.g., CS0246)

**Examples:**
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

**JSON Output:**
```json
{
  "totalDiagnostics": 5,
  "errors": 2,
  "warnings": 3,
  "diagnostics": [
    {
      "severity": "error",
      "code": "CS0246",
      "message": "The type or namespace name 'UserDto' could not be found",
      "file": "src/Services/UserService.cs",
      "line": 25,
      "column": 20
    }
  ]
}
```

---

### check-symbol-exists

Quickly verify if a symbol exists and is accessible.

**Syntax:**
```bash
./scripts/CSharpExpertCli -s <solution> check-symbol-exists <symbol-name> [options]
```

**Options:**
- `--type, -t <type>` - Expected symbol type
- `--in-namespace, -n <namespace>` - Expected namespace

**Examples:**
```bash
# Check if a class exists
./scripts/CSharpExpertCli -s MySolution.sln check-symbol-exists UserDto --type class

# Check if a method exists in a namespace
./scripts/CSharpExpertCli -s MySolution.sln check-symbol-exists GetById \
  --type method \
  --in-namespace MyApp.Services
```

**JSON Output:**
```json
{
  "symbol": "UserDto",
  "exists": true,
  "kind": "class",
  "namespace": "MyApp.Models",
  "accessibility": "public"
}
```

---

## Type Hierarchy Commands

### find-implementations

Find all implementations of an interface or abstract class.

**Syntax:**
```bash
./scripts/CSharpExpertCli -s <solution> find-implementations <interface-name>
```

**Examples:**
```bash
# Find implementations of an interface
./scripts/CSharpExpertCli -s MySolution.sln find-implementations IUserRepository

# Find implementations of IDisposable
./scripts/CSharpExpertCli -s MySolution.sln find-implementations IDisposable

# Find implementations of abstract class
./scripts/CSharpExpertCli -s MySolution.sln find-implementations BaseService
```

**JSON Output:**
```json
{
  "interface": "IUserRepository",
  "implementations": [
    {
      "name": "UserRepository",
      "namespace": "MyApp.Repositories",
      "file": "src/Repositories/UserRepository.cs",
      "line": 10,
      "isAbstract": false
    },
    {
      "name": "CachedUserRepository",
      "namespace": "MyApp.Repositories",
      "file": "src/Repositories/CachedUserRepository.cs",
      "line": 8,
      "isAbstract": false
    }
  ]
}
```

---

### inheritance-tree

Show inheritance hierarchy (ancestors and descendants).

**Syntax:**
```bash
./scripts/CSharpExpertCli -s <solution> inheritance-tree <type-name> [options]
```

**Options:**
- `--direction, -d <dir>` - Show `ancestors`, `descendants`, or `both` (default: both)

**Examples:**
```bash
# Show full inheritance tree
./scripts/CSharpExpertCli -s MySolution.sln inheritance-tree UserService

# Show only descendants
./scripts/CSharpExpertCli -s MySolution.sln inheritance-tree BaseService --direction descendants

# Show only ancestors
./scripts/CSharpExpertCli -s MySolution.sln inheritance-tree AdminUserService --direction ancestors
```

**JSON Output:**
```json
{
  "type": "UserService",
  "ancestors": [
    "BaseService",
    "Object"
  ],
  "descendants": [
    "AdminUserService",
    "GuestUserService"
  ],
  "interfaces": [
    "IUserService",
    "IDisposable"
  ]
}
```

---

## Call Analysis Commands

### find-callers

Find all methods that call a specific method.

**Syntax:**
```bash
./scripts/CSharpExpertCli -s <solution> find-callers <method-name>
```

**Examples:**
```bash
# Find what calls GetById
./scripts/CSharpExpertCli -s MySolution.sln find-callers GetById

# Find what calls ProcessOrder
./scripts/CSharpExpertCli -s MySolution.sln find-callers ProcessOrder
```

**JSON Output:**
```json
{
  "method": "UserService.GetById",
  "callers": [
    {
      "caller": "UserController.GetUserById",
      "file": "src/Controllers/UserController.cs",
      "line": 45,
      "context": "var user = _userService.GetById(id);"
    },
    {
      "caller": "AdminService.GetAdminUser",
      "file": "src/Services/AdminService.cs",
      "line": 102,
      "context": "return userService.GetById(adminId);"
    }
  ]
}
```

---

### find-callees

Find all methods called by a specific method.

**Syntax:**
```bash
./scripts/CSharpExpertCli -s <solution> find-callees <method-name>
```

**Examples:**
```bash
# Find what GetUser calls
./scripts/CSharpExpertCli -s MySolution.sln find-callees GetUser

# Find what ProcessOrder calls
./scripts/CSharpExpertCli -s MySolution.sln find-callees ProcessOrder
```

**JSON Output:**
```json
{
  "method": "UserService.GetUser",
  "callees": [
    {
      "callee": "UserRepository.FindById",
      "file": "src/Services/UserService.cs",
      "line": 35
    },
    {
      "callee": "UserMapper.MapToDto",
      "file": "src/Services/UserService.cs",
      "line": 36
    }
  ]
}
```

---

## Dependency Commands

### dependencies

Analyze what types/namespaces a file or type depends on.

**Syntax:**
```bash
./scripts/CSharpExpertCli -s <solution> dependencies <file-or-type>
```

**Examples:**
```bash
# Analyze dependencies of a file
./scripts/CSharpExpertCli -s MySolution.sln dependencies src/Controllers/UserController.cs

# Analyze dependencies of a type
./scripts/CSharpExpertCli -s MySolution.sln dependencies UserService
```

**JSON Output:**
```json
{
  "file": "src/Controllers/UserController.cs",
  "dependencies": {
    "namespaces": [
      "MyApp.Services",
      "MyApp.Models",
      "Microsoft.AspNetCore.Mvc"
    ],
    "types": [
      "UserService",
      "UserDto",
      "IMapper"
    ]
  }
}
```

---

### unused-code

Find potentially unused code (methods, classes, properties).

**Syntax:**
```bash
./scripts/CSharpExpertCli -s <solution> unused-code
```

**Examples:**
```bash
# Find all unused code
./scripts/CSharpExpertCli -s MySolution.sln unused-code
```

**JSON Output:**
```json
{
  "unusedSymbols": [
    {
      "symbol": "OldHelper.ProcessData",
      "kind": "method",
      "file": "src/Helpers/OldHelper.cs",
      "line": 45,
      "accessibility": "private"
    },
    {
      "symbol": "TempClass",
      "kind": "class",
      "file": "src/Temp/TempClass.cs",
      "line": 10,
      "accessibility": "internal"
    }
  ]
}
```

---

## Code Generation Commands

### generate-interface

Extract an interface from a class.

**Syntax:**
```bash
./scripts/CSharpExpertCli -s <solution> generate-interface <class-name>
```

**Examples:**
```bash
# Generate interface from a class
./scripts/CSharpExpertCli -s MySolution.sln generate-interface UserService

# Generate interface in text format
./scripts/CSharpExpertCli -s MySolution.sln -o text generate-interface UserService
```

**JSON Output:**
```json
{
  "interfaceName": "IUserService",
  "code": "public interface IUserService\n{\n    User GetById(int id);\n    List<User> GetAll();\n}"
}
```

---

### implement-interface

Generate implementation stubs for an interface.

**Syntax:**
```bash
./scripts/CSharpExpertCli -s <solution> implement-interface <interface-name>
```

**Examples:**
```bash
# Generate implementation stubs
./scripts/CSharpExpertCli -s MySolution.sln implement-interface IUserRepository

# Generate stubs for IDisposable
./scripts/CSharpExpertCli -s MySolution.sln implement-interface IDisposable
```

**JSON Output:**
```json
{
  "interface": "IUserRepository",
  "stubs": "public class UserRepositoryImpl : IUserRepository\n{\n    public User FindById(int id)\n    {\n        throw new NotImplementedException();\n    }\n}"
}
```

---

## Organization Commands

### list-types

List all types in a namespace or file.

**Syntax:**
```bash
./scripts/CSharpExpertCli -s <solution> list-types [options]
```

**Options:**
- `--namespace <namespace>` - Filter by namespace

**Examples:**
```bash
# List all types in solution
./scripts/CSharpExpertCli -s MySolution.sln list-types

# List types in specific namespace
./scripts/CSharpExpertCli -s MySolution.sln list-types --namespace MyApp.Services
```

**JSON Output:**
```json
{
  "types": [
    {"name": "UserService", "kind": "class", "namespace": "MyApp.Services"},
    {"name": "OrderService", "kind": "class", "namespace": "MyApp.Services"},
    {"name": "IUserService", "kind": "interface", "namespace": "MyApp.Services"}
  ]
}
```

---

### namespace-tree

Show the namespace hierarchy of the solution.

**Syntax:**
```bash
./scripts/CSharpExpertCli -s <solution> namespace-tree
```

**Examples:**
```bash
# Show namespace tree
./scripts/CSharpExpertCli -s MySolution.sln namespace-tree

# Show namespace tree in markdown format
./scripts/CSharpExpertCli -s MySolution.sln -o markdown namespace-tree
```

**JSON Output:**
```json
{
  "root": "MyApp",
  "namespaces": {
    "MyApp": {
      "Services": ["UserService", "OrderService"],
      "Models": ["User", "Order"],
      "Controllers": ["UserController", "OrderController"]
    }
  }
}
```

---

### analyze-file

Quick comprehensive analysis of a single file.

**Syntax:**
```bash
./scripts/CSharpExpertCli -s <solution> analyze-file <file-path>
```

**Examples:**
```bash
# Analyze a file
./scripts/CSharpExpertCli -s MySolution.sln analyze-file src/Services/UserService.cs

# Analyze in markdown format
./scripts/CSharpExpertCli -s MySolution.sln -o markdown analyze-file src/Program.cs
```

**JSON Output:**
```json
{
  "file": "src/Services/UserService.cs",
  "types": [
    {"name": "UserService", "kind": "class", "members": 8}
  ],
  "dependencies": ["IUserRepository", "IMapper"],
  "diagnostics": [],
  "linesOfCode": 145
}
```

---

## Exit Codes

- `0` - Success
- `1` - General error (exception, invalid arguments)
- `2` - Not found (symbol not found, file not found)

---

## Output Formats

All commands support three output formats via `--output` or `-o`:

### JSON (Default)
Machine-readable, ideal for automation and parsing.

### Text
Human-readable, good for terminal output and quick inspection.

### Markdown
Formatted for documentation and reports.

---

## Common Patterns

### Piping Output

Since all commands output to stdout, you can pipe results:

```bash
# Parse JSON with jq
./scripts/CSharpExpertCli -s MySolution.sln find-definition UserService --type class | jq '.location.file'

# Count references
./scripts/CSharpExpertCli -s MySolution.sln find-references GetById --type method | jq '.totalReferences'
```

### Combining Commands

Use multiple commands in sequence for complex analysis:

```bash
# Find a class, then list its members
./scripts/CSharpExpertCli -s MySolution.sln find-definition UserService --type class
./scripts/CSharpExpertCli -s MySolution.sln list-members UserService

# Find a method, check its callers, then get its signature
./scripts/CSharpExpertCli -s MySolution.sln find-callers ProcessOrder
./scripts/CSharpExpertCli -s MySolution.sln signature ProcessOrder --type method
```

### Error Handling

Check exit codes for success:

```bash
if ./scripts/CSharpExpertCli -s MySolution.sln find-definition UserService --type class; then
    echo "Found UserService"
else
    echo "UserService not found"
fi
```
