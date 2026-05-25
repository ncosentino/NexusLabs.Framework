using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

using Xunit;

namespace NexusLabs.Framework.Analyzers.Tests;

public sealed class TryResultErrorUsageAnalyzerTests
{
    [Fact]
    public async Task ErrorNullCheck_AfterEarlyReturnOnSuccess_ReportsDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void ProcessResult()
                    {
                        var result = GetResult();
                        if (result.Success)
                        {
                            return;
                        }
                        if ({|#0:result.Error|} != null)
                        {
                            Console.WriteLine(result.Error.Message);
                        }
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0004", DiagnosticSeverity.Warning).WithLocation(0);
        await VerifyAsync(source, expected);
    }

    [Fact]
    public async Task ErrorNullCheck_WithIsNotNull_AfterEarlyReturnOnSuccess_ReportsDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void ProcessResult()
                    {
                        var result = GetResult();
                        if (result.Success)
                        {
                            return;
                        }
                        if ({|#0:result.Error|} is not null)
                        {
                            Console.WriteLine(result.Error.Message);
                        }
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0004", DiagnosticSeverity.Warning).WithLocation(0);
        await VerifyAsync(source, expected);
    }

    [Fact]
    public async Task ErrorNullCheck_WithIsNull_AfterEarlyReturnOnSuccess_ReportsDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void ProcessResult()
                    {
                        var result = GetResult();
                        if (result.Success)
                        {
                            return;
                        }
                        if ({|#0:result.Error|} is null)
                        {
                            Console.WriteLine("Error is null");
                        }
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0004", DiagnosticSeverity.Warning).WithLocation(0);
        await VerifyAsync(source, expected);
    }

    [Fact]
    public async Task ErrorNullCheck_InFalseBranchOfSuccess_ReportsDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void ProcessResult()
                    {
                        var result = GetResult();
                        if (result.Success)
                        {
                            Console.WriteLine("Success");
                        }
                        else
                        {
                            if ({|#0:result.Error|} == null)
                            {
                                Console.WriteLine("Should never happen");
                            }
                        }
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0004", DiagnosticSeverity.Warning).WithLocation(0);
        await VerifyAsync(source, expected);
    }

    [Fact]
    public async Task ErrorNullCheck_InTrueBranchOfNotSuccess_ReportsDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void ProcessResult()
                    {
                        var result = GetResult();
                        if (!result.Success)
                        {
                            if ({|#0:result.Error|} != null)
                            {
                                Console.WriteLine(result.Error.Message);
                            }
                        }
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0004", DiagnosticSeverity.Warning).WithLocation(0);
        await VerifyAsync(source, expected);
    }

    [Fact]
    public async Task ErrorNullCheck_WithoutSuccessCheck_NoDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void ProcessResult()
                    {
                        var result = GetResult();
                        if (result.Error != null)
                        {
                            Console.WriteLine(result.Error.Message);
                        }
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ErrorNullCheck_OnTriedNullEx_AfterEarlyReturnOnSuccess_ReportsDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void ProcessResult()
                    {
                        var result = GetResult();
                        if (result.Success)
                        {
                            return;
                        }
                        if ({|#0:result.Error|} != null)
                        {
                            Console.WriteLine(result.Error.Message);
                        }
                    }
                    private TriedNullEx<string?> GetResult() => default;
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0004", DiagnosticSeverity.Warning).WithLocation(0);
        await VerifyAsync(source, expected);
    }

    [Fact]
    public async Task ReturningNewException_AfterEarlyReturnOnSuccess_WithoutError_ReportsDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public Exception ProcessResult()
                    {
                        var result = GetResult();
                        if (result.Success)
                        {
                            return new Exception("Success");
                        }
                        {|#0:return new Exception("Something failed");|}
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0005", DiagnosticSeverity.Warning).WithLocation(0);
        await VerifyAsync(source, expected);
    }

    [Fact]
    public async Task ReturningNewException_InFalseBranchOfSuccess_WithoutError_ReportsDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public Exception? ProcessResult()
                    {
                        var result = GetResult();
                        if (result.Success)
                        {
                            return null;
                        }
                        else
                        {
                            {|#0:return new InvalidOperationException("Operation failed");|}
                        }
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        var expected = new DiagnosticResult("NLF0005", DiagnosticSeverity.Warning).WithLocation(0);
        await VerifyAsync(source, expected);
    }

    [Fact]
    public async Task ReturningError_AfterEarlyReturnOnSuccess_NoDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public Exception ProcessResult()
                    {
                        var result = GetResult();
                        if (result.Success)
                        {
                            return new Exception("Success");
                        }
                        return result.Error;
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ReturningNewExceptionWithErrorAsInner_AfterEarlyReturnOnSuccess_NoDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public Exception ProcessResult()
                    {
                        var result = GetResult();
                        if (result.Success)
                        {
                            return new Exception("Success");
                        }
                        return new InvalidOperationException("Wrapper message", result.Error);
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ReturningNewException_InTrueBranchOfSuccess_NoDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public Exception ProcessResult()
                    {
                        var result = GetResult();
                        if (result.Success)
                        {
                            return new Exception("Success case");
                        }
                        return result.Error;
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ReturningNewException_WithoutSuccessCheck_NoDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public Exception ProcessResult()
                    {
                        var result = GetResult();
                        return new Exception("Some exception");
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ReturningError_InTrueBranchOfNotSuccess_NoDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public Exception? ProcessResult()
                    {
                        var result = GetResult();
                        if (!result.Success)
                        {
                            return result.Error;
                        }
                        return null;
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ErrorAccess_DirectWithoutNullCheck_AfterEarlyReturnOnSuccess_NoDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public void ProcessResult()
                    {
                        var result = GetResult();
                        if (result.Success)
                        {
                            return;
                        }
                        Console.WriteLine(result.Error.Message);
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ReturningCustomExceptionWithErrorAsInner_NoDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class CustomException : Exception
                {
                    public CustomException(string message, Exception inner) : base(message, inner) { }
                }
                public class TestClass
                {
                    public Exception ProcessResult()
                    {
                        var result = GetResult();
                        if (result.Success)
                        {
                            return new Exception("Success");
                        }
                        return new CustomException("Custom wrapper", result.Error);
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task NonExceptionReturn_AfterEarlyReturnOnSuccess_NoDiagnostic()
    {
        var source = """
            using System;
            using NexusLabs.Framework;
            namespace Test
            {
                public class TestClass
                {
                    public string ProcessResult()
                    {
                        var result = GetResult();
                        if (result.Success)
                        {
                            return result.Value;
                        }
                        return "default value";
                    }
                    private TriedEx<string> GetResult() => default;
                }
            }
            """;

        await VerifyAsync(source);
    }

    private static async Task VerifyAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<TryResultErrorUsageAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.TestState.Sources.Add(("TriedExStubs.cs", TestSources.TriedExStubs));
        test.ExpectedDiagnostics.AddRange(expected);

        await test.RunAsync();
    }
}
