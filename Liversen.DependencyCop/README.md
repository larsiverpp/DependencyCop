# DependencyCop

This package contains a number of Roslyn analyzer rules using the .NET Compiler Platform. The rules enforce certain restrictions on dependencies between code in different namespaces.

[Rule DC1001: Using namespace statements must not reference disallowed namespaces](https://github.com/larsiverpp/DependencyCop/blob/main/Liversen.DependencyCop/Documentation/DC1001.md)

[Rule DC1002: Code must not refer code in descendant namespaces](https://github.com/larsiverpp/DependencyCop/blob/main/Liversen.DependencyCop/Documentation/DC1002.md)

[Rule DC1003: Code must not contain namespace cycles](https://github.com/larsiverpp/DependencyCop/blob/main/Liversen.DependencyCop/Documentation/DC1003.md)

[Rule DC1004: Rule DC1001 is not configured](https://github.com/larsiverpp/DependencyCop/blob/main/Liversen.DependencyCop/Documentation/DC1004.md)

## Changelog

### 1.0 - 2.0
- Added FixProvider for DC1001.