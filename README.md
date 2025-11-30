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
        else if (this.updated is not null)
        {
            return onUpdated(this.updated);
        }
        else
        {
            throw new InvalidOperationException("EventEnvelope must contain either Created or Updated event.");
        }
    }

    public void Match(
        Action<Created> onCreated,
        Action<Updated> onUpdated)
    {
        if (this.created is not null)
        {
            onCreated(this.created);
        }
        else if (this.updated is not null)
        {
            onUpdated(this.updated);
        }
        else
        {
            throw new InvalidOperationException("EventEnvelope must contain either Created or Updated event.");
        }
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
- Implicit operators
- `Map<T>` method for functional-style pattern matching
- `Match` method for action-based pattern matching

## Features

- ✅ Supports multiple variants (2 or more)
- ✅ Automatic factory method generation
- ✅ Implicit operator support
- ✅ Functional pattern matching with `Map<T>`
- ✅ Action-based pattern matching with `Match`
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
public partial class EventEnvelope
{
    private readonly Created? created;
    private readonly Updated? updated;
    private readonly Deleted? deleted;
}

// Usage:
var envelope = EventEnvelope.FromDeleted(new Deleted("1"));

var action = envelope.Map(
    onCreated: c => "CREATE",
    onUpdated: u => "UPDATE", 
    onDeleted: d => "DELETE"
);
// action: "DELETE"
```

## Installation

Add the source generator to your project:

```xml
<ItemGroup>
    <ProjectReference Include="path/to/Sorry.SourceGens.csproj" />
    <Analyzer Include="path/to/Sorry.SourceGens.dll" />
</ItemGroup>
```

## Requirements

- .NET Standard 2.0 or higher
- C# 8.0 or higher (for nullable reference types and pattern matching)

## Contributing

This project follows the standard GitHub flow. Please feel free to submit issues and pull requests.

## License

MIT