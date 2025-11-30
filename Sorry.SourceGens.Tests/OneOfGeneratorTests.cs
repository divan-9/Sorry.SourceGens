using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Reflection;

namespace Sorry.SourceGens.Tests;

public class OneOfGeneratorTests
{
    [Fact]
    public void GeneratesBasicOneOfImplementation()
    {
        var source = @"
using Sorry.SourceGens;

namespace TestNamespace
{
    public class Created { }
    public class Updated { }

    [OneOf]
    public partial class EventEnvelope
    {
        private readonly Created? created;
        private readonly Updated? updated;
    }
}";

        var (compilation, diagnostics) = GetGeneratedOutput(source);
        
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        
        var allFiles = compilation.SyntaxTrees.ToList();
        Assert.True(allFiles.Count >= 3, $"Expected at least 3 files (source, attribute, generated), got {allFiles.Count}");
        
        // Find the generated file (the one that's not our original source or attribute)
        var generatedFile = allFiles.FirstOrDefault(tree => 
            tree.ToString().Contains("FromCreated") && 
            tree.ToString().Contains("FromUpdated"));
        
        Assert.NotNull(generatedFile);
        
        var generatedCode = generatedFile.ToString();
        
        Assert.Contains("FromCreated", generatedCode);
        Assert.Contains("FromUpdated", generatedCode);
        Assert.Contains("public T Map<T>", generatedCode);
        Assert.Contains("public void Match", generatedCode);
        Assert.Contains("implicit operator", generatedCode);
    }

    [Fact]
    public void DoesNotGenerateForClassWithoutOneOfAttribute()
    {
        var source = @"
namespace TestNamespace
{
    public class Created { }
    public class Updated { }

    public partial class EventEnvelope
    {
        private readonly Created? created;
        private readonly Updated? updated;
    }
}";

        var (compilation, diagnostics) = GetGeneratedOutput(source);
        
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        
        var allFiles = compilation.SyntaxTrees.ToList();
        
        // Should only have original source and attribute definition, no generated OneOf code
        var oneOfGeneratedFile = allFiles.FirstOrDefault(tree => 
            tree.ToString().Contains("FromCreated") && 
            tree.ToString().Contains("FromUpdated"));
        
        Assert.Null(oneOfGeneratedFile);
    }

    [Fact] 
    public void DoesNotGenerateForClassWithoutPartialModifier()
    {
        var source = @"
using Sorry.SourceGens;

namespace TestNamespace
{
    public class Created { }
    public class Updated { }

    [OneOf]
    public class EventEnvelope
    {
        private readonly Created? created;
        private readonly Updated? updated;
    }
}";

        var (compilation, diagnostics) = GetGeneratedOutput(source);
        
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        
        var allFiles = compilation.SyntaxTrees.ToList();
        
        // Should only have original source and attribute definition, no generated OneOf code
        var oneOfGeneratedFile = allFiles.FirstOrDefault(tree => 
            tree.ToString().Contains("FromCreated") && 
            tree.ToString().Contains("FromUpdated"));
        
        Assert.Null(oneOfGeneratedFile);
    }

    [Fact]
    public void DoesNotGenerateImplicitOperatorsForDuplicateTypes()
    {
        var source = @"
using Sorry.SourceGens;

namespace TestNamespace
{
    [OneOf]
    public partial class DuplicateTypeEnvelope
    {
        private readonly string? firstString;
        private readonly string? secondString;
    }
}";

        var (compilation, diagnostics) = GetGeneratedOutput(source);
        
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        
        var generatedFile = compilation.SyntaxTrees.FirstOrDefault(tree => 
            tree.ToString().Contains("FromFirstString") && 
            tree.ToString().Contains("FromSecondString"));
        
        Assert.NotNull(generatedFile);
        
        var generatedCode = generatedFile.ToString();
        
        Assert.Contains("FromFirstString", generatedCode);
        Assert.Contains("FromSecondString", generatedCode);
        Assert.Contains("public T Map<T>", generatedCode);
        Assert.Contains("public void Match", generatedCode);
        
        // Should NOT contain implicit operators for duplicate types
        Assert.DoesNotContain("implicit operator", generatedCode);
    }

    [Fact]
    public void GeneratesMapAndMatchOverloadsWithDefaults()
    {
        var source = @"
using Sorry.SourceGens;

namespace TestNamespace
{
    public class Created { }
    public class Updated { }

    [OneOf]
    public partial class EventEnvelope
    {
        private readonly Created? created;
        private readonly Updated? updated;
    }
}";

        var (compilation, diagnostics) = GetGeneratedOutput(source);
        
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        
        var generatedFile = compilation.SyntaxTrees.FirstOrDefault(tree => 
            tree.ToString().Contains("FromCreated") && 
            tree.ToString().Contains("FromUpdated"));
        
        Assert.NotNull(generatedFile);
        
        var generatedCode = generatedFile.ToString();
        
        // Should have both regular and default overloads
        Assert.Contains("public T Map<T>(", generatedCode);
        Assert.Contains("Func<T> onDefault", generatedCode);
        Assert.Contains("Action onDefault", generatedCode);
        Assert.Contains("= null", generatedCode); // Optional parameters
    }

    private static (Compilation compilation, ImmutableArray<Diagnostic> diagnostics) GetGeneratedOutput(string source)
    {
        var attributeSource = @"
using System;

namespace Sorry.SourceGens
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class OneOfAttribute : Attribute
    {
    }
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var attributeTree = CSharpSyntaxTree.ParseText(attributeSource);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree, attributeTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new OneOfGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        var runResult = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        return (outputCompilation, diagnostics);
    }
}