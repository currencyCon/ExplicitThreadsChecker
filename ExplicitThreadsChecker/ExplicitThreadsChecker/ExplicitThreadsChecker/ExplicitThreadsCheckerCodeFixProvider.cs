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

namespace ExplicitThreadsChecker
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExplicitThreadsCheckerCodeFixProvider)), Shared]
    public class ExplicitThreadsCheckerCodeFixProvider : CodeFixProvider
    {
        private const string title = "Use Task.Run";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(ExplicitThreadsCheckerAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // Get the root syntax node for the current document
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // Get a reference to the diagnostic to fix
            var diagnostic = context.Diagnostics.First();

            // Get the location in the code editor for the diagnostic
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the syntax node on the span where there is a squiggle 
            var node = root.FindNode(context.Span);

            // If the syntax node is not an IdentifierName return
            if (node is InvocationExpressionSyntax == false) { return; }

            // Register a code action that invokes the fix on the current document 
            context.RegisterCodeFix(CodeAction.Create(title: title, createChangedDocument: c => ReplaceThreadWithTask(context.Document, node, c), equivalenceKey: title), diagnostic);
        }

        private async Task<Document> ReplaceThreadWithTask(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync();
            root = AddUsingSystemThreadingTasks(root);

            var argument = node.DescendantNodes().OfType<ArgumentSyntax>().First();
            InvocationExpressionSyntax invocationStatement;
            if (argument.ChildNodes().OfType<IdentifierNameSyntax>().Any())
            {
                var methodName = argument.ChildNodes().OfType<IdentifierNameSyntax>().First().Identifier.ToString();
                invocationStatement = ReplaceMethod(methodName);
            }
            else
            {
                var lambda = argument.ChildNodes().OfType<ParenthesizedLambdaExpressionSyntax>().First();
                invocationStatement = ReplaceLambda(lambda);
            }
            
            
            var newRoot = root.ReplaceNode(node, invocationStatement);
            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument;
        }

        private InvocationExpressionSyntax ReplaceLambda(ParenthesizedLambdaExpressionSyntax lambda)
        {
            var task = SyntaxFactory.IdentifierName("Task");
            var run = SyntaxFactory.IdentifierName("Run");
            var taskRunSyntax = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, task, run);
            
            var argument = SyntaxFactory.Argument(lambda);
            var argumentList = SyntaxFactory.SeparatedList(new[] { argument });
            var invocationStatement = SyntaxFactory.InvocationExpression(taskRunSyntax, SyntaxFactory.ArgumentList(argumentList));
            return invocationStatement;
        }

        private static InvocationExpressionSyntax ReplaceMethod(string methodName)
        {
            var task = SyntaxFactory.IdentifierName("Task");
            var run = SyntaxFactory.IdentifierName("Run");
            var taskRunSyntax = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, task, run);
            var emptyParameterList = SyntaxFactory.ParameterList();

            var compute = SyntaxFactory.IdentifierName(methodName);
            var lambdaBody = SyntaxFactory.InvocationExpression(compute);

            var parenthesizedLambdaExpression = SyntaxFactory.ParenthesizedLambdaExpression(emptyParameterList, lambdaBody);
            var argument = SyntaxFactory.Argument(parenthesizedLambdaExpression);
            var argumentList = SyntaxFactory.SeparatedList(new[] {argument});
            var invocationStatement = SyntaxFactory.InvocationExpression(taskRunSyntax, SyntaxFactory.ArgumentList(argumentList));
            return invocationStatement;
        }

        private SyntaxNode AddUsingSystemThreadingTasks(SyntaxNode root)
        {
            var compilationUnit = (CompilationUnitSyntax)root;
            
            var exists = compilationUnit.Usings.Any(u => u.Name.ToString() == "System.Threading.Tasks");
            if (!exists)
            {
                var usingSystemThreadingTask = CreateUsingDirective("System.Threading.Tasks");
                compilationUnit = compilationUnit.AddUsings(usingSystemThreadingTask);
            }

            return compilationUnit;
        }


        private UsingDirectiveSyntax CreateUsingDirective(string usingName)
        {
            NameSyntax qualifiedName = null;

            foreach (var identifier in usingName.Split('.'))
            {
                var name = SyntaxFactory.IdentifierName(identifier);

                if (qualifiedName != null)
                {
                    qualifiedName = SyntaxFactory.QualifiedName(qualifiedName, name);
                }
                else
                {
                    qualifiedName = name;
                }
            }
            return SyntaxFactory.UsingDirective(qualifiedName);
        }

    }
}