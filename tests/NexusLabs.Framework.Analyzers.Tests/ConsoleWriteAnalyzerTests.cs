using System.Threading.Tasks;

using NexusLabs.Framework.Analyzers;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class ConsoleWriteAnalyzerTests
{
    [Fact]
    public async Task ConsoleWriteLine_Reports()
    {
        var source = """
            using System;
            namespace App
            {
                public class C
                {
                    public void M() => {|#0:Console.WriteLine("hello")|};
                }
            }
            """;

        var expected = AnalyzerVerifier<ConsoleWriteAnalyzer>
            .Diagnostic(DiagnosticDescriptors.DoNotUseConsoleWrite)
            .WithLocation(0)
            .WithArguments("System.Console.WriteLine");

        await AnalyzerVerifier<ConsoleWriteAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task ConsoleWrite_Reports()
    {
        var source = """
            using System;
            namespace App
            {
                public class C
                {
                    public void M() => {|#0:Console.Write("x")|};
                }
            }
            """;

        var expected = AnalyzerVerifier<ConsoleWriteAnalyzer>
            .Diagnostic(DiagnosticDescriptors.DoNotUseConsoleWrite)
            .WithLocation(0)
            .WithArguments("System.Console.Write");

        await AnalyzerVerifier<ConsoleWriteAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task DebugWriteLine_Reports()
    {
        var source = """
            using System.Diagnostics;
            namespace App
            {
                public class C
                {
                    public void M() => {|#0:Debug.WriteLine("x")|};
                }
            }
            """;

        var expected = AnalyzerVerifier<ConsoleWriteAnalyzer>
            .Diagnostic(DiagnosticDescriptors.DoNotUseConsoleWrite)
            .WithLocation(0)
            .WithArguments("System.Diagnostics.Debug.WriteLine");

        await AnalyzerVerifier<ConsoleWriteAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task DebugWrite_Reports()
    {
        var source = """
            using System.Diagnostics;
            namespace App
            {
                public class C
                {
                    public void M() => {|#0:Debug.Write("x")|};
                }
            }
            """;

        var expected = AnalyzerVerifier<ConsoleWriteAnalyzer>
            .Diagnostic(DiagnosticDescriptors.DoNotUseConsoleWrite)
            .WithLocation(0)
            .WithArguments("System.Diagnostics.Debug.Write");

        await AnalyzerVerifier<ConsoleWriteAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task UsingStaticConsole_StillReports()
    {
        var source = """
            using static System.Console;
            namespace App
            {
                public class C
                {
                    public void M() => {|#0:WriteLine("x")|};
                }
            }
            """;

        var expected = AnalyzerVerifier<ConsoleWriteAnalyzer>
            .Diagnostic(DiagnosticDescriptors.DoNotUseConsoleWrite)
            .WithLocation(0)
            .WithArguments("System.Console.WriteLine");

        await AnalyzerVerifier<ConsoleWriteAnalyzer>.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task UnrelatedMethodWithSameName_DoesNotReport()
    {
        var source = """
            namespace App
            {
                public static class Logger
                {
                    public static void WriteLine(string s) { }
                    public static void Write(string s) { }
                }

                public class C
                {
                    public void M()
                    {
                        Logger.WriteLine("x");
                        Logger.Write("x");
                    }
                }
            }
            """;

        await AnalyzerVerifier<ConsoleWriteAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task EmptyClass_DoesNotReport()
    {
        var source = """
            namespace App
            {
                public class C { }
            }
            """;

        await AnalyzerVerifier<ConsoleWriteAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task MultipleViolations_AllReported()
    {
        var source = """
            using System;
            using System.Diagnostics;
            namespace App
            {
                public class C
                {
                    public void M()
                    {
                        {|#0:Console.WriteLine("a")|};
                        {|#1:Debug.WriteLine("b")|};
                    }
                }
            }
            """;

        var expectedA = AnalyzerVerifier<ConsoleWriteAnalyzer>
            .Diagnostic(DiagnosticDescriptors.DoNotUseConsoleWrite)
            .WithLocation(0)
            .WithArguments("System.Console.WriteLine");

        var expectedB = AnalyzerVerifier<ConsoleWriteAnalyzer>
            .Diagnostic(DiagnosticDescriptors.DoNotUseConsoleWrite)
            .WithLocation(1)
            .WithArguments("System.Diagnostics.Debug.WriteLine");

        await AnalyzerVerifier<ConsoleWriteAnalyzer>.VerifyAnalyzerAsync(source, expectedA, expectedB);
    }
}
