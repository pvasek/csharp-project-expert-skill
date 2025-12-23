# Real-World Examples

This document provides real-world usage examples and integration patterns for the C# Project Expert skill.

## Table of Contents

- [Refactoring a Legacy Codebase](#refactoring-a-legacy-codebase)
- [Understanding a New Project](#understanding-a-new-project)
- [Pre-Merge Code Review](#pre-merge-code-review)
- [Automated Code Quality Checks](#automated-code-quality-checks)
- [Integration with CI/CD](#integration-with-cicd)
- [JSON Parsing for Claude](#json-parsing-for-claude)
- [Complex Multi-Step Scenarios](#complex-multi-step-scenarios)

---

## Refactoring a Legacy Codebase

**Scenario:** You inherit a legacy C# project and need to modernize the data access layer. The old `UserRepository` class needs to be split into `UserReadRepository` and `UserWriteRepository`.

### Step 1: Understand Current Usage

```bash
# Find all references to UserRepository
./scripts/CSharpExpertCli -s LegacyApp.sln find-references UserRepository \
  --type class \
  -o json | jq '{total: .totalReferences, files: [.references[].file] | unique | length}'
```

**Output:**
```json
{
  "total": 47,
  "files": 12
}
```

**Analysis:** 47 usages across 12 files. This is a significant change.

### Step 2: Categorize Methods

```bash
# List all methods to categorize as read vs write
./scripts/CSharpExpertCli -s LegacyApp.sln list-members UserRepository \
  --kind method \
  --accessibility public \
  -o json | jq '.members[] | {name, signature}'
```

**Output:**
```json
[
  {"name": "GetById", "signature": "public User GetById(int id)"},
  {"name": "GetAll", "signature": "public List<User> GetAll()"},
  {"name": "Create", "signature": "public void Create(User user)"},
  {"name": "Update", "signature": "public void Update(User user)"},
  {"name": "Delete", "signature": "public void Delete(int id)"}
]
```

**Categorization:**
- **Read**: `GetById`, `GetAll`
- **Write**: `Create`, `Update`, `Delete`

### Step 3: Check Dependencies

```bash
# See what UserRepository depends on
./scripts/CSharpExpertCli -s LegacyApp.sln dependencies UserRepository \
  -o json | jq '.dependencies'
```

### Step 4: Generate New Interfaces

```bash
# Generate interface to understand structure
./scripts/CSharpExpertCli -s LegacyApp.sln generate-interface UserRepository -o text
```

**Result:** Use this as a template to create `IUserReadRepository` and `IUserWriteRepository`.

### Step 5: Find Impact of Each Method

For each method you're moving:

```bash
#!/bin/bash
for method in "GetById" "GetAll" "Create" "Update" "Delete"; do
    echo "=== $method Usage ==="
    ./scripts/CSharpExpertCli -s LegacyApp.sln find-callers "$method" | \
      jq '{method: "'$method'", callers: .callers | length}'
done
```

**Output:**
```json
{"method": "GetById", "callers": 15}
{"method": "GetAll", "callers": 8}
{"method": "Create", "callers": 12}
{"method": "Update", "callers": 7}
{"method": "Delete", "callers": 5}
```

---

## Understanding a New Project

**Scenario:** First day on a new team. Need to understand the project structure quickly.

### Quick Project Overview

```bash
#!/bin/bash
SOLUTION="NewProject.sln"

echo "===New Project Analysis ==="
echo

# 1. Namespace structure
echo "1. Namespace Structure:"
./scripts/CSharpExpertCli -s "$SOLUTION" namespace-tree -o text | head -20

# 2. Count types by namespace
echo -e "\n2. Type Counts by Namespace:"
./scripts/CSharpExpertCli -s "$SOLUTION" list-types -o json | \
  jq -r '.types | group_by(.namespace) | .[] | "\(.[0].namespace): \(length) types"'

# 3. Find entry point
echo -e "\n3. Entry Points:"
./scripts/CSharpExpertCli -s "$SOLUTION" find-definition Main --type method -o text

# 4. Find key interfaces
echo -e "\n4. Key Interfaces:"
./scripts/CSharpExpertCli -s "$SOLUTION" list-types -o json | \
  jq -r '.types[] | select(.kind == "interface") | .name' | head -10

# 5. Check for compilation issues
echo -e "\n5. Health Check:"
ERROR_COUNT=$(./scripts/CSharpExpertCli -s "$SOLUTION" diagnostics --severity error -o json | jq '.errors')
WARNING_COUNT=$(./scripts/CSharpExpertCli -s "$SOLUTION" diagnostics --severity warning -o json | jq '.warnings')
echo "Errors: $ERROR_COUNT, Warnings: $WARNING_COUNT"
```

### Exploring a Specific Feature

```bash
#!/bin/bash
# Understand the "User Management" feature

SOLUTION="NewProject.sln"
NAMESPACE="NewProject.UserManagement"

echo "=== User Management Feature Analysis ==="

# 1. List all types in this namespace
echo -e "\n1. Types in $NAMESPACE:"
./scripts/CSharpExpertCli -s "$SOLUTION" list-types --namespace "$NAMESPACE" -o text

# 2. For each service, show what it does
for service in $(./scripts/CSharpExpertCli -s "$SOLUTION" list-types --namespace "$NAMESPACE" -o json | \
  jq -r '.types[] | select(.name | endswith("Service")) | .name'); do

    echo -e "\n=== $service ==="

    # Show public methods
    echo "Public Methods:"
    ./scripts/CSharpExpertCli -s "$SOLUTION" list-members "$service" \
      --kind method \
      --accessibility public \
      -o text | grep "Name:" | sed 's/Name: /  - /'

    # Show dependencies
    echo "Dependencies:"
    ./scripts/CSharpExpertCli -s "$SOLUTION" dependencies "$service" -o json | \
      jq -r '.dependencies.types[]' | sed 's/^/  - /'
done
```

---

## Pre-Merge Code Review

**Scenario:** Reviewing a pull request that refactors the authentication system.

### Automated PR Review Script

```bash
#!/bin/bash
SOLUTION="MyApp.sln"
BASE_BRANCH="main"
PR_BRANCH="feature/auth-refactor"

echo "=== PR Review: $PR_BRANCH ==="

# 1. Get list of changed C# files
git diff "$BASE_BRANCH"..."$PR_BRANCH" --name-only | grep '\.cs$' > changed_files.txt

echo "Changed Files: $(wc -l < changed_files.txt)"

# 2. Check each file for issues
while IFS= read -r file; do
    echo -e "\n=== Analyzing $file ==="

    # Check for new errors
    ./scripts/CSharpExpertCli -s "$SOLUTION" diagnostics --file "$file" --severity error -o json > /tmp/diagnostics.json

    ERROR_COUNT=$(jq '.errors' < /tmp/diagnostics.json)
    if [ "$ERROR_COUNT" -gt 0 ]; then
        echo "❌ $ERROR_COUNT errors found:"
        jq -r '.diagnostics[] | "  Line \(.line): \(.message)"' < /tmp/diagnostics.json
    fi

    # Check for new warnings
    WARNING_COUNT=$(./scripts/CSharpExpertCli -s "$SOLUTION" diagnostics --file "$file" --severity warning -o json | jq '.warnings')
    if [ "$WARNING_COUNT" -gt 0 ]; then
        echo "⚠️  $WARNING_COUNT warnings found"
    fi

done < changed_files.txt

# 3. Check if any public APIs were changed
echo -e "\n=== Public API Changes ==="
./scripts/CSharpExpertCli -s "$SOLUTION" list-types --namespace "MyApp.Auth" -o json | \
  jq -r '.types[] | select(.kind == "interface" or .kind == "class") | .name' | \
  while read -r type; do
      echo "Checking $type for breaking changes..."
      # Compare members before/after (requires checking out both branches)
  done

# 4. Summary
echo -e "\n=== Summary ==="
TOTAL_ERRORS=$(find /tmp -name "diagnostics.json" -exec jq -s 'map(.errors) | add' {} \;)
echo "Total Errors: ${TOTAL_ERRORS:-0}"
echo "Review complete."
```

---

## Automated Code Quality Checks

**Scenario:** Run automated quality checks before every commit.

### Git Pre-Commit Hook

Create `.git/hooks/pre-commit`:

```bash
#!/bin/bash
set -e

SOLUTION="MyApp.sln"
SKILL_PATH="./skills/csharp-project-expert/scripts/CSharpExpertCli"

echo "Running code quality checks..."

# Get staged .cs files
STAGED_FILES=$(git diff --cached --name-only --diff-filter=ACM | grep '\.cs$' || true)

if [ -z "$STAGED_FILES" ]; then
    echo "No C# files changed."
    exit 0
fi

# 1. Check for compilation errors
echo "Checking for compilation errors..."
ERROR_COUNT=$("$SKILL_PATH" -s "$SOLUTION" diagnostics --severity error -o json | jq '.errors')

if [ "$ERROR_COUNT" -gt 0 ]; then
    echo "❌ Found $ERROR_COUNT compilation errors. Fix before committing."
    "$SKILL_PATH" -s "$SOLUTION" diagnostics --severity error -o text
    exit 1
fi

# 2. Check each staged file for warnings
echo "Checking staged files for warnings..."
for file in $STAGED_FILES; do
    WARNING_COUNT=$("$SKILL_PATH" -s "$SOLUTION" diagnostics --file "$file" --severity warning -o json | jq '.warnings')

    if [ "$WARNING_COUNT" -gt 3 ]; then
        echo "⚠️  $file has $WARNING_COUNT warnings (threshold: 3)"
        "$SKILL_PATH" -s "$SOLUTION" diagnostics --file "$file" --severity warning -o text
        read -p "Continue anyway? (y/n) " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            exit 1
        fi
    fi
done

# 3. Check for unused code in staged files
echo "Checking for unused code..."
UNUSED=$("$SKILL_PATH" -s "$SOLUTION" unused-code -o json)
UNUSED_COUNT=$(echo "$UNUSED" | jq '.unusedSymbols | length')

if [ "$UNUSED_COUNT" -gt 0 ]; then
    echo "ℹ️  Found $UNUSED_COUNT potentially unused symbols"
    # Don't block commit, just inform
fi

echo "✅ Code quality checks passed!"
exit 0
```

Make it executable:
```bash
chmod +x .git/hooks/pre-commit
```

---

## Integration with CI/CD

### GitHub Actions Workflow

`.github/workflows/code-quality.yml`:

```yaml
name: Code Quality

on: [pull_request]

jobs:
  analyze:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Download C# Project Expert Skill
        run: |
          wget https://github.com/your-org/csharp-project-expert/releases/latest/download/csharp-project-expert-v1.0.0.zip
          unzip csharp-project-expert-v1.0.0.zip
          chmod +x csharp-project-expert/scripts/CSharpExpertCli

      - name: Check for compilation errors
        run: |
          ERROR_COUNT=$(./csharp-project-expert/scripts/CSharpExpertCli -s MyApp.sln diagnostics --severity error -o json | jq '.errors')
          if [ "$ERROR_COUNT" -gt 0 ]; then
            echo "Found $ERROR_COUNT errors"
            ./csharp-project-expert/scripts/CSharpExpertCli -s MyApp.sln diagnostics --severity error -o text
            exit 1
          fi

      - name: Check for unused code
        run: |
          ./csharp-project-expert/scripts/CSharpExpertCli -s MyApp.sln unused-code -o json > unused.json
          UNUSED_COUNT=$(jq '.unusedSymbols | length' unused.json)
          echo "Found $UNUSED_COUNT unused symbols"

      - name: Upload unused code report
        uses: actions/upload-artifact@v3
        with:
          name: unused-code-report
          path: unused.json
```

---

## JSON Parsing for Claude

**Scenario:** Claude needs to parse and understand the output for further processing.

### Example: Finding All Controllers and Their Actions

```bash
# Get all controller classes
./scripts/CSharpExpertCli -s WebApp.sln list-types --namespace "WebApp.Controllers" -o json > controllers.json

# For each controller, get actions (public methods)
jq -r '.types[] | select(.name | endswith("Controller")) | .name' controllers.json | \
  while read -r controller; do
    echo "=== $controller Actions ==="
    ./scripts/CSharpExpertCli -s WebApp.sln list-members "$controller" \
      --kind method \
      --accessibility public \
      -o json | \
      jq -r '.members[] | "  - \(.name): \(.returnType)"'
  done
```

**Output for Claude to Process:**
```
=== UserController Actions ===
  - GetUsers: List<User>
  - GetUser: User
  - CreateUser: ActionResult
  - UpdateUser: ActionResult
  - DeleteUser: ActionResult

=== OrderController Actions ===
  - GetOrders: List<Order>
  - GetOrder: Order
  - CreateOrder: ActionResult
```

### Example: Build Dependency Graph

```bash
#!/bin/bash
# Generate a dependency graph for Claude to visualize

SOLUTION="MyApp.sln"
NAMESPACE="MyApp.Services"

echo "{"
echo '  "namespace": "'$NAMESPACE'",'
echo '  "dependencies": ['

FIRST=true
./scripts/CSharpExpertCli -s "$SOLUTION" list-types --namespace "$NAMESPACE" -o json | \
  jq -r '.types[].name' | \
  while read -r type; do
    if [ "$FIRST" = true ]; then
        FIRST=false
    else
        echo "    ,"
    fi

    DEPS=$(./scripts/CSharpExpertCli -s "$SOLUTION" dependencies "$type" -o json 2>/dev/null | \
      jq -c '{type: "'$type'", dependsOn: .dependencies.types}')
    echo -n "    $DEPS"
  done

echo
echo "  ]"
echo "}"
```

**Claude can then visualize:**
```
UserService depends on:
  → IUserRepository
  → IMapper
  → ILogger

OrderService depends on:
  → IOrderRepository
  → IUserRepository
  → IPaymentService
```

---

## Complex Multi-Step Scenarios

### Scenario: Extract and Implement CQRS Pattern

**Goal:** Refactor a service layer to use CQRS (Command Query Responsibility Segregation).

```bash
#!/bin/bash
SERVICE="UserService"
SOLUTION="MyApp.sln"

echo "=== CQRS Refactoring for $SERVICE ==="

# Step 1: List all methods
echo "1. Analyzing methods..."
./scripts/CSharpExpertCli -s "$SOLUTION" list-members "$SERVICE" \
  --kind method \
  --accessibility public \
  -o json > /tmp/methods.json

# Step 2: Categorize as Query (read) or Command (write)
echo "2. Categorizing methods..."
QUERIES=$(jq -r '.members[] | select(.name | startswith("Get") or startswith("Find") or startswith("List")) | .name' < /tmp/methods.json)
COMMANDS=$(jq -r '.members[] | select(.name | startswith("Create") or startswith("Update") or startswith("Delete") or startswith("Save")) | .name' < /tmp/methods.json)

echo "Queries: $(echo "$QUERIES" | wc -l)"
echo "$QUERIES" | sed 's/^/  - /'

echo "Commands: $(echo "$COMMANDS" | wc -l)"
echo "$COMMANDS" | sed 's/^/  - /'

# Step 3: For each query, generate interface
echo -e "\n3. Generating Query Interfaces..."
for query in $QUERIES; do
    ./scripts/CSharpExpertCli -s "$SOLUTION" signature "$query" \
      --type method \
      -o json | \
      jq -r '.signatures[0] | "public interface I\(input.query)Query { \(.returnType) Execute(\(.parameters | map("\(.type) \(.name)") | join(", "))); }"' \
        --arg query "$query"
done

# Step 4: Check impact of splitting the service
echo -e "\n4. Impact Analysis..."
for method in $QUERIES $COMMANDS; do
    CALLER_COUNT=$(./scripts/CSharpExpertCli -s "$SOLUTION" find-callers "$method" -o json | jq '.callers | length')
    echo "$method is called from $CALLER_COUNT places"
done

# Step 5: Suggest new structure
echo -e "\n5. Suggested Structure:"
echo "  - IUserQueryService (with $(echo "$QUERIES" | wc -l) methods)"
echo "  - IUserCommandService (with $(echo "$COMMANDS" | wc -l) methods)"
echo "  - Refactor $(./scripts/CSharpExpertCli -s "$SOLUTION" find-references "$SERVICE" -o json | jq '.totalReferences') references"
```

---

## Performance Benchmarking

```bash
#!/bin/bash
# Compare tool performance vs manual searching

SOLUTION="LargeSolution.sln"

echo "=== Performance Comparison ==="

# Benchmark: Find all references to a common symbol
echo "1. Finding references to 'Logger'..."

# Using C# Project Expert
START=$(date +%s%N)
./scripts/CSharpExpertCli -s "$SOLUTION" find-references Logger --type class -o json > /dev/null
END=$(date +%s%N)
TOOL_TIME=$(echo "scale=3; ($END - $START) / 1000000000" | bc)

# Using grep (for comparison)
START=$(date +%s%N)
grep -r "Logger" --include="*.cs" . > /dev/null 2>&1
END=$(date +%s%N)
GREP_TIME=$(echo "scale=3; ($END - $START) / 1000000000" | bc)

echo "C# Project Expert: ${TOOL_TIME}s (accurate, semantic)"
echo "grep: ${GREP_TIME}s (fast but imprecise)"

# Count accuracy
TOOL_COUNT=$(./scripts/CSharpExpertCli -s "$SOLUTION" find-references Logger --type class -o json | jq '.totalReferences')
GREP_COUNT=$(grep -r "Logger" --include="*.cs" . 2>/dev/null | wc -l)

echo "Tool found: $TOOL_COUNT references (actual usage)"
echo "grep found: $GREP_COUNT matches (includes comments, strings)"
```

---

These examples demonstrate real-world usage patterns and show how the C# Project Expert skill can be integrated into development workflows, automation scripts, and CI/CD pipelines.
