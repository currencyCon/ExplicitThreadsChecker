using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExplicitThreadsChecker
{
    internal class UsingHandler
    {
        internal SyntaxNode AddUsingIfNotExists(SyntaxNode root, String usingName)
        {
            var compilationUnit = (CompilationUnitSyntax)root;

            var exists = compilationUnit.Usings.Any(u => u.Name.ToString() == usingName);
            if (!exists)
            {
                var usingSystemThreadingTask = UsingDirectiveFactory.CreateUsingDirective(usingName);
                compilationUnit = compilationUnit.AddUsings(usingSystemThreadingTask);
            }

            return compilationUnit;
        }


    }
}
