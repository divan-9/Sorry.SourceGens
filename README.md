# Sorry.SourceGens

A C# source generator that automatically implements the OneOf pattern for discriminated unions.

## Overview

Often we need to create OneOf types to represent events that can be of different kinds. This source generator helps you reduce boilerplate code by automatically generating the OneOf pattern implementation.

Instead of writing this manually:

```csharp
public class EventEnvelope
{
    private readonly Created? created;
    private readonly Updated? updated;

    private EventEnvelope(
        Created? created,
        Updated? updated)
    {
        this.created = created;
        this.updated = updated;
    }

    public static EventEnvelope FromCreated(
        Created created) =>
        new EventEnvelope(
            created: created,
            updated: null);

    public static EventEnvelope FromUpdated(
        Updated updated) =>
        new EventEnvelope(
            created: null,
            updated: updated);

    public static implicit operator EventEnvelope(Created created) => FromCreated(created);

    public static implicit operator EventEnvelope(Updated updated) => FromUpdated(updated);

    public T Map<T>(
        Func<Created, T> onCreated,
        Func<Updated, T> onUpdated)
    {
        if (this.created is not null)
        {
            return onCreated(this.created);
        }
        if (this.updated is not null)
        {
            return onUpdated(this.updated);
        }
        throw new InvalidOperationException("EventEnvelope must contain one of: Created, Updated");
    }

    // Map with default case
    public T Map<T>(
        Func<T> onDefault,
        Func<Created, T>? onCreated = null,
        Func<Updated, T>? onUpdated = null)
    {
        if (this.created is not null && onCreated is not null)
        {
            return onCreated(this.created);
        }
        if (this.updated is not null && onUpdated is not null)
        {
            return onUpdated(this.updated);
        }
        return onDefault();
    }

    public void Match(
        Action<Created> onCreated,
        Action<Updated> onUpdated)
    {
        if (this.created is not null)
        {
            onCreated(this.created);
            return;
        }
        if (this.updated is not null)
        {
            onUpdated(this.updated);
            return;
        }
        throw new InvalidOperationException("EventEnvelope must contain one of: Created, Updated");
    }

    // Match with default case
    public void Match(
        Action onDefault,
        Action<Created>? onCreated = null,
        Action<Updated>? onUpdated = null)
    {
        if (this.created is not null && onCreated is not null)
        {
            onCreated(this.created);
            return;
        }
        if (this.updated is not null && onUpdated is not null)
        {
            onUpdated(this.updated);
            return;
        }
        onDefault();
    }
}
```

You can simply write:

```csharp
[Sorry.SourceGens.OneOf]
public partial class EventEnvelope
{
    private readonly Created? created;
    private readonly Updated? updated;
}
```

## Usage

1. Mark your class with the `[Sorry.SourceGens.OneOf]` attribute
2. Make the class `partial`
3. Add private readonly nullable fields for each variant

The source generator will automatically generate:
- Private constructor
- Factory methods (`FromXxx`)
- Implicit operators (when types are unique)
- `Map<T>` method for functional-style pattern matching
- `Match` method for action-based pattern matching
- Overloaded `Map<T>` and `Match` methods with default cases

## Features

