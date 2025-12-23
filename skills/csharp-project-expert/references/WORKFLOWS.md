# Workflows and Best Practices

This document provides detailed workflows and best practices for using the C# Project Expert skill effectively.

## Table of Contents

- [Safe Refactoring Workflow](#safe-refactoring-workflow)
- [Understanding Unfamiliar Code](#understanding-unfamiliar-code)
- [Code Quality Analysis](#code-quality-analysis)
- [Interface Implementation Workflow](#interface-implementation-workflow)
- [Dependency Auditing](#dependency-auditing)
- [Large-Scale Renaming](#large-scale-renaming)
- [Best Practices](#best-practices)
- [Anti-Patterns to Avoid](#anti-patterns-to-avoid)

---

## Safe Refactoring Workflow

When making changes to existing code, follow these steps to ensure safety and prevent breaking changes.

### Step 1: Understand Current Usage

Before changing any symbol, understand how it's currently used:

```bash
# Find all references to the method you want to change
./scripts/CSharpExpertCli -s MySolution.sln find-references GetById --type method -o text

# Count total references (using JSON)
./scripts/CSharpExpertCli -s MySolution.sln find-references GetById --type method | jq '.totalReferences'
```

**Why:** Knowing who uses your code helps you assess impact and plan the refactoring.

### Step 2: Check Method Signature

Understand the current contract:

```bash
# Get full signature with documentation
./scripts/CSharpExpertCli -s MySolution.sln signature GetById \
  --type method \
  --include-overloads \
  --include-docs \
  -o text
```

### Step 3: Preview Changes

Always preview before applying changes:

```bash
# Preview rename to see exact changes
./scripts/CSharpExpertCli -s MySolution.sln rename GetById FindById \
  --type method \
  --preview \
  -o text
```

Review the output carefully. Look for:
- Total number of changes
- Affected files
- Specific line-by-line edits

### Step 4: Apply Changes

Once you're confident:

```bash
# Apply the rename
./scripts/CSharpExpertCli -s MySolution.sln rename GetById FindById --type method
```

### Step 5: Verify Success

Check that no new errors were introduced:

```bash
# Check for compilation errors
./scripts/CSharpExpertCli -s MySolution.sln diagnostics --severity error

# Verify the new symbol exists
./scripts/CSharpExpertCli -s MySolution.sln check-symbol-exists FindById --type method
```

**Complete Example:**

```bash
# 1. Check current usage
echo "=== Current Usage ==="
./scripts/CSharpExpertCli -s MySolution.sln find-references GetById --type method -o text

# 2. Get signature
echo -e "\n=== Method Signature ==="
./scripts/CSharpExpertCli -s MySolution.sln signature GetById --type method -o text

# 3. Preview rename
echo -e "\n=== Preview Changes ==="
./scripts/CSharpExpertCli -s MySolution.sln rename GetById FindById \
  --type method \
  --preview \
  -o text

# 4. Ask for confirmation
read -p "Apply changes? (y/n) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    # 5. Apply rename
    ./scripts/CSharpExpertCli -s MySolution.sln rename GetById FindById --type method

    # 6. Verify
    echo -e "\n=== Checking for Errors ==="
    ./scripts/CSharpExpertCli -s MySolution.sln diagnostics --severity error
fi
```

---

## Understanding Unfamiliar Code

When exploring a codebase you're not familiar with, use this systematic approach.

### Step 1: Find the Type

Locate the class or interface:

```bash
# Find where UserService is defined
./scripts/CSharpExpertCli -s MySolution.sln find-definition UserService --type class -o text
```

### Step 2: Explore Members

See what the type can do:

```bash
# List all public members
./scripts/CSharpExpertCli -s MySolution.sln list-members UserService \
  --accessibility public \
  -o text
```

### Step 3: Check Inheritance

Understand the type hierarchy:

```bash
# See what it inherits and implements
./scripts/CSharpExpertCli -s MySolution.sln inheritance-tree UserService -o text
```

### Step 4: Analyze Dependencies

See what this type depends on:

```bash
# Find dependencies
./scripts/CSharpExpertCli -s MySolution.sln dependencies UserService -o text
```

### Step 5: Examine Key Methods

For each important method, get details:

```bash
# Get method signature with documentation
./scripts/CSharpExpertCli -s MySolution.sln signature GetUser \
  --type method \
  --include-docs \
  -o text

# See what calls this method
./scripts/CSharpExpertCli -s MySolution.sln find-callers GetUser -o text

# See what this method calls
./scripts/CSharpExpertCli -s MySolution.sln find-callees GetUser -o text
```

**Complete Exploration Script:**

```bash
#!/bin/bash
TYPE_NAME=$1
SOLUTION=$2

echo "=== Exploring $TYPE_NAME in $SOLUTION ==="

echo -e "\n1. Location:"
./scripts/CSharpExpertCli -s "$SOLUTION" find-definition "$TYPE_NAME" --type class -o text

echo -e "\n2. Public Members:"
./scripts/CSharpExpertCli -s "$SOLUTION" list-members "$TYPE_NAME" --accessibility public -o text

echo -e "\n3. Inheritance:"
./scripts/CSharpExpertCli -s "$SOLUTION" inheritance-tree "$TYPE_NAME" -o text

echo -e "\n4. Dependencies:"
./scripts/CSharpExpertCli -s "$SOLUTION" dependencies "$TYPE_NAME" -o text
```

Usage: `./explore-type.sh UserService MySolution.sln`

---

## Code Quality Analysis

Perform comprehensive code quality checks before committing or deploying.

### Pre-Commit Checklist

```bash
# 1. Check for compilation errors
echo "=== Checking for Errors ==="
ERROR_COUNT=$(./scripts/CSharpExpertCli -s MySolution.sln diagnostics --severity error | jq '.errors')

if [ "$ERROR_COUNT" -gt 0 ]; then
    echo "❌ Found $ERROR_COUNT errors"
    ./scripts/CSharpExpertCli -s MySolution.sln diagnostics --severity error -o text
    exit 1
fi

# 2. Check for warnings in changed files
echo -e "\n=== Checking Warnings in Changed Files ==="
for file in $(git diff --name-only --cached | grep '\.cs$'); do
    echo "Checking $file..."
    ./scripts/CSharpExpertCli -s MySolution.sln diagnostics --file "$file" --severity warning -o text
done

# 3. Look for unused code
echo -e "\n=== Checking for Unused Code ==="
./scripts/CSharpExpertCli -s MySolution.sln unused-code -o text

echo -e "\n✅ Code quality checks passed"
```

### Analyzing Technical Debt

Identify areas needing attention:

```bash
# Find unused code
./scripts/CSharpExpertCli -s MySolution.sln unused-code | \
  jq -r '.unusedSymbols[] | "\(.file):\(.line) - \(.symbol) (\(.kind))"'

# Find classes with many dependencies (potential code smell)
for class in $(./scripts/CSharpExpertCli -s MySolution.sln list-types | jq -r '.types[].name'); do
    dep_count=$(./scripts/CSharpExpertCli -s MySolution.sln dependencies "$class" | \
      jq '.dependencies.types | length')
    if [ "$dep_count" -gt 10 ]; then
        echo "$class has $dep_count dependencies"
    fi
done
```

---

## Interface Implementation Workflow

When implementing a new class that uses an interface.

### Step 1: Find Interface Definition

```bash
# Locate the interface
./scripts/CSharpExpertCli -s MySolution.sln find-definition IUserRepository --type interface -o text
```

### Step 2: See Existing Implementations

Learn from existing code:

```bash
# Find who already implements this
./scripts/CSharpExpertCli -s MySolution.sln find-implementations IUserRepository -o text
```

### Step 3: Generate Implementation Stubs

Get a starting point:

```bash
# Generate implementation code
./scripts/CSharpExpertCli -s MySolution.sln implement-interface IUserRepository -o text
```

### Step 4: Understand Interface Members

Know what you need to implement:

```bash
# List all interface members
./scripts/CSharpExpertCli -s MySolution.sln list-members IUserRepository -o text

# Get detailed signatures
./scripts/CSharpExpertCli -s MySolution.sln signature IUserRepository \
  --type interface \
  --include-docs \
  -o text
```

---

## Dependency Auditing

Analyze and manage dependencies in your solution.

### Finding Circular Dependencies

```bash
#!/bin/bash
# Check for potential circular dependencies

echo "Analyzing dependencies for circular references..."

for type in $(./scripts/CSharpExpertCli -s MySolution.sln list-types | jq -r '.types[].name'); do
    deps=$(./scripts/CSharpExpertCli -s MySolution.sln dependencies "$type" 2>/dev/null | \
      jq -r '.dependencies.types[]?' 2>/dev/null)

    for dep in $deps; do
        # Check if dependency also depends on original type
        reverse_deps=$(./scripts/CSharpExpertCli -s MySolution.sln dependencies "$dep" 2>/dev/null | \
          jq -r '.dependencies.types[]?' 2>/dev/null)

        if echo "$reverse_deps" | grep -q "^$type$"; then
            echo "⚠️  Circular dependency: $type <-> $dep"
        fi
    done
done
```

### Dependency Graph

Create a dependency map for a namespace:

```bash
# Analyze all types in a namespace
NAMESPACE="MyApp.Services"

echo "Dependency graph for $NAMESPACE:"
for type in $(./scripts/CSharpExpertCli -s MySolution.sln list-types --namespace "$NAMESPACE" | \
  jq -r '.types[].name'); do
    echo -e "\n$type depends on:"
    ./scripts/CSharpExpertCli -s MySolution.sln dependencies "$type" | \
      jq -r '.dependencies.types[]' | sed 's/^/  - /'
done
```

---

## Large-Scale Renaming

When renaming affects many files across the solution.

### Planning Phase

```bash
# 1. Assess impact
echo "=== Impact Assessment ==="
./scripts/CSharpExpertCli -s MySolution.sln find-references OldClassName --type class | \
  jq '{totalReferences, affectedFiles: [.references[].file] | unique | length}'

# 2. Preview all changes (including file rename)
echo -e "\n=== Preview Changes ==="
./scripts/CSharpExpertCli -s MySolution.sln rename OldClassName NewClassName \
  --type class \
  --preview \
  --rename-file \
  -o text > rename-preview.txt

# Review the preview file
less rename-preview.txt
```

### Execution Phase

```bash
# 1. Create a backup (git)
git add -A
git commit -m "Backup before renaming OldClassName to NewClassName"

# 2. Apply rename (IMPORTANT: use --rename-file for classes to rename the source file too)
echo "Applying rename..."
./scripts/CSharpExpertCli -s MySolution.sln rename OldClassName NewClassName \
  --type class \
  --rename-file

# 3. Verify compilation
echo -e "\n=== Checking for Errors ==="
if ./scripts/CSharpExpertCli -s MySolution.sln diagnostics --severity error | jq -e '.errors == 0' > /dev/null; then
    echo "✅ No compilation errors"
    git add -A
    git commit -m "Rename OldClassName to NewClassName"
else
    echo "❌ Compilation errors found - review and fix"
    ./scripts/CSharpExpertCli -s MySolution.sln diagnostics --severity error -o text
    echo "Consider: git reset --hard HEAD~1 to rollback"
fi
```

**IMPORTANT:** When renaming classes, interfaces, or other types, always use `--rename-file` to ensure the source file is renamed to match the type name. This follows C# naming conventions where the file name matches the primary type name.

---

## Best Practices

### 1. Always Preview Before Refactoring

```bash
# GOOD
./scripts/CSharpExpertCli -s MySolution.sln rename OldName NewName --preview
# Review output, then apply if satisfied

# BAD
./scripts/CSharpExpertCli -s MySolution.sln rename OldName NewName
# Direct application without preview
```

### 2. Use Specific Type Filters

```bash
# GOOD - Specific type
./scripts/CSharpExpertCli -s MySolution.sln find-definition GetUser --type method

# BAD - No type filter (may find wrong symbol)
./scripts/CSharpExpertCli -s MySolution.sln find-definition GetUser
```

### 3. Check for Errors After Changes

```bash
# After any refactoring
./scripts/CSharpExpertCli -s MySolution.sln diagnostics --severity error
```

### 4. Use JSON for Automation

```bash
# GOOD - Parse JSON in scripts
ERROR_COUNT=$(./scripts/CSharpExpertCli -s MySolution.sln diagnostics --severity error | jq '.errors')

# Less ideal - Parse text output
ERROR_COUNT=$(./scripts/CSharpExpertCli -s MySolution.sln -o text diagnostics --severity error | grep -c "Error")
```

### 5. Combine Commands for Complete Analysis

```bash
# Comprehensive method analysis
METHOD="ProcessOrder"

echo "=== Signature ==="
./scripts/CSharpExpertCli -s MySolution.sln signature "$METHOD" --type method -o text

echo -e "\n=== Who Calls This ==="
./scripts/CSharpExpertCli -s MySolution.sln find-callers "$METHOD" -o text

echo -e "\n=== What This Calls ==="
./scripts/CSharpExpertCli -s MySolution.sln find-callees "$METHOD" -o text
```

---

## Anti-Patterns to Avoid

### ❌ Don't Skip the Preview Step

```bash
# This is risky
./scripts/CSharpExpertCli -s MySolution.sln rename ImportantClass NewName --rename-file
```

**Why:** You might not realize the full scope of changes. Always preview first.

### ❌ Don't Ignore Compilation Errors

```bash
# Don't do this
./scripts/CSharpExpertCli -s MySolution.sln rename Foo Bar
# ... then continue without checking diagnostics
```

**Why:** Errors compound. Fix them immediately after refactoring.

### ❌ Don't Use Text Parsing When JSON Is Available

```bash
# Don't do this
COUNT=$(./scripts/CSharpExpertCli -s MySolution.sln -o text find-references GetUser | grep -c "Reference")

# Do this instead
COUNT=$(./scripts/CSharpExpertCli -s MySolution.sln find-references GetUser | jq '.totalReferences')
```

**Why:** Text output format may change. JSON is stable and precise.

### ❌ Don't Rename Without Checking References

```bash
# Don't do this
./scripts/CSharpExpertCli -s MySolution.sln rename GetUser FindUser --type method --preview

# Do this instead
./scripts/CSharpExpertCli -s MySolution.sln find-references GetUser --type method  # Check first
./scripts/CSharpExpertCli -s MySolution.sln rename GetUser FindUser --type method --preview
```

**Why:** You need context on how many places will be affected.

### ❌ Don't Make Assumptions About Symbol Types

```bash
# Don't do this (might find class instead of method)
./scripts/CSharpExpertCli -s MySolution.sln find-definition GetUser

# Do this instead
./scripts/CSharpExpertCli -s MySolution.sln find-definition GetUser --type method
```

**Why:** Ambiguous symbols might exist as both class names and method names.

---

## Quick Reference Cheatsheet

### Before Changing Code
1. `find-references` - See who uses it
2. `signature` - Understand the contract
3. `find-callers` / `find-callees` - Analyze call graph

### During Refactoring
1. `rename --preview` - See what will change
2. `rename` - Apply changes
3. `diagnostics --severity error` - Check for errors

### Exploring Code
1. `find-definition` - Where is it?
2. `list-members` - What can it do?
3. `inheritance-tree` - How does it fit in?
4. `dependencies` - What does it need?

### Code Quality
1. `diagnostics` - Find errors/warnings
2. `unused-code` - Find dead code
3. `check-symbol-exists` - Verify symbols exist
