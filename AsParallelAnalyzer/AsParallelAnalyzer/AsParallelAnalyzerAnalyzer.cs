using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AsParallelAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AsParallelAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AsParallelAnalyzer";
        static readonly string[] knownLastCalls = new string[] { "ToList", "ToArray" };

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            var invocation = context.Node as InvocationExpressionSyntax;

            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                if (!memberAccess.Name.Identifier.ValueText.Equals("AsParallel"))
                {
                    if(!(knownLastCalls.Contains(memberAccess.Name.Identifier.ValueText)
                && memberAccess.Expression is InvocationExpressionSyntax nestedInvocation
                && nestedInvocation.Expression is MemberAccessExpressionSyntax possibleAsParallelCall
                && possibleAsParallelCall.Name.Identifier.ValueText.Equals("AsParallel")))
                    {
                        return;
                    }
                }//true when it is the last statement
            }
            var parentForEach = (invocation.Parent as ForEachStatementSyntax);
            if (parentForEach is null)
                return;
            // Find just those named type symbols with names containing lowercase letters.
            if (parentForEach.Expression.IsEquivalentTo(invocation) || //Last call is AsParallel
                parentForEach.Expression is MemberAccessExpressionSyntax mem && knownLastCalls.Contains(mem.Name.Identifier.ValueText)
                )
            {
                // For all such symbols, produce a diagnostic.
                var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation(), invocation);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