- ✅ Supports multiple variants (2 or more)
- ✅ Automatic factory method generation
- ✅ Smart implicit operator support (skipped for duplicate types)
- ✅ Functional pattern matching with `Map<T>`
- ✅ Action-based pattern matching with `Match`
- ✅ Default case overloads for `Map<T>` and `Match`
- ✅ Optional parameter support for partial case handling
- ✅ Proper exception handling for invalid states
- ✅ Nullable reference type annotations
- ✅ Respects existing constructors (won't duplicate if you provide your own)

## Examples

### Basic Two-Variant OneOf

```csharp
public record Created(string Id, string Name);
public record Updated(string Id, string Name, DateTime UpdatedAt);

[Sorry.SourceGens.OneOf]
public partial class EventEnvelope
{
    private readonly Created? created;
    private readonly Updated? updated;
}

// Usage:
var createdEvent = new Created("1", "Test");
EventEnvelope envelope = createdEvent; // Implicit conversion

var result = envelope.Map(
    onCreated: c => $"Created: {c.Name}",
    onUpdated: u => $"Updated: {u.Name}"
);
// result: "Created: Test"

envelope.Match(
    onCreated: c => Console.WriteLine($"Processing created event: {c.Name}"),
    onUpdated: u => Console.WriteLine($"Processing updated event: {u.Name}")
);
```

### Three-Variant OneOf

```csharp
public record Created(string Id, string Name);
public record Updated(string Id, string Name, DateTime UpdatedAt);
public record Deleted(string Id);

[Sorry.SourceGens.OneOf]
public partial class ExtendedEventEnvelope
{
    private readonly Created? created;
    private readonly Updated? updated;
    private readonly Deleted? deleted;
}

// Usage:
var envelope = ExtendedEventEnvelope.FromDeleted(new Deleted("1"));

var action = envelope.Map(
    onCreated: c => "CREATE",
    onUpdated: u => "UPDATE", 
    onDeleted: d => "DELETE"
);
// action: "DELETE"
```

### Default Case Handling

```csharp
var envelope = EventEnvelope.FromCreated(new Created("1", "Test"));

// Map with default case - only handle some variants
var result = envelope.Map(
    onDefault: () => "Unknown event",
    onCreated: c => $"Created: {c.Name}"
    // onUpdated intentionally omitted
);
// result: "Created: Test"

// Match with default case
envelope.Match(
    onDefault: () => Console.WriteLine("Default action"),
    onCreated: c => Console.WriteLine($"Created: {c.Name}")
);
```

### Duplicate Types (No Implicit Operators)

When multiple fields have the same type, implicit operators are automatically skipped to prevent compilation errors:

```csharp
[Sorry.SourceGens.OneOf]
public partial class DuplicateTypeEnvelope
{
    private readonly string? firstMessage;
    private readonly string? secondMessage;
}

// Must use factory methods (no implicit conversion available)
var envelope1 = DuplicateTypeEnvelope.FromFirstMessage("Hello");
var envelope2 = DuplicateTypeEnvelope.FromSecondMessage("World");

var result = envelope1.Map(
    onFirstMessage: msg => $"First: {msg}",
    onSecondMessage: msg => $"Second: {msg}"
);
// result: "First: Hello"
```

## Architecture

The source generator uses a **StringBuilder-based code generation** approach that:

- **Direct generation**: All code is generated directly using StringBuilder for maximum reliability
- **Zero dependencies**: No external template engines or dependencies to worry about
- **Fast compilation**: Minimal overhead during build time
- **Easy maintenance**: Straightforward code generation logic that's easy to understand and modify

## Installation

### Using Project Reference (Development)

Add the source generator to your project:

```xml
<ItemGroup>
    <ProjectReference Include="path/to/Sorry.SourceGens.csproj" />
    <Analyzer Include="path/to/Sorry.SourceGens/bin/Debug/netstandard2.0/Sorry.SourceGens.dll" />
</ItemGroup>
```

### Using NuGet Package

The project includes NuGet packaging configuration. Build the package with:

```bash
dotnet pack Sorry.SourceGens/Sorry.SourceGens.csproj --configuration Release
```

Then reference it in your project:

```xml
<ItemGroup>
    <PackageReference Include="Sorry.SourceGens" Version="1.0.0" />
</ItemGroup>
```

## Requirements

- .NET Standard 2.0 or higher
- C# 8.0 or higher (for nullable reference types and pattern matching)

## Generated Code Structure

For each `[OneOf]` class, the generator creates:

1. **Private Constructor** (if not already present)
2. **Factory Methods**: `FromXxx(Type value)` for each variant
3. **Implicit Operators**: `Type -> OneOfClass` (only when types are unique)
4. **Map Method**: `T Map<T>(Func<Type1, T>, Func<Type2, T>, ...)`
5. **Map with Default**: `T Map<T>(Func<T> onDefault, Func<Type1, T>?, ...)`
6. **Match Method**: `Match(Action<Type1>, Action<Type2>, ...)`
7. **Match with Default**: `Match(Action onDefault, Action<Type1>?, ...)`

## Testing

Run the comprehensive test suite:

```bash
dotnet test
```

Run the example project:

```bash
dotnet run --project Sorry.SourceGens.Example
```

## Contributing

This project follows the standard GitHub flow. Please feel free to submit issues and pull requests.

## License

MIT