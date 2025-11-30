namespace Sorry.SourceGens;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

internal class OneOfSyntaxReceiver : ISyntaxReceiver
{
    public List<ClassDeclarationSyntax> CandidateClasses { get; } = [];

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is ClassDeclarationSyntax classDeclaration &&
            classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword) &&
            classDeclaration.AttributeLists.Count > 0)
        {
            this.CandidateClasses.Add(classDeclaration);
        }
    }
}