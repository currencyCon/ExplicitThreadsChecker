﻿using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExplicitThreadsChecker
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExplicitThreadsCheckerCodeFixProvider)), Shared]
    public class ExplicitThreadsCheckerCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Use Task.Run";
        private const string TaskUsing = "System.Threading.Tasks";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(ExplicitThreadsCheckerAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();

            var node = root.FindNode(context.Span);

            if (node is InvocationExpressionSyntax == false)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(Title, c => ReplaceThreadWithTask(context.Document, node, c), Title),
                diagnostic);
        }

        private async Task<Document> ReplaceThreadWithTask(Document document, SyntaxNode node,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            
            var argument = node.DescendantNodes().OfType<ArgumentSyntax>().First();
            var invocationStatement = TaskSyntaxHelper.CreateInvocationStatement(argument);

            var newRoot = root.ReplaceNode(node, invocationStatement);

            newRoot = UsingHandler.AddUsingIfNotExists(newRoot, TaskUsing);
            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument;
        }

        private static bool IsThreadWithMethodArgument(ArgumentSyntax argument)
        {
            return argument.ChildNodes().OfType<IdentifierNameSyntax>().Any();
        }
    }
}