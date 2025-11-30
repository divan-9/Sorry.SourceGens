using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sorry.SourceGens;

[Generator]
public class OneOfGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new OneOfSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not OneOfSyntaxReceiver receiver)
            return;

        var compilation = context.Compilation;
        var oneOfAttributeSymbol = compilation.GetTypeByMetadataName("Sorry.SourceGens.OneOfAttribute");

        if (oneOfAttributeSymbol == null)
            return;

        foreach (var candidateClass in receiver.CandidateClasses)
        {
            var semanticModel = compilation.GetSemanticModel(candidateClass.SyntaxTree);
            var classSymbol = semanticModel.GetDeclaredSymbol(candidateClass);

            if (classSymbol == null)
                continue;

            var hasOneOfAttribute = classSymbol.GetAttributes()
                .Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, oneOfAttributeSymbol));

            if (!hasOneOfAttribute)
                continue;

            var fields = GetNullableFields(candidateClass);
            if (fields.Count == 0)
                continue;

            var source = GenerateOneOfImplementation(candidateClass, fields);
            context.AddSource($"{classSymbol.Name}_OneOf.g.cs", source);
        }
    }

    private static List<FieldInfo> GetNullableFields(ClassDeclarationSyntax classDeclaration)
    {
        var fields = new List<FieldInfo>();

        foreach (var member in classDeclaration.Members.OfType<FieldDeclarationSyntax>())
        {
            if (member.Modifiers.Any(SyntaxKind.PrivateKeyword) && member.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
            {
                foreach (var variable in member.Declaration.Variables)
                {
                    var typeName = member.Declaration.Type.ToString();
                    if (typeName.EndsWith("?"))
                    {
                        var fieldName = variable.Identifier.ValueText;
                        var baseTypeName = typeName.TrimEnd('?');
                        fields.Add(new FieldInfo(fieldName, baseTypeName));
                    }
                }
            }
        }

        return fields;
    }

    private static string GenerateOneOfImplementation(ClassDeclarationSyntax classDeclaration, List<FieldInfo> fields)
    {
        var className = classDeclaration.Identifier.ValueText;
        var namespaceName = GetNamespace(classDeclaration);
        var hasExistingConstructor = HasExistingConstructor(classDeclaration, fields);

        var sb = new StringBuilder();
        
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(namespaceName))
        {
            sb.AppendLine($"namespace {namespaceName};");
            sb.AppendLine();
        }

        sb.AppendLine($"partial class {className}");
        sb.AppendLine("{");

        // Generate constructor only if it doesn't exist
        if (!hasExistingConstructor)
        {
            sb.AppendLine($"    private {className}(");
            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                sb.Append($"        {field.TypeName}? {field.Name}");
                if (i < fields.Count - 1)
                    sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("    )");
            sb.AppendLine("    {");
            foreach (var field in fields)
            {
                sb.AppendLine($"        this.{field.Name} = {field.Name};");
            }
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        // Generate factory methods
        foreach (var field in fields)
        {
            var methodName = ToPascalCase(field.Name);
            sb.AppendLine($"    public static {className} From{methodName}({field.TypeName} {field.Name}) =>");
            sb.AppendLine($"        new {className}(");
            for (int i = 0; i < fields.Count; i++)
            {
                var f = fields[i];
                var value = f.Name == field.Name ? f.Name : "null";
                sb.Append($"            {f.Name}: {value}");
                if (i < fields.Count - 1)
                    sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("        );");
            sb.AppendLine();
        }

        // Generate implicit operators
        foreach (var field in fields)
        {
            sb.AppendLine($"    public static implicit operator {className}({field.TypeName} {field.Name}) => From{ToPascalCase(field.Name)}({field.Name});");
        }
        sb.AppendLine();

        // Generate Map method
        sb.AppendLine("    public T Map<T>(");
        for (int i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            sb.Append($"        Func<{field.TypeName}, T> on{ToPascalCase(field.Name)}");
            if (i < fields.Count - 1)
                sb.Append(",");
            sb.AppendLine();
        }
        sb.AppendLine("    )");
        sb.AppendLine("    {");
        foreach (var field in fields)
        {
            sb.AppendLine($"        if (this.{field.Name} is not null)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            return on{ToPascalCase(field.Name)}(this.{field.Name});");
            sb.AppendLine($"        }}");
        }
        sb.AppendLine($"        throw new InvalidOperationException(\"{className} must contain one of: {string.Join(", ", fields.Select(f => f.TypeName))}\");");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate Match method
        sb.AppendLine("    public void Match(");
        for (int i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            sb.Append($"        Action<{field.TypeName}> on{ToPascalCase(field.Name)}");
            if (i < fields.Count - 1)
                sb.Append(",");
            sb.AppendLine();
        }
        sb.AppendLine("    )");
        sb.AppendLine("    {");
        foreach (var field in fields)
        {
            sb.AppendLine($"        if (this.{field.Name} is not null)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            on{ToPascalCase(field.Name)}(this.{field.Name});");
            sb.AppendLine($"            return;");
            sb.AppendLine($"        }}");
        }
        sb.AppendLine($"        throw new InvalidOperationException(\"{className} must contain one of: {string.Join(", ", fields.Select(f => f.TypeName))}\");");
        sb.AppendLine("    }");

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static bool HasExistingConstructor(ClassDeclarationSyntax classDeclaration, List<FieldInfo> fields)
    {
        var constructors = classDeclaration.Members.OfType<ConstructorDeclarationSyntax>();
        
        foreach (var constructor in constructors)
        {
            if (constructor.ParameterList.Parameters.Count == fields.Count)
            {
                var parameterTypes = constructor.ParameterList.Parameters
                    .Select(p => p.Type?.ToString())
                    .ToList();
                
                var expectedTypes = fields.Select(f => f.TypeName + "?").ToList();
                
                if (parameterTypes.SequenceEqual(expectedTypes))
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    private static string GetNamespace(SyntaxNode syntaxNode)
    {
        var namespaceDeclaration = syntaxNode.Ancestors()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .FirstOrDefault();

        return namespaceDeclaration?.Name?.ToString() ?? "";
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        
        return char.ToUpperInvariant(input[0]) + input.Substring(1);
    }

    private class FieldInfo
    {
        public string Name { get; }
        public string TypeName { get; }

        public FieldInfo(string name, string typeName)
        {
            Name = name;
            TypeName = typeName;
        }
    }
}

internal class OneOfSyntaxReceiver : ISyntaxReceiver
{
    public List<ClassDeclarationSyntax> CandidateClasses { get; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is ClassDeclarationSyntax classDeclaration &&
            classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword) &&
            classDeclaration.AttributeLists.Count > 0)
        {
            CandidateClasses.Add(classDeclaration);
        }
    }
}