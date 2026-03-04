# Contributing to PersistenceToolkit

Thank you for your interest in contributing to PersistenceToolkit! This document provides guidelines and instructions for contributing.

## Code of Conduct

By participating in this project, you agree to maintain a respectful and inclusive environment for all contributors.

## How to Contribute

### Reporting Bugs

If you find a bug, please create an issue with:
- A clear, descriptive title
- Steps to reproduce the issue
- Expected behavior
- Actual behavior
- .NET version and EF Core version you're using
- Any relevant code snippets or error messages

### Suggesting Enhancements

We welcome suggestions for new features or improvements. Please create an issue with:
- A clear description of the enhancement
- Use cases and examples
- Any potential implementation considerations

### Pull Requests

1. **Fork the repository** and create a new branch from `main`
2. **Make your changes** following the coding standards below
3. **Add tests** for new features or bug fixes
4. **Update documentation** if needed (README, CHANGELOG, etc.)
5. **Ensure all tests pass** and the solution builds successfully
6. **Submit a pull request** with a clear description of your changes

## Coding Standards

### General Guidelines

- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and single-purpose
- Write unit tests for new functionality

### Architecture Principles

- **Layer Separation**: Maintain strict boundaries between Domain, Abstractions, and Persistence layers
- **DDD First**: Always respect aggregate root boundaries
- **Repository Pattern**: Use repository interfaces, never expose DbContext directly
- **Specification Pattern**: Use specifications for all queries

### Code Style

- Use `var` when the type is obvious
- Prefer expression-bodied members when appropriate
- Use nullable reference types
- Follow async/await best practices

## Testing

- All new features should include unit tests
- Tests should be in the `PersistenceToolkit.Tests` project
- Use descriptive test method names following the pattern: `MethodName_Scenario_ExpectedResult`
- Ensure tests pass for all target frameworks (.NET 6.0, 8.0, 9.0)

## Documentation

- Update README.md if you add new features or change existing behavior
- Update CHANGELOG.md with your changes
- Add XML documentation comments for public APIs
- Update cursor rules if you change patterns or best practices

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

## Questions?

If you have questions about contributing, please open an issue or contact the maintainers.

Thank you for contributing to PersistenceToolkit! 🎉

