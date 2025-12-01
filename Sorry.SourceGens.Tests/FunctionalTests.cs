using Sorry.SourceGens;
using System.Reflection;

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
    public void ThrowsWhenNoFieldIsSetUsingReflection()
    {
        // Since we can't create invalid states with the public API,
        // we test exception handling using reflection to create an invalid state
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type - this is intentional for testing
        var envelope = (ExtendedEventEnvelope)Activator.CreateInstance(
            typeof(ExtendedEventEnvelope), 
            BindingFlags.NonPublic | BindingFlags.Instance, 
            null, 
            new object[] { null, null, null }, 
            null)!;
#pragma warning restore CS8625

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

    [Fact]
    public void EqualityWorksForSameValues()
    {
        var created1 = new Created("1", "Test");
        var created2 = new Created("1", "Test"); // Same values
        
        var envelope1 = EventEnvelope.FromCreated(created1);
        var envelope2 = EventEnvelope.FromCreated(created2);
        
        Assert.True(envelope1.Equals(envelope2));
        Assert.True(envelope1 == envelope2);
        Assert.False(envelope1 != envelope2);
        Assert.Equal(envelope1.GetHashCode(), envelope2.GetHashCode());
    }

    [Fact]
    public void EqualityWorksForDifferentValues()
    {
        var created1 = new Created("1", "Test1");
        var created2 = new Created("2", "Test2"); // Different values
        
        var envelope1 = EventEnvelope.FromCreated(created1);
        var envelope2 = EventEnvelope.FromCreated(created2);
        
        Assert.False(envelope1.Equals(envelope2));
        Assert.False(envelope1 == envelope2);
        Assert.True(envelope1 != envelope2);
    }

    [Fact]
    public void EqualityWorksForDifferentTypes()
    {
        var created = new Created("1", "Test");
        var updated = new Updated("1", "Test", DateTime.Now);
        
        var envelope1 = EventEnvelope.FromCreated(created);
        var envelope2 = EventEnvelope.FromUpdated(updated);
        
        Assert.False(envelope1.Equals(envelope2));
        Assert.False(envelope1 == envelope2);
        Assert.True(envelope1 != envelope2);
    }

    [Fact]
    public void EqualityWorksWithReferenceEquality()
    {
        var created = new Created("1", "Test");
        var envelope = EventEnvelope.FromCreated(created);
        var sameReference = envelope;
        
        Assert.True(envelope.Equals(sameReference)); // Reference equality
        Assert.True(envelope == sameReference);
        Assert.False(envelope != sameReference);
    }

    [Fact]
    public void EqualityWorksWithNull()
    {
        var created = new Created("1", "Test");
        var envelope = EventEnvelope.FromCreated(created);
        
        Assert.False(envelope.Equals(null));
        Assert.False(envelope == null);
        Assert.False(null == envelope);
        Assert.True(envelope != null);
        Assert.True(null != envelope);
    }

    [Fact]
    public void EqualityWorksWithObjectType()
    {
        var created1 = new Created("1", "Test");
        var created2 = new Created("1", "Test");
        
        var envelope1 = EventEnvelope.FromCreated(created1);
        var envelope2 = EventEnvelope.FromCreated(created2);
        
        // Test object.Equals override
        object obj1 = envelope1;
        object obj2 = envelope2;
        object differentObj = "not an envelope";
        
        Assert.True(obj1.Equals(obj2));
        Assert.False(obj1.Equals(differentObj));
        Assert.False(obj1.Equals(null));
    }

    [Fact]
    public void EqualityWorksForExtendedOneOfTypes()
    {
        var created1 = new Created("1", "Test");
        var created2 = new Created("1", "Test");
        var deleted1 = new Deleted("1");
        var deleted2 = new Deleted("1");
        
        var envelope1 = ExtendedEventEnvelope.FromCreated(created1);
        var envelope2 = ExtendedEventEnvelope.FromCreated(created2);
        var envelope3 = ExtendedEventEnvelope.FromDeleted(deleted1);
        var envelope4 = ExtendedEventEnvelope.FromDeleted(deleted2);
        
        // Same type, same values
        Assert.True(envelope1.Equals(envelope2));
        Assert.True(envelope3.Equals(envelope4));
        Assert.True(envelope1 == envelope2);
        Assert.True(envelope3 == envelope4);
        
        // Different types
        Assert.False(envelope1.Equals(envelope3));
        Assert.False(envelope1 == envelope3);
        Assert.True(envelope1 != envelope3);
        
        // Hash codes should be equal for equal objects
        Assert.Equal(envelope1.GetHashCode(), envelope2.GetHashCode());
        Assert.Equal(envelope3.GetHashCode(), envelope4.GetHashCode());
    }

    [Fact]
    public void EqualityWorksInCollections()
    {
        var created1 = new Created("1", "Test");
        var created2 = new Created("1", "Test");
        var created3 = new Created("2", "Different");
        
        var envelope1 = EventEnvelope.FromCreated(created1);
        var envelope2 = EventEnvelope.FromCreated(created2);
        var envelope3 = EventEnvelope.FromCreated(created3);
        
        var list = new List<EventEnvelope> { envelope1 };
        
        Assert.Contains(envelope2, list); // Should find equal object
        Assert.DoesNotContain(envelope3, list); // Should not find different object
        
        var set = new HashSet<EventEnvelope> { envelope1, envelope2, envelope3 };
        
        // Should only have 2 unique items (envelope1 and envelope2 are equal)
        Assert.Equal(2, set.Count);
        Assert.Contains(envelope1, set);
        Assert.Contains(envelope3, set);
    }

    [Fact]
    public void EqualityWorksInDictionaries()
    {
        var created1 = new Created("1", "Test");
        var created2 = new Created("1", "Test");
        
        var envelope1 = EventEnvelope.FromCreated(created1);
        var envelope2 = EventEnvelope.FromCreated(created2);
        
        var dict = new Dictionary<EventEnvelope, string>
        {
            [envelope1] = "value1"
        };
        
        // Should be able to retrieve value using equal key
        Assert.True(dict.ContainsKey(envelope2));
        Assert.Equal("value1", dict[envelope2]);
        
        // Adding with equal key should update existing entry
        dict[envelope2] = "value2";
        Assert.Single(dict);
        Assert.Equal("value2", dict[envelope1]);
    }

    [Fact]
    public void HashCodeIsConsistent()
    {
        var created = new Created("1", "Test");
        var envelope = EventEnvelope.FromCreated(created);
        
        var hash1 = envelope.GetHashCode();
        var hash2 = envelope.GetHashCode();
        
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashCodeDiffersForDifferentObjects()
    {
        var created1 = new Created("1", "Test1");
        var created2 = new Created("2", "Test2");
        var updated = new Updated("1", "Test1", DateTime.Now);
        
        var envelope1 = EventEnvelope.FromCreated(created1);
        var envelope2 = EventEnvelope.FromCreated(created2);
        var envelope3 = EventEnvelope.FromUpdated(updated);
        
        var hash1 = envelope1.GetHashCode();
        var hash2 = envelope2.GetHashCode();
        var hash3 = envelope3.GetHashCode();
        
        // Hash codes should be different (though collisions are possible, very unlikely with these values)
        Assert.NotEqual(hash1, hash2);
        Assert.NotEqual(hash1, hash3);
        Assert.NotEqual(hash2, hash3);
    }
}