using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using ExplicitThreadsChecker;

namespace ExplicitThreadsChecker.Test
{
    [TestClass]
    public class UnitTestMultiline : CodeFixVerifier
    {
        
        

        [TestMethod]
        public void TestMultilineCodeSmell()
        {
            var test = @"
using System.Threading;

namespace ExplicitThreadsSmell
{
    class SimpleThread
    {
        public void Test1()
        {
            Thread t;
            t = new Thread(Compute);
            t.Start();
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "ETC002",
                Message = String.Format("'{0}' should be replaced with Task.Run", "t"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 12, 13)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

        }

        [TestMethod]
        public void TestMultilineCodeSmellDirectAssignment()
        {
            var test = @"
using System.Threading;

namespace ExplicitThreadsSmell
{
    class SimpleThread
    {
        public void Test1()
        {
            Thread t = new Thread(Compute);
            t.Start();
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "ETC002",
                Message = String.Format("'{0}' should be replaced with Task.Run", "t"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 11, 13)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

        }

        [TestMethod]
        public void TestMultilineNoSmell()
        {
            var test = @"
using System.Threading;

namespace ExplicitThreadsSmell
{
    class SimpleThread
    {
        public void Test1()
        {
            Thread t;
            t = new Thread(Compute);
            t.Start();
            t.Join();
        }
    }
}";

            VerifyCSharpDiagnostic(test);

        }

        [TestMethod]
        public void TestMultilineNoSmellMethod()
        {
            var test = @"
using System.Threading;

namespace ExplicitThreadsSmell
{
    class SimpleThread
    {
        public void Test1()
        {
            Thread t;
            t = new Thread(Compute);
            t.Start();
            Hallo(t);
        }
        private void Hallo(Thread thread)
        {
            throw new NotImplementedException();
        }
    }
}";

            VerifyCSharpDiagnostic(test);

        }



        [TestMethod]
        public void TestMultilineCodeSmellMultiDeclaration()
        {
            var test = @"
using System.Threading;

namespace ExplicitThreadsSmell
{
    class SimpleThread
    {
        public void Test1()
        {
            Thread t,j;
            t = new Thread(Compute);
            j = new Thread(Compute);
            t.Start();
            j.Start();
        }
    }
}";
            var expected1 = new DiagnosticResult
            {
                Id = "ETC002",
                Message = String.Format("'{0}' should be replaced with Task.Run", "t"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 13, 13)
                        }
            };

            var expected2 = new DiagnosticResult
            {
                Id = "ETC002",
                Message = String.Format("'{0}' should be replaced with Task.Run", "j"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 14, 13)
                        }
            };

            VerifyCSharpDiagnostic(test, expected1, expected2);

        }


        [TestMethod]
        public void TestMultilineCodeSmellMultiDeclarationNoSmell()
        {
            var test = @"
using System.Threading;

namespace ExplicitThreadsSmell
{
    class SimpleThread
    {
        public void Test1()
        {
            Thread t,j;
            t = new Thread(Compute);
            j = new Thread(Compute);
            t.Start();
            j.Start();
            j.Join();
            Compute(t);
        }
    }
}";
            VerifyCSharpDiagnostic(test);

        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            //return new ExplicitThreadsCheckerCodeFixProvider();
            return null;
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ExplicitThreadsMultilineCheckerAnalyzer();
        }



    }




}