using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExplicitThreadsChecker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExplicitThreadsCheckerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ETC001";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "ParallelCorrectness";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxNodeAction(AnalyzeThreadStart, SyntaxKind.IdentifierName);
        }

        private void AnalyzeThreadStart(SyntaxNodeAnalysisContext context)
        {
            var root = context.Node;

            if (!(root is IdentifierNameSyntax)) { return; }

            root = context.Node as IdentifierNameSyntax;
            
            var threadSymbol = context.SemanticModel.GetSymbolInfo(root).Symbol as INamedTypeSymbol;
            
            if (threadSymbol == null) { return; }
            if (threadSymbol.MetadataName != "Thread") { return; }

            var startNode = root.Parent.Parent.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Last();
            if (startNode == null) { return; }

            var startSymbol = context.SemanticModel.GetSymbolInfo(startNode).Symbol as IMethodSymbol;
            if (startSymbol == null) { return; }
            if (startSymbol.MetadataName != "Start") { return; }

            var argumentNode = root.Parent.DescendantNodesAndSelf().OfType<ArgumentSyntax>().First();
            
            var argument = argumentNode.Expression;
            var fullexpression = root.Parent.Parent.Parent.ToString();

            var diagn = Diagnostic.Create(Rule, root.Parent.Parent.Parent.GetLocation(), fullexpression, argument);
            
            context.ReportDiagnostic(diagn);

        }
    }
}
