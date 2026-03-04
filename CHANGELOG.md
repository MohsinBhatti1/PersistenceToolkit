# Changelog

All notable changes to PersistenceToolkit will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [10.0.2] - 2025-01-XX

### Added
- Multi-targeting support for .NET 6.0, .NET 8.0, and .NET 9.0
- Conditional Entity Framework Core package references based on target framework

### Changed
- Updated all projects to support multiple target frameworks
- Collection expressions replaced with traditional syntax for .NET 6.0 compatibility

### Fixed
- Build compatibility issues with .NET 6.0
- C# language version compatibility across all target frameworks

## [10.0.1] - 2025-01-XX

### Changed
- Updated package metadata and descriptions

## [10.0.0] - 2025-01-XX

### Added
- Initial release of PersistenceToolkit
- Domain layer with `Entity` base class and `IAggregateRoot` interface
- Abstractions layer with repository interfaces and specification pattern
- Persistence layer with EF Core integration
- Aggregate root enforcement
- Smart entity state management with snapshot-based change detection
- Automatic audit logging (CreatedBy, UpdatedBy, TenantId)
- Soft delete support
- Multi-tenant isolation
- Navigation ignore rules for safe partial updates
- Specification pattern implementation for querying
- Repository pattern with read and write separation

### Credits
- Specification pattern implementation inspired by [Ardalis.Specification](https://github.com/ardalis/Specification)

