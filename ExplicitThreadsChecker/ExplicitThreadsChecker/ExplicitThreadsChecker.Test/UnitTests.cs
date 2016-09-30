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
    public class UnitTest : CodeFixVerifier
    {

        //No diagnostics expected to show up
        [TestMethod]
        public void TestNoDiagnostics()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }


        [TestMethod]
        public void TestThreadNoDiagnostics()
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
            t.start();
            t.join();
        }
    }
}";
            VerifyCSharpDiagnostic(test);
        }


        [TestMethod]
        public void TestMultiDiagnostics()
        {
            var test = @"
using System.Threading;

namespace ExplicitThreadsSmell
{
    class SimpleThread
    {
        public void Test1()
        {
            while(true) {
                int i = 0;
                if(i == 10)             
                {
                    new Thread(Compute).Start();  
                }
                else 
                {
                    i++;
                    new Thread(Compute).Start();  
                }
            }
        }
    }
}";

            var expected1 = new DiagnosticResult
            {
                Id = "ETC001",
                Message = String.Format("'{0}' should be replaced with Task.Run", "new Thread(Compute).Start()"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 14, 21),
                        }
            };

            var expected2 = new DiagnosticResult
            {
                Id = "ETC001",
                Message = String.Format("'{0}' should be replaced with Task.Run", "new Thread(Compute).Start()"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 19, 21),
                        }
            };

            VerifyCSharpDiagnostic(test, expected1, expected2);
        }


        [TestMethod]
        public void TestThreadCodeSmellMethod()
        {
            var test = @"
using System.Threading;

namespace ExplicitThreadsSmell
{
    class SimpleThread
    {
        public void Test1()
        {
            new Thread(Compute).Start();  
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "ETC001",
                Message = String.Format("'{0}' should be replaced with Task.Run", "new Thread(Compute).Start()"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 10, 13)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
using System.Threading;
using System.Threading.Tasks;

namespace ExplicitThreadsSmell
{
    class SimpleThread
    {
        public void Test1()
        {
            Task.Run(() => Compute());  
        }
    }
}";
            VerifyCSharpFix(test, fixtest, allowNewCompilerDiagnostics: true);
        }

        [TestMethod]
        public void TestThreadCodeSmellLambda()
        {
            var test = @"
using System.Threading;

namespace ExplicitThreadsSmell
{
    class SimpleThread
    {
        public void Test1()
        {
            new Thread(() => { int i = 10; i++;}).Start();
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "ETC001",
                Message = String.Format("'{0}' should be replaced with Task.Run", "new Thread(() => { int i = 10; i++;}).Start()"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 10, 13)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
using System.Threading;
using System.Threading.Tasks;

namespace ExplicitThreadsSmell
{
    class SimpleThread
    {
        public void Test1()
        {
            Task.Run(() => { int i = 10; i++;});
        }
    }
}";
            VerifyCSharpFix(test, fixtest, allowNewCompilerDiagnostics:true);
        }


        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ExplicitThreadsCheckerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ExplicitThreadsCheckerAnalyzer();
        }



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
                Id = "ETC001",
                Message = String.Format("'{0}' should be replaced with Task.Run", "t"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 10, 20)
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
                Id = "ETC001",
                Message = String.Format("'{0}' should be replaced with Task.Run", "t"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 10, 20)
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
                Id = "ETC001",
                Message = String.Format("'{0}' should be replaced with Task.Run", "t"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 10, 20)
                        }
            };

            var expected2 = new DiagnosticResult
            {
                Id = "ETC001",
                Message = String.Format("'{0}' should be replaced with Task.Run", "j"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 10, 22)
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

    }




}