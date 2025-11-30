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
}