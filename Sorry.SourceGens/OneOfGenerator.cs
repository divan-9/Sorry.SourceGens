namespace Sorry.SourceGens;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[Generator]
public class OneOfGenerator : ISourceGenerator
{
    public void Initialize(
        GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new OneOfSyntaxReceiver());
    }

    public void Execute(
        GeneratorExecutionContext context)
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

    private static List<FieldInfo> GetNullableFields(
        ClassDeclarationSyntax classDeclaration)
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

    private static string GenerateOneOfImplementation(
        ClassDeclarationSyntax classDeclaration, List<FieldInfo> fields)
    {
        var className = classDeclaration.Identifier.ValueText;
        var namespaceName = GetNamespace(classDeclaration);
        var hasDuplicateTypes = HasDuplicateTypes(fields);

        var result = new StringBuilder();
        result.AppendLine("#pragma warning disable");
        result.AppendLine("#nullable enable");
        result.AppendLine("using System;");

        if (!string.IsNullOrEmpty(namespaceName))
        {
            result.AppendLine();
            result.AppendLine($"namespace {namespaceName};");
        }

        result.AppendLine();
        result.AppendLine($"partial class {className} : IEquatable<{className}>");
        result.AppendLine("{");

        // Generate constructor
        result.AppendLine($"    private {className}(");
        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            result.Append($"        {field.TypeName}? {field.Name}");
            if (i < fields.Count - 1)
                result.Append(",");
            result.AppendLine();
        }
        result.AppendLine("    )");
        result.AppendLine("    {");
        foreach (var field in fields)
        {
            result.AppendLine($"        this.{field.Name} = {field.Name};");
        }
        result.AppendLine("    }");
        result.AppendLine();

        // Generate factory methods
        foreach (var field in fields)
        {
            var methodName = ToPascalCase(field.Name);
            result.AppendLine($"    public static {className} From{methodName}({field.TypeName} {field.Name}) =>");
            result.AppendLine($"        new {className}(");
            for (int i = 0; i < fields.Count; i++)
            {
                var f = fields[i];
                var value = f.Name == field.Name ? f.Name : "null";
                result.Append($"            {f.Name}: {value}");
                if (i < fields.Count - 1)
                    result.Append(",");
                result.AppendLine();
            }
            result.AppendLine("        );");
            result.AppendLine();
        }

        // Generate implicit operators only if there are no duplicate types
        if (!hasDuplicateTypes)
        {
            foreach (var field in fields)
            {
                result.AppendLine($"    public static implicit operator {className}({field.TypeName} {field.Name}) => From{ToPascalCase(field.Name)}({field.Name});");
            }
            result.AppendLine();
        }

        // Generate original Map method
        result.AppendLine("    public T Map<T>(");
        for (int i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            result.Append($"        Func<{field.TypeName}, T> on{ToPascalCase(field.Name)}");
            if (i < fields.Count - 1)
                result.Append(",");
            result.AppendLine();
        }
        result.AppendLine("    )");
        result.AppendLine("    {");
        foreach (var field in fields)
        {
            result.AppendLine($"        if (this.{field.Name} is not null)");
            result.AppendLine($"        {{");
            result.AppendLine($"            return on{ToPascalCase(field.Name)}(this.{field.Name});");
            result.AppendLine($"        }}");
        }
        result.AppendLine($"        throw new InvalidOperationException(\"{className} must contain one of: {string.Join(", ", fields.Select(f => f.TypeName))}\");");
        result.AppendLine("    }");
        result.AppendLine();

        // Generate Map method with default case
        result.AppendLine("    public T Map<T>(");
        result.Append("        Func<T> onDefault");
        if (fields.Count > 0)
            result.Append(",");
        result.AppendLine();
        for (int i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            result.Append($"        Func<{field.TypeName}, T>? on{ToPascalCase(field.Name)} = null");
            if (i < fields.Count - 1)
                result.Append(",");
            result.AppendLine();
        }
        result.AppendLine("    )");
        result.AppendLine("    {");
        foreach (var field in fields)
        {
            result.AppendLine($"        if (this.{field.Name} is not null && on{ToPascalCase(field.Name)} is not null)");
            result.AppendLine($"        {{");
            result.AppendLine($"            return on{ToPascalCase(field.Name)}(this.{field.Name});");
            result.AppendLine($"        }}");
        }
        result.AppendLine("        return onDefault();");
        result.AppendLine("    }");
        result.AppendLine();

        // Generate original Match method
        result.AppendLine("    public void Match(");
        for (int i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            result.Append($"        Action<{field.TypeName}> on{ToPascalCase(field.Name)}");
            if (i < fields.Count - 1)
                result.Append(",");
            result.AppendLine();
        }
        result.AppendLine("    )");
        result.AppendLine("    {");
        foreach (var field in fields)
        {
            result.AppendLine($"        if (this.{field.Name} is not null)");
            result.AppendLine($"        {{");
            result.AppendLine($"            on{ToPascalCase(field.Name)}(this.{field.Name});");
            result.AppendLine($"            return;");
            result.AppendLine($"        }}");
        }
        result.AppendLine($"        throw new InvalidOperationException(\"{className} must contain one of: {string.Join(", ", fields.Select(f => f.TypeName))}\");");
        result.AppendLine("    }");
        result.AppendLine();

        // Generate Match method with default case
        result.AppendLine("    public void Match(");
        result.Append("        Action onDefault");
        if (fields.Count > 0)
            result.Append(",");
        result.AppendLine();
        for (int i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            result.Append($"        Action<{field.TypeName}>? on{ToPascalCase(field.Name)} = null");
            if (i < fields.Count - 1)
                result.Append(",");
            result.AppendLine();
        }
        result.AppendLine("    )");
        result.AppendLine("    {");
        foreach (var field in fields)
        {
            result.AppendLine($"        if (this.{field.Name} is not null && on{ToPascalCase(field.Name)} is not null)");
            result.AppendLine($"        {{");
            result.AppendLine($"            on{ToPascalCase(field.Name)}(this.{field.Name});");
            result.AppendLine($"            return;");
            result.AppendLine($"        }}");
        }
        result.AppendLine("        onDefault();");
        result.AppendLine("    }");
        result.AppendLine();

        // Generate Equals method
        result.AppendLine("    public override bool Equals(object? obj)");
        result.AppendLine("    {");
        result.AppendLine($"        if (obj is not {className} other) return false;");
        result.AppendLine("        return Equals(other);");
        result.AppendLine("    }");
        result.AppendLine();

        // Generate typed Equals method
        result.AppendLine($"    public bool Equals({className}? other)");
        result.AppendLine("    {");
        result.AppendLine("        if (other is null) return false;");
        result.AppendLine("        if (ReferenceEquals(this, other)) return true;");
        result.AppendLine();
        
        // Generate equality check for each possible state
        result.Append("        return ");
        for (int i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            result.Append("(");
            
            // First condition: this field is set and equals other's field
            result.Append($"this.{field.Name} is not null && other.{field.Name} is not null && this.{field.Name}.Equals(other.{field.Name})");
            
            // Additional conditions: all other fields are null in both objects
            for (int j = 0; j < fields.Count; j++)
            {
                if (j != i)
                {
                    var f = fields[j];
                    result.Append($" && this.{f.Name} is null && other.{f.Name} is null");
                }
            }
            
            result.Append(")");
            
            if (i < fields.Count - 1)
            {
                result.AppendLine(" ||");
                result.Append("               ");
            }
            else
            {
                result.AppendLine(";");
            }
        }
        result.AppendLine("    }");
        result.AppendLine();

        // Generate GetHashCode method
        result.AppendLine("    public override int GetHashCode()");
        result.AppendLine("    {");
        result.AppendLine("        return HashCode.Combine(");
        for (int i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            result.Append($"            this.{field.Name}?.GetHashCode() ?? 0");
            if (i < fields.Count - 1)
                result.Append(",");
            result.AppendLine();
        }
        result.AppendLine("        );");
        result.AppendLine("    }");
        result.AppendLine();

        // Generate equality operators
        result.AppendLine($"    public static bool operator ==({className}? left, {className}? right)");
        result.AppendLine("    {");
        result.AppendLine("        return ReferenceEquals(left, right) || (left?.Equals(right) ?? false);");
        result.AppendLine("    }");
        result.AppendLine();

        result.AppendLine($"    public static bool operator !=({className}? left, {className}? right)");
        result.AppendLine("    {");
        result.AppendLine("        return !(left == right);");
        result.AppendLine("    }");

        result.AppendLine("}");

        return result.ToString();
    }

    private static bool HasDuplicateTypes(List<FieldInfo> fields)
    {
        var typeGroups = fields.GroupBy(f => f.TypeName);
        return typeGroups.Any(g => g.Count() > 1);
    }



    private static string GetNamespace(
        SyntaxNode syntaxNode)
    {
        var namespaceDeclaration = syntaxNode.Ancestors()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .FirstOrDefault();

        return namespaceDeclaration?.Name?.ToString() ?? "";
    }

    private static string ToPascalCase(
        string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return char.ToUpperInvariant(input[0]) + input.Substring(1);
    }

    private class FieldInfo
    {
        public FieldInfo(string name, string typeName)
        {
            this.Name = name;
            this.TypeName = typeName;
        }

        public string Name { get; }
        public string TypeName { get; }
    }
}