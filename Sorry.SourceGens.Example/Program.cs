using System;
using System.Linq;
using Sorry.SourceGens;

// Define event types
public record Created(string Id, string Name);
public record Updated(string Id, string Name, DateTime UpdatedAt);
public record Deleted(string Id);

// Simple OneOf with two variants
[OneOf]
public partial class EventEnvelope
{
    private readonly Created? created;
    private readonly Updated? updated;
}

// Extended OneOf with three variants
[OneOf]
public partial class ExtendedEventEnvelope
{
    private readonly Created? created;
    private readonly Updated? updated;
    private readonly Deleted? deleted;
}

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Sorry.SourceGens OneOf Pattern Demo");
        Console.WriteLine("===================================");

        DemoBasicOneOf();
        Console.WriteLine();
        DemoExtendedOneOf();
        Console.WriteLine();
        DemoFunctionalPatternMatching();
        Console.WriteLine();
        DemoEquality();
    }

    static void DemoBasicOneOf()
    {
        Console.WriteLine("📧 Basic OneOf Demo (Created/Updated)");

        var createdEvent = new Created("1", "New Product");
        var updatedEvent = new Updated("2", "Updated Product", DateTime.Now);

        // Using factory methods
        var envelope1 = EventEnvelope.FromCreated(createdEvent);
        var envelope2 = EventEnvelope.FromUpdated(updatedEvent);

        Console.WriteLine($"✅ Created envelope: {ProcessEventEnvelope(envelope1)}");
        Console.WriteLine($"✅ Updated envelope: {ProcessEventEnvelope(envelope2)}");

        // Using implicit operators
        EventEnvelope envelope3 = createdEvent;
        EventEnvelope envelope4 = updatedEvent;

        Console.WriteLine($"✅ Implicit Created: {ProcessEventEnvelope(envelope3)}");
        Console.WriteLine($"✅ Implicit Updated: {ProcessEventEnvelope(envelope4)}");
    }

    static void DemoExtendedOneOf()
    {
        Console.WriteLine("🗂️  Extended OneOf Demo (Created/Updated/Deleted)");

        var events = new ExtendedEventEnvelope[]
        {
            ExtendedEventEnvelope.FromCreated(new Created("1", "Product A")),
            ExtendedEventEnvelope.FromUpdated(new Updated("2", "Product B", DateTime.Now)),
            ExtendedEventEnvelope.FromDeleted(new Deleted("3"))
        };

        foreach (var eventEnv in events)
        {
            ProcessEventWithMatch(eventEnv);
        }
    }

    static void DemoFunctionalPatternMatching()
    {
        Console.WriteLine("🔄 Functional Pattern Matching Demo");

        var events = new ExtendedEventEnvelope[]
        {
            ExtendedEventEnvelope.FromCreated(new Created("100", "Laptop")),
            ExtendedEventEnvelope.FromUpdated(new Updated("101", "Mouse", DateTime.Now.AddMinutes(-30))),
            ExtendedEventEnvelope.FromDeleted(new Deleted("102"))
        };

        var summary = events.Select(GetEventSummary).ToArray();

        Console.WriteLine($"📊 Event Summary: [{string.Join(", ", summary)}]");
    }

    static string ProcessEventEnvelope(EventEnvelope envelope)
    {
        return envelope.Map(
            onCreated: c => $"Processing CREATE for '{c.Name}' (ID: {c.Id})",
            onUpdated: u => $"Processing UPDATE for '{u.Name}' (ID: {u.Id}) at {u.UpdatedAt:HH:mm:ss}"
        );
    }

    static void ProcessEventWithMatch(ExtendedEventEnvelope envelope)
    {
        envelope.Match(
            onCreated: c => Console.WriteLine($"🆕 NEW: '{c.Name}' created with ID {c.Id}"),
            onUpdated: u => Console.WriteLine($"✏️  MODIFY: '{u.Name}' (ID: {u.Id}) updated at {u.UpdatedAt:HH:mm:ss}"),
            onDeleted: d => Console.WriteLine($"🗑️  DELETE: Entity with ID {d.Id} was removed")
        );
    }

    static string GetEventSummary(ExtendedEventEnvelope envelope)
    {
        return envelope.Map(
            onCreated: _ => "C",
            onUpdated: _ => "U",
            onDeleted: _ => "D"
        );
    }

    static void DemoEquality()
    {
        Console.WriteLine("⚖️  Equality Demo");

        var created1 = new Created("1", "Product A");
        var created2 = new Created("1", "Product A");
        var created3 = new Created("2", "Product B");
        var updated1 = new Updated("1", "Product A", DateTime.Now);

        var envelope1 = EventEnvelope.FromCreated(created1);
        var envelope2 = EventEnvelope.FromCreated(created2);
        var envelope3 = EventEnvelope.FromCreated(created3);
        var envelope4 = EventEnvelope.FromUpdated(updated1);

        Console.WriteLine($"✅ Same Created objects: {envelope1.Equals(envelope2)} (should be True)");
        Console.WriteLine($"❌ Different Created objects: {envelope1.Equals(envelope3)} (should be False)");
        Console.WriteLine($"❌ Created vs Updated: {envelope1.Equals(envelope4)} (should be False)");
        Console.WriteLine($"✅ == operator: {envelope1 == envelope2} (should be True)");
        Console.WriteLine($"✅ != operator: {envelope1 != envelope3} (should be True)");
        Console.WriteLine($"✅ HashCode consistency: {envelope1.GetHashCode() == envelope2.GetHashCode()} (should be True)");
    }
}