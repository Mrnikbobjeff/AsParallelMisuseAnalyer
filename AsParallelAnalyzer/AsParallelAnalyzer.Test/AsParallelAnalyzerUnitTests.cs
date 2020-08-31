using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using AsParallelAnalyzer;
using System.Linq;

namespace AsParallelAnalyzer.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {

        //No diagnostics expected to show up
        [TestMethod]
        public void EmptyText_NoDiagnostic()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }
        [TestMethod]
        public void AsParallelToListInForeach_SingleDiagnostic()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
                public void Test() { foreach(var s in Enumerable.Range(0,1).Select(x => x*2).AsParallel().ToList());}
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "AsParallelAnalyzer",
                Message = String.Format("AsParallel Expression '{0}' has no effect", "Enumerable.Range(0,1).Select(x => x*2).AsParallel().ToList()"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 10, 55)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void AsParallelToArrayInForeach_SingleDiagnostic()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
                public void Test() { foreach(var s in Enumerable.Range(0,1).Select(x => x*2).AsParallel().ToArray());}
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "AsParallelAnalyzer",
                Message = String.Format("AsParallel Expression '{0}' has no effect", "Enumerable.Range(0,1).Select(x => x*2).AsParallel().ToArray()"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 10, 55)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }
        [TestMethod]
        public void AsParallelToListInForeach_SingleFix()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
                public void Test() { foreach(var s in Enumerable.Range(0,1).Select(x => x*2).AsParallel().ToList());}
        }
    }";
            

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
                public void Test() { foreach(var s in Enumerable.Range(0,1).Select(x => x*2));}
        }
    }";
            VerifyCSharpFix(test, fixtest, allowNewCompilerDiagnostics:true);
        }

        [TestMethod]
        public void AsParallelAtEnd_SingleDiagnostic()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
                public void Test() {foreach(var s in Enumerable.Range(0,1).Select(x => x*2).AsParallel());}
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "AsParallelAnalyzer",
                Message = String.Format("AsParallel Expression '{0}' has no effect", "Enumerable.Range(0,1).Select(x => x*2).AsParallel()"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 10, 54)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;

    namespace ConsoleApplication1
    {
        class TypeName
        {   
                public void Test() {foreach(var s in Enumerable.Range(0,1).Select(x => x*2));}
        }
    }";
            VerifyCSharpFix(test, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new AsParallelAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new AsParallelAnalyzerAnalyzer();
        }
    }
}
