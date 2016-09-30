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

            FindSingleLineCodeSmell(context, root);
            FindMultilineCodeSmellDirectAllocation(context, root);
            FindMultilineCodeSmellDeclartion(context, root);
        }


        private void FindMultilineCodeSmellDeclartion(SyntaxNodeAnalysisContext context, SyntaxNode root)
        {
            if (!root.Ancestors().OfType<VariableDeclarationSyntax>().Any())
            {
                return;
            }

            var variableDeclartionNode = root.Ancestors().OfType<VariableDeclarationSyntax>().First();
            if (variableDeclartionNode == null)
            {
                return;
            }

            var variables = variableDeclartionNode.Variables;

            foreach (var variableDeclaratorSyntax in variables)
            {
                CheckThreadUsage(context, root, variableDeclaratorSyntax);
            }
            

        }

        private void CheckThreadUsage(SyntaxNodeAnalysisContext context, SyntaxNode root, VariableDeclaratorSyntax identifier)
        {
            var block = root.Ancestors().OfType<BlockSyntax>().First();
            var references = block.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Where(i => i.Identifier.ToString() == identifier.Identifier.ToString());

            bool logWarning = true;
            foreach (var identifierNameSyntax in references)
            {
                if (identifierNameSyntax.Parent is LocalDeclarationStatementSyntax ||
                    identifierNameSyntax.Parent is AssignmentExpressionSyntax)
                {

                }
                else if (identifierNameSyntax.Parent is MemberAccessExpressionSyntax)
                {
                    var method = identifierNameSyntax.Parent as MemberAccessExpressionSyntax;
                    var methodName = method.Name.ToString();
                    if (methodName != "Start")
                    {
                        logWarning = false;
                    }
                }
                else
                {
                    logWarning = false;
                }
            }


            if (logWarning)
            {
                var fullexpression = root.Parent.Parent.Parent.ToString();
                var diagn = Diagnostic.Create(Rule, identifier.GetLocation(), identifier.Identifier.ToString(), "");
                context.ReportDiagnostic(diagn);
            }
        }


        private bool FindMultilineCodeSmellDirectAllocation(SyntaxNodeAnalysisContext context, SyntaxNode root)
        {
            if (!root.Ancestors().OfType<VariableDeclaratorSyntax>().Any())
            {
                return false;
            }

            var variableDeclartionNode = root.Ancestors().OfType<VariableDeclaratorSyntax>().First();
            if (variableDeclartionNode == null)
            {
                return false;
            }

            var variableName = variableDeclartionNode.Identifier;
            var block = root.Ancestors().OfType<BlockSyntax>().First();
            var references = block.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Where(i => i.Identifier.ToString() == variableName.ToString());

            bool logWarning = true;
            foreach (var identifierNameSyntax in references)
            {
                if (identifierNameSyntax.Parent is LocalDeclarationStatementSyntax ||
                    identifierNameSyntax.Parent is AssignmentExpressionSyntax)
                {

                }
                else if (identifierNameSyntax.Parent is MemberAccessExpressionSyntax)
                {
                    var method = identifierNameSyntax.Parent as MemberAccessExpressionSyntax;
                    var methodName = method.Name.ToString();
                    if (methodName != "Start")
                    {
                        logWarning = false;
                    }
                }
                else
                {
                    logWarning = false;
                }
            }


            if (logWarning)
            {
                var fullexpression = root.Parent.Parent.Parent.ToString();
                var diagn = Diagnostic.Create(Rule, root.Parent.Parent.Parent.GetLocation(), variableName, "");
                context.ReportDiagnostic(diagn);
            }

            return logWarning;

        }

        private static void FindSingleLineCodeSmell(SyntaxNodeAnalysisContext context, SyntaxNode root)
        {
            var startNode = root.Parent.Parent.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Last();
            if (startNode == null)
            {
                return;
            }

            var startSymbol = context.SemanticModel.GetSymbolInfo(startNode).Symbol as IMethodSymbol;
            if (startSymbol == null)
            {
                return;
            }
            if (startSymbol.MetadataName != "Start")
            {
                return;
            }

            var argumentNode = root.Parent.DescendantNodesAndSelf().OfType<ArgumentSyntax>().First();

            var argument = argumentNode.Expression;
            var fullexpression = root.Parent.Parent.Parent.ToString();

            var diagn = Diagnostic.Create(Rule, root.Parent.Parent.Parent.GetLocation(), fullexpression, argument);

            context.ReportDiagnostic(diagn);
        }
    }
}
