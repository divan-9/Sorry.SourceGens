using Sorry.SourceGens;

namespace Sorry.SourceGens.Tests;

// Test models
public record Created(string Id, string Name);
public record Updated(string Id, string Name, DateTime UpdatedAt);
public record Deleted(string Id);

[OneOf]
public partial class EventEnvelope
{
    private readonly Created? created;
    private readonly Updated? updated;
}

[OneOf]
public partial class ExtendedEventEnvelope
{
    private readonly Created? created;
    private readonly Updated? updated;
    private readonly Deleted? deleted;

    // Test constructor - for testing exception scenarios only
    internal ExtendedEventEnvelope(Created? created, Updated? updated, Deleted? deleted)
    {
        this.created = created;
        this.updated = updated;
        this.deleted = deleted;
    }
}

// Test case for duplicate types (should not have implicit operators)
public record FirstStringEvent(string Value);
public record SecondStringEvent(string Value);

[OneOf]
public partial class DuplicateTypeEnvelope
{
    private readonly string? firstString;
    private readonly string? secondString;
}

public class FunctionalTests
{
    [Fact]
    public void CanCreateEventEnvelopeFromCreated()
    {
        var createdEvent = new Created("1", "Test");
        var envelope = EventEnvelope.FromCreated(createdEvent);

        var result = envelope.Map(
            onCreated: c => $"Created: {c.Name}",
            onUpdated: u => $"Updated: {u.Name}"
        );

        Assert.Equal("Created: Test", result);
    }

    [Fact]
    public void CanCreateEventEnvelopeFromUpdated()
    {
        var updatedEvent = new Updated("1", "Test", DateTime.Now);
        var envelope = EventEnvelope.FromUpdated(updatedEvent);

        var result = envelope.Map(
            onCreated: c => $"Created: {c.Name}",
            onUpdated: u => $"Updated: {u.Name}"
        );

        Assert.Equal("Updated: Test", result);
    }

    [Fact]
    public void CanUseImplicitOperatorFromCreated()
    {
        var createdEvent = new Created("1", "Test");
        EventEnvelope envelope = createdEvent; // Implicit conversion

        var result = envelope.Map(
            onCreated: c => $"Created: {c.Name}",
            onUpdated: u => $"Updated: {u.Name}"
        );

        Assert.Equal("Created: Test", result);
    }

    [Fact]
    public void CanUseImplicitOperatorFromUpdated()
    {
        var updatedEvent = new Updated("1", "Test", DateTime.Now);
        EventEnvelope envelope = updatedEvent; // Implicit conversion

        var result = envelope.Map(
            onCreated: c => $"Created: {c.Name}",
            onUpdated: u => $"Updated: {u.Name}"
        );

        Assert.Equal("Updated: Test", result);
    }

    [Fact]
    public void CanUseMatchWithActions()
    {
        var createdEvent = new Created("1", "Test");
        var envelope = EventEnvelope.FromCreated(createdEvent);
        var result = "";

        envelope.Match(
            onCreated: c => result = $"Handled Created: {c.Name}",
            onUpdated: u => result = $"Handled Updated: {u.Name}"
        );

        Assert.Equal("Handled Created: Test", result);
    }

    [Fact]
    public void CanWorkWithThreeVariants()
    {
        var createdEvent = new Created("1", "Test");
        var updatedEvent = new Updated("2", "Test2", DateTime.Now);
        var deletedEvent = new Deleted("3");

        var envelope1 = ExtendedEventEnvelope.FromCreated(createdEvent);
        var envelope2 = ExtendedEventEnvelope.FromUpdated(updatedEvent);
        var envelope3 = ExtendedEventEnvelope.FromDeleted(deletedEvent);

        var result1 = envelope1.Map(
            onCreated: c => "Created",
            onUpdated: u => "Updated",
            onDeleted: d => "Deleted"
        );

        var result2 = envelope2.Map(
            onCreated: c => "Created",
            onUpdated: u => "Updated",
            onDeleted: d => "Deleted"
        );

        var result3 = envelope3.Map(
            onCreated: c => "Created",
            onUpdated: u => "Updated",
            onDeleted: d => "Deleted"
        );

        Assert.Equal("Created", result1);
        Assert.Equal("Updated", result2);
        Assert.Equal("Deleted", result3);
    }

    [Fact]
    public void ThrowsWhenNoFieldIsSet()
    {
        // This should not be possible with the generated code, but let's verify the exception handling
        var envelope = new ExtendedEventEnvelope(null, null, null);

        Assert.Throws<InvalidOperationException>(() =>
            envelope.Map(
                onCreated: c => "Created",
                onUpdated: u => "Updated",
                onDeleted: d => "Deleted"
            )
        );

        Assert.Throws<InvalidOperationException>(() =>
            envelope.Match(
                onCreated: c => { },
                onUpdated: u => { },
                onDeleted: d => { }
            )
        );
    }

    [Fact]
    public void CanUseMapWithDefaultCase()
    {
        var createdEvent = new Created("1", "Test");
        var envelope = EventEnvelope.FromCreated(createdEvent);

        // Test with specific handler
        var result1 = envelope.Map(
            onDefault: () => "DEFAULT",
            onCreated: c => $"Created: {c.Name}",
            onUpdated: null // Optional parameter
        );
        Assert.Equal("Created: Test", result1);

        // Test with only default handler
        var result2 = envelope.Map(
            onDefault: () => "DEFAULT"
        );
        Assert.Equal("DEFAULT", result2);
    }

    [Fact]
    public void CanUseMatchWithDefaultCase()
    {
        var createdEvent = new Created("1", "Test");
        var envelope = EventEnvelope.FromCreated(createdEvent);
        var result = "";

        // Test with specific handler
        envelope.Match(
            onDefault: () => result = "DEFAULT",
            onCreated: c => result = $"Created: {c.Name}",
            onUpdated: null // Optional parameter
        );
        Assert.Equal("Created: Test", result);

        // Test with only default handler
        result = "";
        envelope.Match(
            onDefault: () => result = "DEFAULT"
        );
        Assert.Equal("DEFAULT", result);
    }

    [Fact]
    public void DuplicateTypesDoNotHaveImplicitOperators()
    {
        // This test verifies that when there are duplicate types,
        // implicit operators are not generated (which would cause compilation errors)
        var envelope = DuplicateTypeEnvelope.FromFirstString("test1");
        
        var result = envelope.Map(
            onFirstString: s => $"First: {s}",
            onSecondString: s => $"Second: {s}"
        );
        
        Assert.Equal("First: test1", result);

        // Verify we can still use factory methods
        var envelope2 = DuplicateTypeEnvelope.FromSecondString("test2");
        var result2 = envelope2.Map(
            onFirstString: s => $"First: {s}",
            onSecondString: s => $"Second: {s}"
        );
        
        Assert.Equal("Second: test2", result2);
    }
}