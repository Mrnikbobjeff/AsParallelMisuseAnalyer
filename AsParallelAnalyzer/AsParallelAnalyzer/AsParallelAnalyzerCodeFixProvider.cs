using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace AsParallelAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AsParallelAnalyzerCodeFixProvider)), Shared]
    public class AsParallelAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string title = "Remove AsParallel";
        static readonly string[] removableEnds = new string[] { "ToList", "ToArray", "AsParallel" };
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(AsParallelAnalyzerAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().Last();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedSolution: c => RemoveAsParallelCall(context.Document, declaration, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private async Task<Solution> RemoveAsParallelCall(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
        {
            // Produce a new solution that has all references to that type renamed, including the declaration.
            var originalSolution = document.Project.Solution;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            SyntaxNode newRoot = root;
            ExpressionSyntax possibleInvocation = invocationExpression;
            do
            {
                var newExpression = ((possibleInvocation as InvocationExpressionSyntax).Expression as MemberAccessExpressionSyntax).Expression;
                possibleInvocation = newExpression;
            } while (possibleInvocation is InvocationExpressionSyntax nestedInvocation && nestedInvocation.Expression is MemberAccessExpressionSyntax member && removableEnds.Contains(member.Name.Identifier.ValueText));
            // Return the new solution with the now-uppercase type name.
            return originalSolution.WithDocumentSyntaxRoot(document.Id, root.ReplaceNode(invocationExpression, possibleInvocation));
        }
    }
}
