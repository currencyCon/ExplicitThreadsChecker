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
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();

            var node = root.FindNode(context.Span);

            if (node is InvocationExpressionSyntax == false) { return; }

            context.RegisterCodeFix(CodeAction.Create(
                    title: title, 
                    createChangedDocument: c => ReplaceThreadWithTask(context.Document, node, c), 
                    equivalenceKey: title), 
                diagnostic);
        }

        private async Task<Document> ReplaceThreadWithTask(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            
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

            UsingHandler handler = new UsingHandler();
            newRoot = handler.AddUsingIfNotExists(newRoot, "System.Threading.Tasks");
            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument;
        }

        private InvocationExpressionSyntax ReplaceLambda(ParenthesizedLambdaExpressionSyntax lambda)
        {
            var taskRunSyntax = CreateTaskRun();

            var argument = SyntaxFactory.Argument(lambda);
            var argumentList = SyntaxFactory.SeparatedList(new[] { argument });
            var invocationStatement = SyntaxFactory.InvocationExpression(taskRunSyntax, SyntaxFactory.ArgumentList(argumentList));
            return invocationStatement;
        }

        private static InvocationExpressionSyntax ReplaceMethod(string methodName)
        {
            var taskRunSyntax = CreateTaskRun();
            var emptyParameterList = SyntaxFactory.ParameterList();

            var compute = SyntaxFactory.IdentifierName(methodName);
            var lambdaBody = SyntaxFactory.InvocationExpression(compute);

            var parenthesizedLambdaExpression = SyntaxFactory.ParenthesizedLambdaExpression(emptyParameterList, lambdaBody);
            var argument = SyntaxFactory.Argument(parenthesizedLambdaExpression);
            var argumentList = SyntaxFactory.SeparatedList(new[] {argument});
            var invocationStatement = SyntaxFactory.InvocationExpression(taskRunSyntax, SyntaxFactory.ArgumentList(argumentList));
            return invocationStatement;
        }

        private static MemberAccessExpressionSyntax CreateTaskRun()
        {
            var task = SyntaxFactory.IdentifierName("Task");
            var run = SyntaxFactory.IdentifierName("Run");
            var taskRunSyntax = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, task, run);
            return taskRunSyntax;
        }
        
    }
}