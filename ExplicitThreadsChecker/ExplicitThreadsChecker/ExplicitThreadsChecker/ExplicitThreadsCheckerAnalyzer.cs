using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FindSymbols;

namespace ExplicitThreadsChecker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExplicitThreadsCheckerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ETC001";
        public const string ThreadStartDefintion = "System.Threading.Thread.Start()";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormatSingleLine), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "ParallelCorrectness";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeThreadStart, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeThreadStart(SyntaxNodeAnalysisContext context)
        {
            var root = context.Node;

            if (!(root is InvocationExpressionSyntax)) { return; }
            if (!root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Any())
            {
                //Ignore ETC002
                return;
            }
            if (root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().First().Type.ToString() != "Thread")
            {
                //Ignore ETC002
                return;
            }

            var invocationExpression = root as InvocationExpressionSyntax;
            
            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocationExpression).Symbol as IMethodSymbol;
            
            if (methodSymbol == null) { return; }
            if (methodSymbol.OriginalDefinition.ToString() != ThreadStartDefintion) { return; }
            
            var diagn = Diagnostic.Create(Rule, invocationExpression.GetLocation());
            context.ReportDiagnostic(diagn);
        }

    }
}
